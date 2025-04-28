using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffManageAccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<StaffManageAccountController> _logger;
        private const int PageSize = 10;

        public StaffManageAccountController(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext, ILogger<StaffManageAccountController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: UserStaff
        public async Task<IActionResult> Index(int? page, int? status, string sortOrder)
        {
            int pageNumber = page ?? 1;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["SelectedStatus"] = status;

            var users = _dbContext.ApplicationUsers.AsQueryable();

            if (status.HasValue)
            {
                users = users.Where(u => u.status == status.Value);
            }

            switch (sortOrder)
            {
                case "username":
                    users = users.OrderBy(u => u.UserName);
                    break;
                case "username_desc":
                    users = users.OrderByDescending(u => u.UserName);
                    break;
                case "fullname":
                    users = users.OrderBy(u => u.FullName);
                    break;
                case "fullname_desc":
                    users = users.OrderByDescending(u => u.FullName);
                    break;
                case "email":
                    users = users.OrderBy(u => u.Email);
                    break;
                case "email_desc":
                    users = users.OrderByDescending(u => u.Email);
                    break;
                case "createdate":
                    users = users.OrderBy(u => u.CreateDate);
                    break;
                case "createdate_desc":
                    users = users.OrderByDescending(u => u.CreateDate);
                    break;
                case "status":
                    users = users.OrderBy(u => u.status);
                    break;
                case "status_desc":
                    users = users.OrderByDescending(u => u.status);
                    break;
                default:
                    users = users.OrderByDescending(u => u.CreateDate);
                    break;
            }

            var userList = await users.ToListAsync();
            var pagedUsers = userList.ToPagedList(pageNumber, PageSize);

            _logger.LogInformation("Hiển thị danh sách người dùng, trang {PageNumber}, trạng thái {Status}, sắp xếp {SortOrder}", pageNumber, status, sortOrder);
            return View(pagedUsers);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Yêu cầu xem chi tiết người dùng với Id null.");
                return NotFound();
            }

            var user = await _dbContext.ApplicationUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Hiển thị chi tiết người dùng Id={Id}", id);
            return View(user);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Yêu cầu chỉnh sửa người dùng với Id null.");
                return NotFound();
            }

            var user = await _dbContext.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Hiển thị trang chỉnh sửa người dùng Id={Id}", id);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,FullName,City,District,Commune,PhoneNumber,status")] ApplicationUser user)
        {
            if (id != user.Id)
            {
                _logger.LogWarning("Id không khớp khi chỉnh sửa người dùng. Id={Id}, User.Id={UserId}", id, user.Id);
                return NotFound();
            }

            var existingUser = await _dbContext.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với Id={Id}", id);
                return NotFound();
            }

            bool hasValidationErrors = false;

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                ModelState.AddModelError("FullName", "Tên đầy đủ là bắt buộc.");
                hasValidationErrors = true;
            }

            if (user.status != 0 && user.status != 1)
            {
                ModelState.AddModelError("status", "Trạng thái chỉ có thể là 'Đang hoạt động' hoặc 'Không hoạt động'.");
                hasValidationErrors = true;
            }

            if (!hasValidationErrors && ModelState.IsValid)
            {
                try
                {
                    existingUser.FullName = user.FullName;
                    existingUser.City = user.City;
                    existingUser.District = user.District;
                    existingUser.Commune = user.Commune;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.status = user.status;

                    _dbContext.ApplicationUsers.Update(existingUser);
                    await _dbContext.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thông tin người dùng đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không mong muốn khi cập nhật người dùng Id={Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật người dùng. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
            }

            return View(existingUser);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Yêu cầu vô hiệu hóa người dùng với Id null.");
                return NotFound();
            }

            var user = await _dbContext.ApplicationUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Hiển thị trang xác nhận vô hiệu hóa người dùng Id={Id}", id);
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _dbContext.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với Id={Id}", id);
                return NotFound();
            }

            try
            {
                user.status = 2;
                _dbContext.ApplicationUsers.Update(user);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tài khoản đã được vô hiệu hóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi vô hiệu hóa người dùng Id={Id}: {Message}", id, ex.Message);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi vô hiệu hóa tài khoản. Vui lòng thử lại.");
                return View(user);
            }
        }
    }
}