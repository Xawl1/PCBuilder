using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PCBuilder.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, int? tier)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Filter by category if selected
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by tier if selected
            if (tier.HasValue && tier.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.Tier == tier.Value);
            }

            var products = await productsQuery.ToListAsync();

            // Get all categories for the filter menu
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedTier = tier;

            return View(products);
        }
    }
}