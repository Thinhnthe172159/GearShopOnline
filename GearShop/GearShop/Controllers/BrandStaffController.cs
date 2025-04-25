using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System;
using System.Threading.Tasks;

namespace GearShop.Controllers
{
    public class BrandStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BrandStaffController> _logger;
        private const int PageSize = 10;

        public BrandStaffController(ApplicationDbContext context, ILogger<BrandStaffController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: BrandStaff
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;

            var brands = await _context.brands
                .OrderBy(b => b.Id)
                .ToListAsync();

            var pagedBrands = brands.ToPagedList(pageNumber, PageSize);

            return View(pagedBrands);
        }

        // GET: BrandStaff/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BrandStaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BrandName,Status")] Brand brand)
        {
            brand.CreatedBy = User.Identity?.Name ?? "System";
            ModelState.Remove("CreatedBy");

            if (string.IsNullOrWhiteSpace(brand.BrandName))
            {
                ModelState.AddModelError("BrandName", "Tên thương hiệu là bắt buộc.");
            }

            if (!string.IsNullOrWhiteSpace(brand.BrandName))
            {
                var existingBrand = await _context.brands
                    .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brand.BrandName.ToLower());
                if (existingBrand != null)
                {
                    ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Validate Status
            if (brand.Status != 0 && brand.Status != 1)
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ. Vui lòng chọn Đang hoạt động hoặc Không hoạt động.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    brand.CreateDate = DateTime.Now;
                    brand.Status = brand.Status == 0 ? 1 : brand.Status; // Default to active if not set

                    _context.Add(brand);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thương hiệu đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo thương hiệu. Vui lòng thử lại.");
                }
            }

            return View(brand);
        }

        // GET: BrandStaff/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: BrandStaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BrandName,Status")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            // Fetch the existing Brand to preserve CreatedBy and set ModifiedBy
            var existingBrand = await _context.brands.FindAsync(id);
            if (existingBrand == null)
            {
                return NotFound();
            }

            // Preserve CreatedBy from the existing record
            brand.CreatedBy = existingBrand.CreatedBy;

            // Set ModifiedBy
            brand.ModifiedBy = User.Identity?.Name ?? "System";

            // Remove CreatedBy and ModifiedBy from ModelState to prevent validation errors
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");

            // Validate BrandName
            if (string.IsNullOrWhiteSpace(brand.BrandName))
            {
                ModelState.AddModelError("BrandName", "Tên thương hiệu là bắt buộc.");
            }

            // Check for duplicate BrandName (case-insensitive), excluding the current Brand
            if (!string.IsNullOrWhiteSpace(brand.BrandName))
            {
                var existingBrandWithSameName = await _context.brands
                    .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brand.BrandName.ToLower() && b.Id != brand.Id);
                if (existingBrandWithSameName != null)
                {
                    ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Validate Status
            if (brand.Status != 0 && brand.Status != 1)
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ. Vui lòng chọn Đang hoạt động hoặc Không hoạt động.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the brand with the new fields
                    existingBrand.BrandName = brand.BrandName;
                    existingBrand.Status = brand.Status;
                    existingBrand.ModifiedDate = DateTime.Now;
                    existingBrand.ModifiedBy = brand.ModifiedBy;

                    _context.Update(existingBrand);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thương hiệu đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật thương hiệu. Vui lòng thử lại.");
                }
            }

            return View(brand);
        }

        // GET: BrandStaff/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.brands
                .Include(b => b.Products)
                .Include(b => b.ProductTypes)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: BrandStaff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.brands
                .Include(b => b.Products)
                .Include(b => b.ProductTypes)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            if (brand.Products.Any() || brand.ProductTypes.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa thương hiệu vì vẫn còn sản phẩm hoặc loại sản phẩm liên kết.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.brands.Remove(brand);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thương hiệu đã được xóa thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa thương hiệu. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.brands.Any(e => e.Id == id);
        }
    }
}