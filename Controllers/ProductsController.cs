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
        private const int PageSize = 20; // Products per page

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, int? tier, string brand, string sortOrder, int page = 1)
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

            // Filter by brand if selected
            if (!string.IsNullOrEmpty(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "name_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Brand).ThenBy(p => p.ModelName);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Brand).ThenBy(p => p.ModelName);
                    break;
            }

            // Get total count for pagination
            var totalItems = await productsQuery.CountAsync();

            // Apply pagination
            var products = await productsQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Get all categories
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Get all unique brands WITH COUNTS
            var brandCounts = await _context.Products
                .GroupBy(p => p.Brand)
                .Select(g => new { Brand = g.Key, Count = g.Count() })
                .OrderBy(b => b.Brand)
                .ToListAsync();

            // Get TOTAL product count
            var totalProducts = await _context.Products.CountAsync();

            ViewBag.Brands = brandCounts;
            ViewBag.TotalProducts = totalProducts;

            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedTier = tier;
            ViewBag.SelectedBrand = brand;
            ViewBag.CurrentSort = sortOrder;

            // Pagination info
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            ViewBag.PageSize = PageSize;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }
    }
}