using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrderController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> userManager;

        public CustomerOrderController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int status = -1)
        {
            //Đơn hàng
            string userId = string.Empty;
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                userId = user.Id;
            }

            var orderstatus = new OrderStatus();
            ViewBag.OrderStatus = orderstatus.GetAllStatus().ToList();

            var list = _context.orders.Include(o => o.Product).Include(o => o.Product.Images).Where(o => o.ApplicationUser.Id == userId).OrderByDescending(o => o.CreateDate).AsQueryable();
            if (status != -1)
            {
                // Hiển thị đơn hàng theo trạng thái
                list = list.Where(o => o.Status == status);
                if(status == 4)
                {
                    list = list.OrderByDescending(o => o.ReviceDate);
                }
            }
            if (!string.IsNullOrEmpty(search))
            {
                list = list.Where(o => o.Product.ProductName.ToLower().Contains(search.ToLower()) || o.OrderCode == search);
            }
            ViewBag.CurrentFilter = new { search = search, status = status };

            return View(await list.ToListAsync());
        }

        [HttpPost]
        public IActionResult Update(long Orderid, int status)
        {
            var order = _context.orders.Include(o => o.Product).FirstOrDefault(o => o.Id == Orderid);
            if (status == 0)// hủy đơn
            {
                if (order != null)
                {
                    if (order.Status == 1)
                    {
                        order.Status = status;
                    }
                    if (order.Status == 2)
                    {
                        order.Status = status;
                        var product = _context.products.Find(order.ProductId);
                        if (product != null)
                        {
                            product.Quantity += order.Quantity;
                        }
                    }
                }
            }
            if (status == 4 && order != null && order.Status == 3)
            {
                order.Status = status;
                order.ReviceDate = DateTime.Now;
            }
            _context.SaveChanges();
            return RedirectToAction("Index", new { status = status });
        }
    }
}
