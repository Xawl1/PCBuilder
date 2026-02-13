using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Models;
using System.Security.Claims;

namespace PCBuilder.Controllers
{
    [Authorize] // Must be logged in to access builds
    public class BuildController : Controller
    {
        private readonly AppDbContext _context;

        public BuildController(AppDbContext context)
        {
            _context = context;
        }

        // Get current user's builds
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var builds = await _context.Builds
                .Include(b => b.BuildItems)
                    .ThenInclude(bi => bi.Product)
                        .ThenInclude(p => p.Category)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync();

            return View(builds);
        }

        // View a specific build
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();

            var build = await _context.Builds
                .Include(b => b.BuildItems)
                    .ThenInclude(bi => bi.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (build == null)
                return NotFound();

            return View(build);
        }

        // Create new build
        [HttpPost]
        public async Task<IActionResult> Create(string buildName)
        {
            var userId = GetCurrentUserId();

            var build = new Build
            {
                UserId = userId,
                BuildName = string.IsNullOrEmpty(buildName) ? $"Build {DateTime.Now:MMM dd}" : buildName,
                CreatedAt = DateTime.Now
            };

            _context.Builds.Add(build);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Add product to build
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> AddToBuild(int productId, int? buildId)
        {
            var userId = GetCurrentUserId();

            // Try to get active build from session
            int? activeBuildId = buildId ?? HttpContext.Session.GetInt32("ActiveBuildId");

            Build build;

            if (activeBuildId.HasValue)
            {
                // Use existing build
                build = await _context.Builds
                    .Include(b => b.BuildItems)
                    .FirstOrDefaultAsync(b => b.Id == activeBuildId && b.UserId == userId);

                if (build == null)
                {
                    // Build doesn't exist or doesn't belong to user, create new
                    build = new Build
                    {
                        UserId = userId,
                        BuildName = $"Build {DateTime.Now:MMM dd}",
                        CreatedAt = DateTime.Now
                    };
                    _context.Builds.Add(build);
                    await _context.SaveChangesAsync();

                    // Set as active
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
            }
            else
            {
                // No active build, check if user has any builds
                var existingBuilds = await _context.Builds
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                    .ToListAsync();

                if (existingBuilds.Any())
                {
                    // Use the most recent build
                    build = existingBuilds.First();
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
                else
                {
                    // Create new build
                    build = new Build
                    {
                        UserId = userId,
                        BuildName = $"Build {DateTime.Now:MMM dd}",
                        CreatedAt = DateTime.Now
                    };
                    _context.Builds.Add(build);
                    await _context.SaveChangesAsync();

                    // Set as active
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
            }

            // Add product to build (same logic)
            var existingItem = build.BuildItems
                .FirstOrDefault(bi => bi.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                build.BuildItems.Add(new BuildItem
                {
                    ProductId = productId,
                    Quantity = 1
                });
            }

            build.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Product added to your build!";
            return RedirectToAction("Index", "Products");
        }

        // Remove from build
        [HttpPost]
        public async Task<IActionResult> RemoveFromBuild(int buildItemId)
        {
            var item = await _context.BuildItems
                .Include(bi => bi.Build)
                .FirstOrDefaultAsync(bi => bi.Id == buildItemId);

            if (item == null || item.Build.UserId != GetCurrentUserId())
                return NotFound();

            _context.BuildItems.Remove(item);

            item.Build.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = item.BuildId });
        }

        // Update quantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int buildItemId, int quantity)
        {
            if (quantity < 1) quantity = 1;

            var item = await _context.BuildItems
                .Include(bi => bi.Build)
                .FirstOrDefaultAsync(bi => bi.Id == buildItemId);

            if (item == null || item.Build.UserId != GetCurrentUserId())
                return NotFound();

            item.Quantity = quantity;
            item.Build.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = item.BuildId });
        }

        // Delete build
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var build = await _context.Builds
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == GetCurrentUserId());

            if (build == null)
                return NotFound();

            _context.Builds.Remove(build);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
        [HttpPost]
        public IActionResult SetActiveBuild(int id)
        {
            var userId = GetCurrentUserId();
            var build = _context.Builds.FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (build != null)
            {
                HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                TempData["Message"] = $"Now building: {build.BuildName}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}