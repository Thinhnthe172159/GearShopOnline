using System.Diagnostics;
using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity != null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "HomeAdmin");
                        }
                        else if (roles.Contains("Staff"))
                        {
                            return RedirectToAction("Index", "HomeStaff");
                        }
                    }
                }
            }
            ViewBag.Feartureproduct = await _context.products.Include(a => a.Orders).Include(a => a.Images).OrderByDescending(a => a.Orders.Count()).Take(8).ToListAsync();
            ViewBag.Recentproduct = await _context.products.Include(a => a.Images).OrderByDescending(a => a.CreatedDate).Take(8).ToListAsync();
            ViewBag.productTypeList = await _context.productTypes.Include(a => a.Products.Where(a => a.Status == 1)).Where(a => a.Status == 1).ToListAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
