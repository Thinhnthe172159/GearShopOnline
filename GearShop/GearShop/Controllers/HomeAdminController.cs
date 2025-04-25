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

                // Bỏ qua người dùng có vai trò Admin
                if (roles.Contains("Admin"))
                    continue;

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
        public async Task<IActionResult> ManageRoles(ManageRoleViewModel model, string selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Đảm bảo vai trò được chọn không phải là Admin
            if (selectedRoles == "Admin")
            {
                TempData["ErrorMessage"] = "Không được phép gán vai trò Admin";
                return RedirectToAction(nameof(UserRoles));
            }

            // Lấy tất cả role hiện tại của user
            var userRoles = await _userManager.GetRolesAsync(user);

            // Xóa tất cả role hiện tại
            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể xóa các vai trò hiện tại");
                return View(model);
            }

            // Thêm vai trò mới được chọn
            if (!string.IsNullOrEmpty(selectedRoles))
            {
                result = await _userManager.AddToRoleAsync(user, selectedRoles);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể gán vai trò đã chọn");
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = "Đã cập nhật vai trò thành công";
            return RedirectToAction(nameof(UserRoles));
        }

        // POST: HomeAdmin/DeleteStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "ID người dùng không hợp lệ";
                return RedirectToAction(nameof(StaffList));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                return RedirectToAction(nameof(StaffList));
            }

            // Kiểm tra xem người dùng có phải là Staff không
            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff)
            {
                TempData["ErrorMessage"] = "Người dùng này không phải là Staff";
                return RedirectToAction(nameof(StaffList));
            }

            // Xóa người dùng khỏi role Staff trước
            await _userManager.RemoveFromRoleAsync(user, "Staff");

            // Xóa người dùng
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã xóa nhân viên thành công";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa nhân viên: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(StaffList));
        }
    }
}
