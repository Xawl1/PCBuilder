using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;

namespace PCBuilder.Controllers
{
    public class VisualizationController : Controller
    {
        private readonly AppDbContext _context;

        public VisualizationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int buildId)
        {
            var build = await _context.Builds
                .Include(b => b.BuildItems)
                .ThenInclude(bi => bi.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.Id == buildId);

            if (build == null)
                return NotFound();

            return View(build);
        }

        [HttpGet]
        public async Task<IActionResult> GetBuildParts(int buildId)
        {
            try
            {
                var build = await _context.Builds
                    .Include(b => b.BuildItems)
                    .ThenInclude(bi => bi.Product)
                    .FirstOrDefaultAsync(b => b.Id == buildId);

                if (build == null)
                    return NotFound();

                var parts = build.BuildItems.Select(bi => new
                {
                    id = bi.ProductId,
                    name = bi.Product.ModelName,
                    categoryId = bi.Product.CategoryId,
                    categoryName = bi.Product.Category?.CategoryName ?? "Unknown",
                    brand = bi.Product.Brand,
                    price = bi.Product.Price,
                    quantity = bi.Quantity
                });

                return Json(parts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}