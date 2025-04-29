using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GearShop.Data;
using GearShop.Models;
using X.PagedList.Extensions;
using Microsoft.Identity.Client;

namespace GearShop.Controllers
{
    [AllowOnlyCustomerOrGuest]
    public class CustomerProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, List<int?> brandId, List<int?> typeId, int choice = 0, int page = 1, int size = 12)
        {
            var priceSelect = new SelectedPrice();
            var productRresults = _context.products.Include(a => a.Brand).Include(p => p.ProductType).Include(a => a.Images).Where(a => a.Status == 1).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                productRresults = productRresults.Where(p => p.ProductName.ToLower().Contains(search.ToLower()) || p.Brand.BrandName.ToLower().Contains(search.ToLower()));
            }
            if (brandId.Count != 0)
            {
                productRresults = productRresults.Where(p => brandId.Contains(p.BrandId));
            }
            if (typeId.Count != 0)
            {
                productRresults = productRresults.Where(p => typeId.Contains(p.ProductTypeId));
            }
            if (choice != 0)
            {
                priceSelect = priceSelect.getAllFilter().FirstOrDefault(a => a.Choice == choice);
                if (priceSelect != null)
                {
                    if (priceSelect?.PriceMin != 0)
                    {
                        productRresults = productRresults.Where(p => p.Price >= priceSelect.PriceMin);
                    }
                    if (priceSelect?.PriceMax != 0)
                    {
                        productRresults = productRresults.Where(p => p.Price <= priceSelect.PriceMax);
                    }
                }
            }
            ViewBag.CurrentFilter = new
            {
                search = search,
                brandId = brandId,
                typeId = typeId,
                page = page,
                size = size,
                choice = choice,
            };
            ViewBag.CountAll = _context.products.Where(a => a.Status == 1).Count();
            ViewBag.priceSelected = priceSelect?.getAllFilter().ToList();
            ViewBag.BrandsList = await _context.brands.Include(a => a.Products.Where(a => a.Status == 1)).Where(a => a.Status == 1).ToListAsync();
            ViewBag.ProductTypes = await _context.productTypes.Include(a => a.Products.Where(a => a.Status == 1)).Where(a => a.Status == 1).ToListAsync();

            return View(productRresults.ToPagedList(page, size));
        }

        // add to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string userId, long productId, int quantity = 1)
        {
            var cart = new Cart { UserId = userId, ProductId = productId, Quantity = quantity };
            var productIncart = _context.carts.FirstOrDefault(p => p.ProductId == productId);
            if (productIncart == null)
            {
                try
                {
                    _context.Add(cart);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception) { }
            }
            else
            {
                productIncart.Quantity += 1;
                _context.carts.Update(productIncart);
                await _context.SaveChangesAsync();
            }
            return PartialView("_userCartPartial");
        }

        // GET: CustomerProducts/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Brand).Include(p => p.Comments).Include(p => p.Orders)
                .Include(p => p.ProductType).Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: CustomerProducts/Create
        public IActionResult Create()
        {
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "Id");
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "Id");
            return View();
        }

        // POST: CustomerProducts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "Id", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "Id", product.ProductTypeId);
            return View(product);
        }

        // GET: CustomerProducts/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "Id", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "Id", product.ProductTypeId);
            return View(product);
        }

        // POST: CustomerProducts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "Id", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "Id", product.ProductTypeId);
            return View(product);
        }

        // GET: CustomerProducts/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: CustomerProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var product = await _context.products.FindAsync(id);
            if (product != null)
            {
                _context.products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(long id)
        {
            return _context.products.Any(e => e.Id == id);
        }
    }
}
