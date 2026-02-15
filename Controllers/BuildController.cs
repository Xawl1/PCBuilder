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
        public async Task<IActionResult> AddToBuild(int productId, int? buildId)
        {
            var userId = GetCurrentUserId();

            // Вземи продукта, който се опитваме да добавим
            var newProduct = await _context.Products.FindAsync(productId);
            if (newProduct == null)
                return NotFound();

            // Try to get active build from session
            int? activeBuildId = buildId ?? HttpContext.Session.GetInt32("ActiveBuildId");

            Build build;

            if (activeBuildId.HasValue)
            {
                build = await _context.Builds
                    .Include(b => b.BuildItems)
                    .ThenInclude(bi => bi.Product)
                    .FirstOrDefaultAsync(b => b.Id == activeBuildId && b.UserId == userId);

                if (build == null)
                {
                    build = new Build
                    {
                        UserId = userId,
                        BuildName = $"Build {DateTime.Now:MMM dd}",
                        CreatedAt = DateTime.Now
                    };
                    _context.Builds.Add(build);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
            }
            else
            {
                var existingBuilds = await _context.Builds
                    .Where(b => b.UserId == userId)
                    .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                    .ToListAsync();

                if (existingBuilds.Any())
                {
                    build = existingBuilds.First();
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
                else
                {
                    build = new Build
                    {
                        UserId = userId,
                        BuildName = $"Build {DateTime.Now:MMM dd}",
                        CreatedAt = DateTime.Now
                    };
                    _context.Builds.Add(build);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetInt32("ActiveBuildId", build.Id);
                }
            }
            // Tier 2 works with everything, but Tier 1 and Tier 3 cannot mix
            if (build.BuildItems.Any())
            {
                var existingTiers = build.BuildItems.Select(bi => bi.Product.Tier).Distinct().ToList();
                if (existingTiers.Contains(1) && newProduct.Tier == 3)
                {
                    TempData["Error"] = $"❌ Cannot add Tier 3 part to a Tier 1 build!";
                    return RedirectToAction("Index", "Products");
                }
                if (existingTiers.Contains(3) && newProduct.Tier == 1)
                {
                    TempData["Error"] = $"❌ Cannot add Tier 1 part to a Tier 3 build!";
                    return RedirectToAction("Index", "Products");
                }
                if (existingTiers.Contains(1) && existingTiers.Contains(3))
                {
                    TempData["Error"] = $"❌ Build contains both Tier 1 and Tier 3 parts! This is not allowed.";
                    return RedirectToAction("Index", "Products");
                }
            }

            // Add product to build
            var existingItem = build.BuildItems
                .FirstOrDefault(bi => bi.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                TempData["Message"] = $"Added another {newProduct.Brand} {newProduct.ModelName} to your build!";
            }
            else
            {
                build.BuildItems.Add(new BuildItem
                {
                    ProductId = productId,
                    Quantity = 1
                });
                TempData["Message"] = $"✅ Added {newProduct.Brand} {newProduct.ModelName} to your build!";
            }

            build.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

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