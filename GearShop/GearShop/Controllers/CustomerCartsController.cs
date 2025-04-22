using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GearShop.Data;
using GearShop.Models;
using AspNetCoreGeneratedDocument;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;

namespace GearShop.Controllers
{

    public class CustomerCartsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerCartsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string? code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var coupon = _context.coupons.FirstOrDefault(c => c.Code == code);
                if (coupon != null && coupon.status == 1 && coupon.DateCreate < DateTime.Now && DateTime.Now < coupon.DateEnd)
                {
                    TempData["coupon"] = coupon.discout.ToString();
                    TempData["Noti"] = "Đã áp dụng thành công mã khuyễn mại";
                    return RedirectToAction(nameof(Index), new { Id = user.Id, couponCode = coupon.Code });
                }
                else
                {
                    TempData["Noti"] = "Mã khuyến mại không khả dụng tại thời điểm này!";
                }
                return RedirectToAction(nameof(Index), new { Id = user.Id });
            }
            return BadRequest();
        }


        // GET: CustomerCarts
        [HttpGet]
        public async Task<IActionResult> Index(string Id, string? couponCode)
        {
            decimal coupon = 0;
            if (TempData["coupon"] != null)
            {
                decimal.TryParse((TempData["coupon"] + ""), out coupon);
            }
            ViewBag.Coupon = coupon;
            ViewBag.Code = couponCode;
            var user = await _userManager.GetUserAsync(User);
            string id = string.Empty;
            if (user != null)
            {
                id = user.Id;
            }
            var applicationDbContext = _context.carts.Include(c => c.Customer).Include(c => c.Product).Include(c => c.Product.Images).Where(c => c.UserId == id);
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
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(string userId, long productId, int quantity = 1)
        {
            var cart = new Cart { UserId = userId, ProductId = productId, Quantity = quantity };
            var productIncart = _context.carts.FirstOrDefault(p => p.ProductId == productId && p.UserId == userId);
            if (productIncart == null)
            {
                try
                {
                    _context.Add(cart);
                    await _context.SaveChangesAsync();
                    TempData["Noti"] = "Đã thêm sản phẩm vào giỏ hàng!";
                }
                catch (Exception) { }
            }
            else
            {
                productIncart.Quantity += 1;
                _context.carts.Update(productIncart);
                await _context.SaveChangesAsync();
                TempData["Noti"] = "Sản phẩm đã tồn tại trong giỏ hàng,\n Đã cập nhật số lượng sản phẩm trong giỏ hàng!";
            }
            return PartialView("_userCartPartial");
        }

        //update quantity of product
        [HttpPost]
        public async Task<IActionResult> Edit(long id, int quantity)
        {

            var itemCart = await _context.carts.Include(a => a.Product).FirstOrDefaultAsync(a => a.Id == id);
            if (itemCart != null && itemCart.Product.Quantity >= quantity)
            {

                try
                {
                    itemCart.Quantity = quantity;
                    _context.carts.Update(itemCart);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {

                }
            }
            else
            {
                TempData["Noti"] = "Số lượng vượt quá giới hạn!";
            }

            var user = await _userManager.GetUserAsync(User);
            string Id = string.Empty;
            if (user != null)
            {
                Id = user.Id;
            }
            var updatedCartItems = _context.carts.Include(c => c.Customer).Include(c => c.Product).Include(c => c.Product.Images).Where(c => c.UserId == Id);

            return PartialView("_itemCartPartial", updatedCartItems);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {

            var cart = await _context.carts.FindAsync(id);
            if (cart != null)
            {
                _context.carts.Remove(cart);
                await _context.SaveChangesAsync();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("Không tìm thấy người dùng");
            }

            var updatedCartItems = await _context.carts
                .Include(c => c.Customer)
                .Include(c => c.Product)
                .Include(c => c.Product.Images)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
            return PartialView("_itemCartPartial", updatedCartItems);
        }


        private bool CartExists(long id)
        {
            return _context.carts.Any(e => e.Id == id);
        }
    }
}
