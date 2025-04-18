using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace GearShop.Controllers
{
    public class HomeStaffController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
