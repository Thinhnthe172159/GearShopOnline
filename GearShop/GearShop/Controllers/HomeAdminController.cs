using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.IO;
using Spire.Xls;
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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> StaffList()
        {
            
            var staffRole = await _roleManager.FindByNameAsync("Staff");
            if (staffRole == null)
            {
                return View(new List<IdentityUser>());
            }

        
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");

            return View(staffUsers);
        }
 
        public async Task<IActionResult> UserRoles()
        {
            var users = _userManager.Users.ToList();
            var userRolesViewModel = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);


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

        
            var roles = _roleManager.Roles.ToList();
            model.AllRoles = roles.Select(r => r.Name).ToList();

   
            var userRoles = await _userManager.GetRolesAsync(user);
            model.UserRoles = userRoles.ToList();

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageRoleViewModel model, string selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

  
            if (selectedRoles == "Admin")
            {
                TempData["ErrorMessage"] = "Không được phép gán vai trò Admin";
                return RedirectToAction(nameof(UserRoles));
            }

       
            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể xóa các vai trò hiện tại");
                return View(model);
            }

            
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

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff)
            {
                TempData["ErrorMessage"] = "Người dùng này không phải là Staff";
                return RedirectToAction(nameof(StaffList));
            }

          
            await _userManager.RemoveFromRoleAsync(user, "Staff");

      
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
 
        public async Task<IActionResult> RevenueStatistics(string period = "day")
        {
            var today = DateTime.Today;
            var stats = new RevenueStatisticsViewModel();

            var productStats = await _context.orders
                .Where(o => o.Status == 4) // Chỉ tính đơn hàng đã nhận
                .GroupBy(o => new { o.ProductId, ProductName = o.Product.ProductName })
                .Select(g => new ProductStatisticsViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(o => o.Quantity),
                    TotalRevenue = g.Sum(o => o.SoldPrice * o.Quantity)
                })
                .ToListAsync();

            stats.BestSellingProducts = productStats.OrderByDescending(p => p.TotalQuantity).Take(5).ToList();
            stats.WorstSellingProducts = productStats.OrderBy(p => p.TotalQuantity).Take(5).ToList();

  
            switch (period.ToLower())
            {
                case "month":
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    stats.Period = "Tháng " + today.Month + "/" + today.Year;
                    stats.DailyRevenues = await GetDailyRevenues(firstDayOfMonth, lastDayOfMonth);
                    break;

                case "year":
                    var firstDayOfYear = new DateTime(today.Year, 1, 1);
                    var lastDayOfYear = new DateTime(today.Year, 12, 31);

                    stats.Period = "Năm " + today.Year;
                    stats.MonthlyRevenues = await GetMonthlyRevenues(today.Year);
                    break;

                default: 
                    stats.Period = "Ngày " + today.ToString("dd/MM/yyyy");
                    stats.DailyRevenue = await _context.orders
                        .Where(o => o.Status == 4 && o.CreateDate.Date == today)
                        .SumAsync(o => o.SoldPrice * o.Quantity);
                    stats.DailyOrderCount = await _context.orders
                        .Where(o => o.Status == 4 && o.CreateDate.Date == today)
                        .CountAsync();
                    break;
            }

            ViewBag.SelectedPeriod = period;
            return View(stats);
        }

        private async Task<List<DailyRevenueViewModel>> GetDailyRevenues(DateTime startDate, DateTime endDate)
        {
            return await _context.orders
                .Where(o => o.Status == 4 && o.CreateDate.Date >= startDate.Date && o.CreateDate.Date <= endDate.Date)
                .GroupBy(o => o.CreateDate.Date)
                .Select(g => new DailyRevenueViewModel
                {
                    Date = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.SoldPrice * o.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        private async Task<List<MonthlyRevenueViewModel>> GetMonthlyRevenues(int year)
        {
            return await _context.orders
                .Where(o => o.Status == 4 && o.CreateDate.Year == year)
                .GroupBy(o => new { Month = o.CreateDate.Month, Year = o.CreateDate.Year })
                .Select(g => new MonthlyRevenueViewModel
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.SoldPrice * o.Quantity)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();
        }
        [Authorize(Roles = "Admin")] // Thêm kiểm tra phân quyền
        public async Task<IActionResult> ExportRevenueStatistics(string period = "day")
        {
            var today = DateTime.Today;

            // Lấy dữ liệu thống kê
            var stats = new RevenueStatisticsViewModel();

            // Lấy dữ liệu sản phẩm bán chạy/ít nhất - chỉ tính đơn hàng đã nhận (Status = 4)
            var productStats = await _context.orders
                .Where(o => o.Status == 4)
                .GroupBy(o => new { o.ProductId, ProductName = o.Product.ProductName })
                .Select(g => new ProductStatisticsViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(o => o.Quantity),
                    TotalRevenue = g.Sum(o => o.SoldPrice * o.Quantity)
                })
                .ToListAsync();

            stats.BestSellingProducts = productStats.OrderByDescending(p => p.TotalQuantity).Take(5).ToList();
            stats.WorstSellingProducts = productStats.OrderBy(p => p.TotalQuantity).Take(5).ToList();

            // Doanh thu theo ngày/tháng/năm
            switch (period.ToLower())
            {
                case "month":
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    stats.Period = "Tháng " + today.Month + "/" + today.Year;
                    stats.DailyRevenues = await GetDailyRevenues(firstDayOfMonth, lastDayOfMonth);
                    break;

                case "year":
                    var firstDayOfYear = new DateTime(today.Year, 1, 1);
                    var lastDayOfYear = new DateTime(today.Year, 12, 31);

                    stats.Period = "Năm " + today.Year;
                    stats.MonthlyRevenues = await GetMonthlyRevenues(today.Year);
                    break;

                default: // day
                    stats.Period = "Ngày " + today.ToString("dd/MM/yyyy");
                    stats.DailyRevenue = await _context.orders
                        .Where(o => o.Status == 4 && o.CreateDate.Date == today)
                        .SumAsync(o => o.SoldPrice * o.Quantity);
                    stats.DailyOrderCount = await _context.orders
                        .Where(o => o.Status == 4 && o.CreateDate.Date == today)
                        .CountAsync();
                    break;
            }

            // Tạo file Excel với FreeSpire.XLS
            Workbook workbook = new Workbook();
            Worksheet sheet = workbook.Worksheets[0];
            sheet.Name = "Thống kê doanh thu";

            // Thiết lập tiêu đề
            sheet.Range["A1"].Text = $"THỐNG KÊ DOANH THU - {stats.Period.ToUpper()}";
            sheet.Range["A1:E1"].Merge();
            sheet.Range["A1"].Style.Font.IsBold = true;
            sheet.Range["A1"].Style.Font.Size = 16;
            sheet.Range["A1"].Style.HorizontalAlignment = HorizontalAlignType.Center;

            int currentRow = 3;

            // Thiết lập dữ liệu dựa trên loại khoảng thời gian
            if (period.ToLower() == "day")
            {
                sheet.Range[$"A{currentRow}"].Text = "Doanh thu hôm nay:";
                sheet.Range[$"B{currentRow}"].Text = stats.DailyRevenue.ToString("#,##0") + " VNĐ";

                currentRow++;
                sheet.Range[$"A{currentRow}"].Text = "Số đơn hàng:";
                sheet.Range[$"B{currentRow}"].Text = stats.DailyOrderCount.ToString();
            }
            else if (period.ToLower() == "month")
            {
                // Header
                sheet.Range[$"A{currentRow}"].Text = "Ngày";
                sheet.Range[$"B{currentRow}"].Text = "Số đơn hàng";
                sheet.Range[$"C{currentRow}"].Text = "Doanh thu";

                ApplyHeaderStyleFreeSpire(sheet, currentRow, 1, 3);

                currentRow++;

                // Data
                foreach (var item in stats.DailyRevenues)
                {
                    sheet.Range[$"A{currentRow}"].Text = item.Date.ToString("dd/MM/yyyy");
                    sheet.Range[$"B{currentRow}"].Text = item.TotalOrders.ToString();
                    sheet.Range[$"C{currentRow}"].Text = item.TotalRevenue.ToString("#,##0") + " VNĐ";
                    currentRow++;
                }

                // Tổng cộng
                sheet.Range[$"A{currentRow}"].Text = "Tổng cộng";
                sheet.Range[$"A{currentRow}"].Style.Font.IsBold = true;
                sheet.Range[$"B{currentRow}"].Text = stats.DailyRevenues.Sum(d => d.TotalOrders).ToString();
                sheet.Range[$"B{currentRow}"].Style.Font.IsBold = true;
                sheet.Range[$"C{currentRow}"].Text = stats.DailyRevenues.Sum(d => d.TotalRevenue).ToString("#,##0") + " VNĐ";
                sheet.Range[$"C{currentRow}"].Style.Font.IsBold = true;
            }
            else if (period.ToLower() == "year")
            {
                // Header
                sheet.Range[$"A{currentRow}"].Text = "Tháng";
                sheet.Range[$"B{currentRow}"].Text = "Số đơn hàng";
                sheet.Range[$"C{currentRow}"].Text = "Doanh thu";

                ApplyHeaderStyleFreeSpire(sheet, currentRow, 1, 3);

                currentRow++;

                // Data
                foreach (var item in stats.MonthlyRevenues)
                {
                    sheet.Range[$"A{currentRow}"].Text = $"Tháng {item.Month}/{item.Year}";
                    sheet.Range[$"B{currentRow}"].Text = item.TotalOrders.ToString();
                    sheet.Range[$"C{currentRow}"].Text = item.TotalRevenue.ToString("#,##0") + " VNĐ";
                    currentRow++;
                }

                // Tổng cộng
                sheet.Range[$"A{currentRow}"].Text = "Tổng cộng";
                sheet.Range[$"A{currentRow}"].Style.Font.IsBold = true;
                sheet.Range[$"B{currentRow}"].Text = stats.MonthlyRevenues.Sum(m => m.TotalOrders).ToString();
                sheet.Range[$"B{currentRow}"].Style.Font.IsBold = true;
                sheet.Range[$"C{currentRow}"].Text = stats.MonthlyRevenues.Sum(m => m.TotalRevenue).ToString("#,##0") + " VNĐ";
                sheet.Range[$"C{currentRow}"].Style.Font.IsBold = true;
            }

            // Thêm khoảng cách
            currentRow += 2;

            // Thêm phần sản phẩm bán chạy nhất
            sheet.Range[$"A{currentRow}"].Text = "SẢN PHẨM BÁN CHẠY NHẤT";
            sheet.Range[$"A{currentRow}:C{currentRow}"].Merge();
            sheet.Range[$"A{currentRow}"].Style.Font.IsBold = true;
            sheet.Range[$"A{currentRow}"].Style.Font.Size = 14;
            sheet.Range[$"A{currentRow}"].Style.HorizontalAlignment = HorizontalAlignType.Center;

            currentRow++;

            // Header cho sản phẩm bán chạy
            sheet.Range[$"A{currentRow}"].Text = "Tên sản phẩm";
            sheet.Range[$"B{currentRow}"].Text = "Số lượng đã bán";
            sheet.Range[$"C{currentRow}"].Text = "Doanh thu";

            ApplyHeaderStyleFreeSpire(sheet, currentRow, 1, 3);

            currentRow++;

            // Data sản phẩm bán chạy
            foreach (var item in stats.BestSellingProducts)
            {
                sheet.Range[$"A{currentRow}"].Text = item.ProductName;
                sheet.Range[$"B{currentRow}"].Text = item.TotalQuantity.ToString();
                sheet.Range[$"C{currentRow}"].Text = item.TotalRevenue.ToString("#,##0") + " VNĐ";
                currentRow++;
            }

            // Thêm khoảng cách
            currentRow += 2;

            // Thêm phần sản phẩm bán ít nhất
            sheet.Range[$"A{currentRow}"].Text = "SẢN PHẨM BÁN ÍT NHẤT";
            sheet.Range[$"A{currentRow}:C{currentRow}"].Merge();
            sheet.Range[$"A{currentRow}"].Style.Font.IsBold = true;
            sheet.Range[$"A{currentRow}"].Style.Font.Size = 14;
            sheet.Range[$"A{currentRow}"].Style.HorizontalAlignment = HorizontalAlignType.Center;

            currentRow++;

            // Header cho sản phẩm bán ít
            sheet.Range[$"A{currentRow}"].Text = "Tên sản phẩm";
            sheet.Range[$"B{currentRow}"].Text = "Số lượng đã bán";
            sheet.Range[$"C{currentRow}"].Text = "Doanh thu";

            ApplyHeaderStyleFreeSpire(sheet, currentRow, 1, 3);

            currentRow++;

            // Data sản phẩm bán ít
            foreach (var item in stats.WorstSellingProducts)
            {
                sheet.Range[$"A{currentRow}"].Text = item.ProductName;
                sheet.Range[$"B{currentRow}"].Text = item.TotalQuantity.ToString();
                sheet.Range[$"C{currentRow}"].Text = item.TotalRevenue.ToString("#,##0") + " VNĐ";
                currentRow++;
            }

            // Tự động điều chỉnh độ rộng cho các cột
            sheet.AllocatedRange.AutoFitColumns();

            // Đặt border cho toàn bộ dữ liệu
            sheet.Range[1, 1, currentRow - 1, 3].BorderAround(LineStyleType.Thin);
            sheet.Range[1, 1, currentRow - 1, 3].BorderInside(LineStyleType.Thin);

            // Tạo tên file dựa vào ngày và loại thống kê
            string fileName = $"ThongKeDoanhThu_{DateTime.Now:yyyyMMdd}_{period}.xlsx";

            // Lưu file và trả về cho client
            var stream = new MemoryStream();
            workbook.SaveToStream(stream, FileFormat.Version2013);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Phương thức hỗ trợ định dạng header cho FreeSpire.XLS
        private void ApplyHeaderStyleFreeSpire(Worksheet sheet, int row, int startCol, int endCol)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                var cell = sheet.Range[row, col];
                cell.Style.Font.IsBold = true;
                cell.Style.Color = System.Drawing.Color.LightGray;
                cell.Style.HorizontalAlignment = HorizontalAlignType.Center;
                cell.Style.VerticalAlignment = VerticalAlignType.Center;

                // Đặt viền bằng cách sử dụng BordersLineType
                cell.Style.Borders[BordersLineType.EdgeLeft].LineStyle = LineStyleType.Thin;   // Viền trái
                cell.Style.Borders[BordersLineType.EdgeRight].LineStyle = LineStyleType.Thin;  // Viền phải
                cell.Style.Borders[BordersLineType.EdgeTop].LineStyle = LineStyleType.Thin;    // Viền trên
                cell.Style.Borders[BordersLineType.EdgeBottom].LineStyle = LineStyleType.Thin; // Viền dưới
            }
        }
        
    }
}
