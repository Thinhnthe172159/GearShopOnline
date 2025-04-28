using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Staff")]
    public class ProductTypesStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductTypesStaffController> _logger;
        private const int PageSize = 10;

        public ProductTypesStaffController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<ProductTypesStaffController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: ProductTypesStaff
        public async Task<IActionResult> Index(int? page, string sortOrder)
        {
            int pageNumber = page ?? 1;

            // Store current sort order and page for View
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = pageNumber;

            // Query product types
            var productTypes = _context.productTypes.AsQueryable();

            // Apply sorting
            switch (sortOrder)
            {
                case "name":
                    productTypes = productTypes.OrderBy(pt => pt.TypeName);
                    break;
                case "name_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.TypeName);
                    break;
                case "created_date":
                    productTypes = productTypes.OrderBy(pt => pt.DateTime);
                    break;
                case "created_date_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.DateTime);
                    break;
                case "created_by":
                    productTypes = productTypes.OrderBy(pt => pt.CreatedBy);
                    break;
                case "created_by_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.CreatedBy);
                    break;
                case "modified_date":
                    productTypes = productTypes.OrderBy(pt => pt.ModifiedDate);
                    break;
                case "modified_date_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.ModifiedDate);
                    break;
                case "modified_by":
                    productTypes = productTypes.OrderBy(pt => pt.MofifiedBy);
                    break;
                case "modified_by_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.MofifiedBy);
                    break;
                case "status":
                    productTypes = productTypes.OrderBy(pt => pt.Status);
                    break;
                case "status_desc":
                    productTypes = productTypes.OrderByDescending(pt => pt.Status);
                    break;
                default:
                    productTypes = productTypes.OrderBy(pt => pt.Id);
                    break;
            }

            // Execute query and apply pagination
            var productTypeList = await productTypes.ToListAsync();
            var pagedProductTypes = productTypeList.ToPagedList(pageNumber, PageSize);

            return View(pagedProductTypes);
        }

        // GET: ProductTypesStaff/Create
        public IActionResult Create()
        {
            return View();
        }

        // Helper method to get a unique filename
        private async Task<string> GetUniqueFileName(string originalFileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName);
            string fileName = originalFileName;
            int counter = 0;

            // Check if the filename already exists in the database
            while (await _context.productTypes.AnyAsync(pt => pt.ImageUrl == $"/sourceimg/{fileName}"))
            {
                counter++;
                fileName = $"{fileNameWithoutExtension}_{counter}{extension}";
            }

            return fileName;
        }

        // POST: ProductTypesStaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TypeName,Status")] Models.ProductType productType, IFormFile? ImageFile)
        {
            // Explicitly set CreatedBy to avoid validation error
            productType.CreatedBy = User.Identity?.Name ?? "System";

            // Remove CreatedBy from ModelState to prevent validation errors from form binding
            ModelState.Remove("CreatedBy");

            // Ensure TypeName is not null or empty
            if (string.IsNullOrWhiteSpace(productType.TypeName))
            {
                ModelState.AddModelError("TypeName", "Tên danh mục là bắt buộc.");
            }

            // Check for duplicate TypeName (case-insensitive)
            if (!string.IsNullOrWhiteSpace(productType.TypeName))
            {
                var existingProductType = await _context.productTypes
                    .FirstOrDefaultAsync(pt => pt.TypeName.ToLower() == productType.TypeName.ToLower());
                if (existingProductType != null)
                {
                    ModelState.AddModelError("TypeName", "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Validate Status
            if (productType.Status != 0 && productType.Status != 1)
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ. Vui lòng chọn Đang bán hoặc Không bán.");
            }

            // Validate ImageFile
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", $"Hình ảnh không hợp lệ. Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif.");
                }
                else if (ImageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "Hình ảnh vượt quá kích thước tối đa 10MB.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Get a unique filename (e.g., laptop.jpg, laptop_1.jpg, etc.)
                        var fileName = await GetUniqueFileName(ImageFile.FileName);
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "sourceimg", fileName);

                        // Ensure the directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        // Set ImageUrl with the leading slash
                        productType.ImageUrl = "/sourceimg/" + fileName;
                    }

                    productType.DateTime = DateTime.Now;
                    productType.Status = productType.Status == 0 ? 1 : productType.Status; // Default to active if not set

                    _context.Add(productType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Danh mục đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo danh mục. Vui lòng thử lại.");
                }
            }

            return View(productType);
        }

        // GET: ProductTypesStaff/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productType = await _context.productTypes.FindAsync(id);
            if (productType == null)
            {
                return NotFound();
            }

            return View(productType);
        }

        // POST: ProductTypesStaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TypeName,ImageUrl,Status")] Models.ProductType productType, IFormFile? ImageFile, bool deleteImage = false)
        {
            if (id != productType.Id)
            {
                return NotFound();
            }

            // Fetch the existing ProductType to preserve CreatedBy and set MofifiedBy
            var existingProductType = await _context.productTypes.FindAsync(id);
            if (existingProductType == null)
            {
                return NotFound();
            }

            // Preserve CreatedBy from the existing record
            productType.CreatedBy = existingProductType.CreatedBy;

            // Set MofifiedBy (using the correct property name from the model)
            productType.MofifiedBy = User.Identity?.Name ?? "System";

            // Remove CreatedBy and MofifiedBy from ModelState to prevent validation errors
            ModelState.Remove("CreatedBy");
            ModelState.Remove("MofifiedBy");

            // Validate TypeName
            if (string.IsNullOrWhiteSpace(productType.TypeName))
            {
                ModelState.AddModelError("TypeName", "Tên danh mục là bắt buộc.");
            }

            // Check for duplicate TypeName (case-insensitive), excluding the current ProductType
            if (!string.IsNullOrWhiteSpace(productType.TypeName))
            {
                var existingProductTypeWithSameName = await _context.productTypes
                    .FirstOrDefaultAsync(pt => pt.TypeName.ToLower() == productType.TypeName.ToLower() && pt.Id != productType.Id);
                if (existingProductTypeWithSameName != null)
                {
                    ModelState.AddModelError("TypeName", "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            // Validate Status
            if (productType.Status != 0 && productType.Status != 1)
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ. Vui lòng chọn Đang bán hoặc Không bán.");
            }

            // Validate ImageFile if uploaded
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", $"Hình ảnh không hợp lệ. Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif.");
                }
                else if (ImageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "Hình ảnh vượt quá kích thước tối đa 10MB.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image update or deletion
                    string newImageUrl = existingProductType.ImageUrl;

                    // Only delete the image if the deleteImage checkbox is checked
                    if (deleteImage && !string.IsNullOrEmpty(existingProductType.ImageUrl) && existingProductType.ImageUrl != "/sourceimg/NoImage.jpg")
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProductType.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        newImageUrl = null;
                    }

                    // If a new image is uploaded, save it and update the ImageUrl
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Delete the old image if it exists and isn't the default NoImage.jpg
                        if (!string.IsNullOrEmpty(existingProductType.ImageUrl) && existingProductType.ImageUrl != "/sourceimg/NoImage.jpg")
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProductType.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Get a unique filename (e.g., laptop.jpg, laptop_1.jpg, etc.)
                        var fileName = await GetUniqueFileName(ImageFile.FileName);
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "sourceimg", fileName);

                        // Ensure the directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        // Set ImageUrl with the leading slash
                        newImageUrl = "/sourceimg/" + fileName;
                    }

                    // If no new image is uploaded and the image was deleted, set to default NoImage.jpg
                    if (string.IsNullOrEmpty(newImageUrl))
                    {
                        newImageUrl = "/sourceimg/NoImage.jpg";
                    }
                    else if (!newImageUrl.StartsWith("/"))
                    {
                        // Normalize existing ImageUrl to start with a slash
                        newImageUrl = "/" + newImageUrl;
                    }

                    // Update the product type with the new image URL and other fields
                    existingProductType.TypeName = productType.TypeName;
                    existingProductType.Status = productType.Status;
                    existingProductType.ImageUrl = newImageUrl;
                    existingProductType.ModifiedDate = DateTime.Now;
                    existingProductType.MofifiedBy = productType.MofifiedBy;

                    _context.Update(existingProductType);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductTypeExists(productType.Id))
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
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật danh mục. Vui lòng thử lại.");
                }
            }

            return View(productType);
        }

        // GET: ProductTypesStaff/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productType = await _context.productTypes
                .Include(p => p.Products)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (productType == null)
            {
                return NotFound();
            }

            return View(productType);
        }

        // POST: ProductTypesStaff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productType = await _context.productTypes
                .Include(p => p.Products)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productType == null)
            {
                return NotFound();
            }

            if (productType.Products.Any() || productType.Brands.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục vì vẫn còn sản phẩm hoặc thương hiệu liên kết.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Xóa hình ảnh nếu tồn tại và không phải ảnh mặc định
                if (!string.IsNullOrEmpty(productType.ImageUrl) && productType.ImageUrl != "/sourceimg/NoImage.jpg")
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productType.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.productTypes.Remove(productType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Danh mục đã được xóa thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa danh mục. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductTypeExists(int id)
        {
            return _context.productTypes.Any(e => e.Id == id);
        }
    }
}