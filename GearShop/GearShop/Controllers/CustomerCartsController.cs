using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GearShop.Data;
using GearShop.Models;

namespace GearShop.Controllers
{
    public class CustomerCartsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerCartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CustomerCarts
        public async Task<IActionResult> Index(string Id)
        {
            var applicationDbContext = _context.carts.Include(c => c.Customer).Include(c => c.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CustomerCarts/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cart = await _context.carts
                .Include(c => c.Customer)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cart == null)
            {
                return NotFound();
            }

            return View(cart);
        }

        // add to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string userId, long productId, int quantity = 1)
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

            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", cart.UserId);
            ViewData["ProductId"] = new SelectList(_context.products, "Id", "Id", cart.ProductId);
            return RedirectToAction(nameof(Details), nameof(CustomerProductsController), new { Id = productId });
        }

        // GET: CustomerCarts/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cart = await _context.carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", cart.UserId);
            ViewData["ProductId"] = new SelectList(_context.products, "Id", "Id", cart.ProductId);
            return View(cart);
        }

        // POST: CustomerCarts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductId,UserId,Quantity")] Cart cart)
        {
            if (id != cart.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cart);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CartExists(cart.Id))
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
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", cart.UserId);
            ViewData["ProductId"] = new SelectList(_context.products, "Id", "Id", cart.ProductId);
            return View(cart);
        }

        // GET: CustomerCarts/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cart = await _context.carts
                .Include(c => c.Customer)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cart == null)
            {
                return NotFound();
            }

            return View(cart);
        }

        // POST: CustomerCarts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var cart = await _context.carts.FindAsync(id);
            if (cart != null)
            {
                _context.carts.Remove(cart);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CartExists(long id)
        {
            return _context.carts.Any(e => e.Id == id);
        }
    }
}
