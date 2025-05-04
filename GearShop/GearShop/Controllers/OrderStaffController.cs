using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using SkiaSharp;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Staff")]
    public class OrderStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderStaffController> _logger;
        private const int PageSize = 10;

        public OrderStaffController(ApplicationDbContext context, ILogger<OrderStaffController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: OrderStaff
        public async Task<IActionResult> Index(int? page, int? status, string sortOrder)
        {
            int pageNumber = page ?? 1;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = pageNumber;
            ViewBag.SelectedStatus = status;

            var orderStatusList = _context.orderStatuses.ToList();
            ViewBag.OrderStatusList = orderStatusList;

            var orders = _context.orders
                .Include(o => o.Product)
                .ThenInclude(p => p.Brand)
                .Include(o => o.Product)
                .ThenInclude(p => p.ProductType)
                .Include(o => o.ApplicationUser)
                .AsQueryable();

            if (status.HasValue)
            {
                orders = orders.Where(o => o.Status == status).AsQueryable();
            }

            switch (sortOrder)
            {
                case "code":
                    orders = orders.OrderBy(o => o.OrderCode);
                    break;
                case "code_desc":
                    orders = orders.OrderByDescending(o => o.OrderCode);
                    break;
                case "product":
                    orders = orders.OrderBy(o => o.Product.ProductName);
                    break;
                case "product_desc":
                    orders = orders.OrderByDescending(o => o.Product.ProductName);
                    break;
                case "quantity":
                    orders = orders.OrderBy(o => o.Quantity);
                    break;
                case "quantity_desc":
                    orders = orders.OrderByDescending(o => o.Quantity);
                    break;
                case "price":
                    orders = orders.OrderBy(o => o.SoldPrice);
                    break;
                case "price_desc":
                    orders = orders.OrderByDescending(o => o.SoldPrice);
                    break;
                case "customer":
                    orders = orders.OrderBy(o => o.ApplicationUser.UserName);
                    break;
                case "customer_desc":
                    orders = orders.OrderByDescending(o => o.ApplicationUser.UserName);
                    break;
                case "status":
                    orders = orders.OrderBy(o => o.Status);
                    break;
                case "status_desc":
                    orders = orders.OrderByDescending(o => o.Status);
                    break;
                default:
                    orders = orders.OrderByDescending(o => o.CreateDate);
                    break;
            }

            var orderList = await orders.ToListAsync();
            var pagedOrders = orderList.ToPagedList(pageNumber, PageSize);

            return View(pagedOrders);
        }

        // GET: OrderStaff/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu xem chi tiết đơn hàng với Id null.");
                return NotFound();
            }

            var order = await _context.orders
                .AsNoTracking()
                .Include(o => o.Product)
                    .ThenInclude(p => p.Brand)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductType)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Không tìm thấy đơn hàng với Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Hiển thị chi tiết đơn hàng Id={Id}", id);
            return View(order);
        }

        // GET: OrderStaff/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Truy cập trang tạo đơn hàng mới.");

            ViewData["ProductId"] = new SelectList(_context.products.AsNoTracking(), "Id", "ProductName");
            ViewData["CustomerId"] = new SelectList(_context.Users.AsNoTracking(), "Id", "UserName");
            ViewData["StatusList"] = new SelectList(_context.orderStatuses.ToList(), "Id", "Status");
            ViewData["ProductPrices"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Price);
            ViewData["ProductQuantities"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Quantity);
            ViewData["ProductImages"] = _context.productImages
                .AsNoTracking()
                .GroupBy(pi => pi.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(pi => new { pi.ImageUrl, pi.Isthumbnail }).ToList());

            return View();
        }

        // POST: OrderStaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Quantity,CustomerId,SoldPrice,Status,CreateDate,ReviceDate")] Order order)
        {
            bool hasValidationErrors = false;

            ModelState.Remove("Product");
            ModelState.Remove("ApplicationUser");

            if (order.ProductId <= 0)
            {
                ModelState.AddModelError("ProductId", "Sản phẩm là bắt buộc.");
                hasValidationErrors = true;
            }

            if (order.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng phải lớn hơn 0.");
                hasValidationErrors = true;
            }

            if (string.IsNullOrEmpty(order.CustomerId))
            {
                ModelState.AddModelError("CustomerId", "Khách hàng là bắt buộc.");
                hasValidationErrors = true;
            }

            if (!_context.orderStatuses.ToList().Any(s => s.Id == order.Status))
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ.");
                hasValidationErrors = true;
            }

            if (order.CreateDate == default)
            {
                order.CreateDate = DateTime.Now;
            }

            var product = await _context.products.FindAsync(order.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("ProductId", "Sản phẩm không tồn tại.");
                hasValidationErrors = true;
            }
            else
            {
                var expectedSoldPrice = product.Price * order.Quantity;
                if (order.SoldPrice != expectedSoldPrice)
                {
                    order.SoldPrice = expectedSoldPrice;
                }

                if (product.Quantity < order.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Số lượng sản phẩm không đủ trong kho.");
                    hasValidationErrors = true;
                }


                if (!hasValidationErrors && ModelState.IsValid)
                {
                    try
                    {
                        order.OrderCode = Guid.NewGuid().ToString();
                        product.Quantity -= order.Quantity;
                        _context.Update(product);
                        _context.Add(order);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Đơn hàng đã được tạo thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lưu đơn hàng: {Message}", ex.Message);
                        ModelState.AddModelError("", "Lỗi khi lưu đơn hàng. Vui lòng kiểm tra dữ liệu và thử lại.");
                        hasValidationErrors = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi không mong muốn khi tạo đơn hàng: {Message}", ex.Message);
                        ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
                        hasValidationErrors = true;
                    }
                }
            }
            ViewData["ProductId"] = new SelectList(_context.products.AsNoTracking(), "Id", "ProductName", order.ProductId);
            ViewData["CustomerId"] = new SelectList(_context.Users.AsNoTracking(), "Id", "UserName", order.CustomerId);
            ViewData["StatusList"] = new SelectList(_context.orderStatuses.ToList(), "Id", "Status", order.Status);
            ViewData["ProductPrices"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Price);
            ViewData["ProductQuantities"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Quantity);
            ViewData["ProductImages"] = _context.productImages
                .AsNoTracking()
                .GroupBy(pi => pi.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(pi => new { pi.ImageUrl, pi.Isthumbnail }).ToList());

            return View(order);
        }

        // GET: OrderStaff/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu chỉnh sửa đơn hàng với Id null.");
                return NotFound();
            }

            var order = await _context.orders
                .Include(o => o.Product)
                    .ThenInclude(p => p.Brand)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductType)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Không tìm thấy đơn hàng với Id={Id}", id);
                return NotFound();
            }

            ViewData["ProductId"] = new SelectList(_context.products.AsNoTracking(), "Id", "ProductName", order.ProductId);
            ViewData["CustomerId"] = new SelectList(_context.Users.AsNoTracking(), "Id", "UserName", order.CustomerId);
            ViewData["StatusList"] = new SelectList(_context.orderStatuses.ToList(), "Id", "Status", order.Status);
            ViewData["ProductPrices"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Price);
            ViewData["ProductQuantities"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Quantity);
            ViewData["ProductImages"] = _context.productImages
                .AsNoTracking()
                .GroupBy(pi => pi.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(pi => new { pi.ImageUrl, pi.Isthumbnail }).ToList());

            _logger.LogInformation("Hiển thị trang chỉnh sửa đơn hàng Id={Id}", id);
            return View(order);
        }

        // POST: OrderStaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductId,Quantity,CustomerId,SoldPrice,Status,CreateDate,ReviceDate,OrderCode")] Order order)
        {
            if (id != order.Id)
            {
                _logger.LogWarning("Id không khớp khi chỉnh sửa đơn hàng. Id={Id}, Order.Id={OrderId}", id, order.Id);
                return NotFound();
            }

            bool hasValidationErrors = false;

            ModelState.Remove("Product");
            ModelState.Remove("ApplicationUser");

            if (order.ProductId <= 0)
            {
                ModelState.AddModelError("ProductId", "Sản phẩm là bắt buộc.");
                hasValidationErrors = true;
            }

            if (order.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng phải lớn hơn 0.");
                hasValidationErrors = true;
            }

            if (string.IsNullOrEmpty(order.CustomerId))
            {
                ModelState.AddModelError("CustomerId", "Khách hàng là bắt buộc.");
                hasValidationErrors = true;
            }

            if (!_context.orderStatuses.ToList().Any(s => s.Id == order.Status))
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ.");
                hasValidationErrors = true;
            }

            var product = await _context.products.FindAsync(order.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("ProductId", "Sản phẩm không tồn tại.");
                hasValidationErrors = true;
            }
            else
            {
                var expectedSoldPrice = product.Price * order.Quantity;
                if (order.SoldPrice != expectedSoldPrice)
                {
                    order.SoldPrice = expectedSoldPrice;
                }

                var originalOrder = await _context.orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                int quantityDifference = order.Quantity - originalOrder.Quantity;
                if (quantityDifference > product.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Số lượng sản phẩm không đủ trong kho.");
                    hasValidationErrors = true;
                }
            }

            if (!hasValidationErrors && ModelState.IsValid)
            {
                try
                {
                    var originalOrder = await _context.orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                    int quantityDifference = order.Quantity - originalOrder.Quantity;
                    product.Quantity -= quantityDifference;
                    _context.Update(product);

                    _context.Update(order);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đơn hàng đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Lỗi đồng bộ hóa khi cập nhật đơn hàng Id={Id}: {Message}", id, ex.Message);
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    ModelState.AddModelError("", "Đã xảy ra lỗi đồng bộ hóa. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không mong muốn khi cập nhật đơn hàng Id={Id}: {Message}", id, ex.Message);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật đơn hàng. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
            }

            ViewData["ProductId"] = new SelectList(_context.products.AsNoTracking(), "Id", "ProductName", order.ProductId);
            ViewData["CustomerId"] = new SelectList(_context.Users.AsNoTracking(), "Id", "UserName", order.CustomerId);
            ViewData["StatusList"] = new SelectList(_context.orderStatuses.ToList(), "Id", "Status", order.Status);
            ViewData["ProductPrices"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Price);
            ViewData["ProductQuantities"] = _context.products.AsNoTracking().ToDictionary(p => p.Id, p => p.Quantity);
            ViewData["ProductImages"] = _context.productImages
                .AsNoTracking()
                .GroupBy(pi => pi.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(pi => new { pi.ImageUrl, pi.Isthumbnail }).ToList());

            return View(order);
        }

        // GET: OrderStaff/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Yêu cầu xóa đơn hàng với Id null.");
                return NotFound();
            }

            var order = await _context.orders
                .AsNoTracking()
                .Include(o => o.Product)
                    .ThenInclude(p => p.Brand)
                .Include(o => o.Product)
                    .ThenInclude(p => p.ProductType)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Không tìm thấy đơn hàng với Id={Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Hiển thị trang xác nhận xóa đơn hàng Id={Id}", id);
            return View(order);
        }

        // POST: OrderStaff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var order = await _context.orders.FindAsync(id);
            if (order != null)
            {
                var product = await _context.products.FindAsync(order.ProductId);
                if (product != null)
                {
                    product.Quantity += order.Quantity;
                    _context.Update(product);
                }

                _context.orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Đơn hàng đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(long id)
        {
            return _context.orders.Any(e => e.Id == id);
        }
    }
}