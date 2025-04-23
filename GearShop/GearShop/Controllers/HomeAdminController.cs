using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeAdminController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeAdminController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Trang chính của admin
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> StaffList()
        {
            // Lấy tất cả các user có role Staff một cách hiệu quả hơn
            var staffRole = await _roleManager.FindByNameAsync("Staff");
            if (staffRole == null)
            {
                return View(new List<IdentityUser>());
            }

            // Lấy danh sách user có role Staff
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");

            return View(staffUsers);
        }
        // GET: HomeAdmin/UserRoles
        public async Task<IActionResult> UserRoles()
        {
            var users = _userManager.Users.ToList();
            var userRolesViewModel = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList()
                });
            }

            return View(userRolesViewModel);
        }

        // GET: HomeAdmin/ManageRoles/userId
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ManageRoleViewModel
            {
                UserId = userId,
                UserName = user.UserName,
                Email = user.Email
            };

            // Lấy tất cả các role
            var roles = _roleManager.Roles.ToList();
            model.AllRoles = roles.Select(r => r.Name).ToList();

            // Lấy các role mà user hiện có
            var userRoles = await _userManager.GetRolesAsync(user);
            model.UserRoles = userRoles.ToList();

            return View(model);
        }

        // POST: HomeAdmin/ManageRoles/userId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageRoleViewModel model, List<string> selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy tất cả role hiện tại của user
            var userRoles = await _userManager.GetRolesAsync(user);

            // Xóa tất cả role hiện tại
            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể xóa các role hiện tại");
                return View(model);
            }

            // Thêm các role được chọn
            if (selectedRoles != null && selectedRoles.Count > 0)
            {
                result = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể gán các role đã chọn");
                    return View(model);
                }
            }

            return RedirectToAction(nameof(UserRoles));
        }
    }
}
