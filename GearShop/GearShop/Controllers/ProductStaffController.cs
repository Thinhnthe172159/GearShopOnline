using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace GearShop.Controllers
{
    public class ProductStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductStaffController> _logger;

        public ProductStaffController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<ProductStaffController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: ProductStaff
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ProductStaff/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: ProductStaff/Create
        public IActionResult Create()
        {
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName");
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName");
            return View();
        }

        public Product? GetLastestProduct(string User)
        {
            var product = _context.products
                .Where(p => p.CreatedBy == User)
                .OrderByDescending(p => p.CreatedDate)
                .FirstOrDefault();
            return product;
        }

        // POST: ProductStaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product, List<IFormFile> imageFiles)
        {
            _logger.LogInformation("Create action called for product: {ProductName}", product.ProductName);
            _logger.LogInformation("Submitted BrandId: {BrandId}, ProductTypeId: {ProductTypeId}", product.BrandId, product.ProductTypeId);

            // Log raw form data
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form data: {Key} = {Value}", key, Request.Form[key]);
            }

            // Log bound product object
            _logger.LogInformation("Bound Product: {Product}", System.Text.Json.JsonSerializer.Serialize(product));

            // Clear ModelState errors for navigation properties
            ModelState.Remove("Brand");
            ModelState.Remove("ProductType");

            // Log all ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToList();
                foreach (var error in errors)
                {
                    foreach (var err in error.Errors)
                    {
                        _logger.LogWarning("ModelState error for {Key}: {ErrorMessage}", error.Key, err.ErrorMessage);
                    }
                }
            }

            bool hasValidationErrors = false;

            // Validate ProductName
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm là bắt buộc.");
                hasValidationErrors = true;
            }
            else if (product.ProductName.Length > 350)
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm không được vượt quá 350 ký tự.");
                hasValidationErrors = true;
            }

            // Validate BrandId
            if (product.BrandId <= 0)
            {
                ModelState.AddModelError("BrandId", "Thương hiệu là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate ProductTypeId
            if (product.ProductTypeId <= 0)
            {
                ModelState.AddModelError("ProductTypeId", "Loại sản phẩm là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate Description
            if (string.IsNullOrWhiteSpace(product.Description))
            {
                ModelState.AddModelError("Description", "Miêu tả là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate Quantity
            if (product.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng phải lớn hơn 0.");
                hasValidationErrors = true;
            }

            // Validate Price
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá bán phải lớn hơn 0.");
                hasValidationErrors = true;
            }

            // Validate Dates
            if (product.InServiceDate == default)
            {
                ModelState.AddModelError("InServiceDate", "Ngày nhập hàng là bắt buộc.");
                hasValidationErrors = true;
            }

            if (product.InStockDate == default)
            {
                ModelState.AddModelError("InStockDate", "Ngày mở bán là bắt buộc.");
                hasValidationErrors = true;
            }
            else if (product.InStockDate < product.InServiceDate)
            {
                ModelState.AddModelError("InStockDate", "Ngày mở bán phải sau hoặc bằng ngày nhập hàng.");
                hasValidationErrors = true;
            }

            // Validate CreatedBy
            if (string.IsNullOrEmpty(product.CreatedBy))
            {
                product.CreatedBy = User.Identity?.Name ?? "Anonymous";
            }

            // Validate CreatedDate
            if (product.CreatedDate == default)
            {
                product.CreatedDate = DateTime.Now;
            }

            // Validate Status
            if (product.Status == 0)
            {
                product.Status = 1;
            }

            // Validate image files
            if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
            {
                if (imageFiles.Count(f => f != null && f.Length > 0) > 5)
                {
                    ModelState.AddModelError("imageFiles", "Bạn chỉ có thể tải lên tối đa 5 hình ảnh.");
                    hasValidationErrors = true;
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB

                foreach (var imageFile in imageFiles)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("imageFiles", $"Hình ảnh {imageFile.FileName} không hợp lệ. Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif.");
                            hasValidationErrors = true;
                        }
                        if (imageFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError("imageFiles", $"Hình ảnh {imageFile.FileName} vượt quá kích thước tối đa 10MB.");
                            hasValidationErrors = true;
                        }
                    }
                }
            }

            if (!hasValidationErrors && ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Saving product to database: {ProductName}", product.ProductName);
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Product saved successfully with ID: {ProductId}", product.Id);

                    if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product_img");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            _logger.LogInformation("Creating uploads folder: {UploadsFolder}", uploadsFolder);
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        for (int i = 0; i < imageFiles.Count; i++)
                        {
                            var imageFile = imageFiles[i];
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                                string extension = Path.GetExtension(imageFile.FileName);
                                string uniqueFileName = fileName + extension;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                int counter = 1;
                                while (System.IO.File.Exists(filePath))
                                {
                                    uniqueFileName = $"{fileName}_{counter}{extension}";
                                    filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                    counter++;
                                }

                                _logger.LogInformation("Saving image: {FilePath}", filePath);
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(fileStream);
                                }

                                var productImage = new ProductImage
                                {
                                    ImageUrl = $"/product_img/{uniqueFileName}",
                                    ProductId = product.Id,
                                    Isthumbnail = i == 0 ? 1 : 0
                                };

                                _context.productImages.Add(productImage);
                            }
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Images saved successfully for product ID: {ProductId}", product.Id);
                    }

                    TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                    _logger.LogInformation("Redirecting to Index after successful creation");
                    return RedirectToAction(nameof(Index));
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error saving image files for product ID: {ProductId}", product.Id);
                    ModelState.AddModelError("imageFiles", "Lỗi khi lưu hình ảnh. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error while creating product");
                    ModelState.AddModelError("", "Lỗi khi lưu sản phẩm vào cơ sở dữ liệu. Vui lòng kiểm tra dữ liệu và thử lại.");
                    hasValidationErrors = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while creating product");
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
                    hasValidationErrors = true;
                }
            }

            // Repopulate dropdowns
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
            _logger.LogInformation("Returning Create view with validation errors");
            return View(product);
        }

        // GET: ProductStaff/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
            return View(product);
        }

        // POST: ProductStaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product, List<IFormFile> imageFiles, long[] imagesToDelete)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            bool hasValidationErrors = false;

            // Validate Description
            if (string.IsNullOrWhiteSpace(product.Description))
            {
                ModelState.AddModelError("Description", "Miêu tả là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate image files if provided
            if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
            {
                var existingImages = await _context.productImages.Where(pi => pi.ProductId == product.Id).CountAsync();
                var newImageCount = imageFiles.Count(f => f != null && f.Length > 0);
                if (existingImages - imagesToDelete.Length + newImageCount > 5)
                {
                    ModelState.AddModelError("imageFiles", "Bạn chỉ có thể có tối đa 5 hình ảnh.");
                    hasValidationErrors = true;
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB

                foreach (var imageFile in imageFiles)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("imageFiles", $"Hình ảnh {imageFile.FileName} không hợp lệ. Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif.");
                            hasValidationErrors = true;
                        }
                        if (imageFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError("imageFiles", $"Hình ảnh {imageFile.FileName} vượt quá kích thước tối đa 10MB.");
                            hasValidationErrors = true;
                        }
                    }
                }
            }

            if (!hasValidationErrors && ModelState.IsValid)
            {
                try
                {
                    // Update product
                    product.ModifiedDate = DateTime.Now;
                    product.ModifiedBy = User.Identity?.Name;
                    _context.Update(product);

                    // Handle image deletion
                    var existingImages = await _context.productImages
                        .Where(pi => pi.ProductId == product.Id)
                        .OrderBy(pi => pi.Id)
                        .ToListAsync();
                    var deletedIndices = new List<int>();
                    bool thumbnailDeleted = false;
                    if (imagesToDelete != null && imagesToDelete.Any())
                    {
                        for (int i = 0; i < existingImages.Count; i++)
                        {
                            if (imagesToDelete.Contains(existingImages[i].Id))
                            {
                                deletedIndices.Add(i);
                                var image = existingImages[i];
                                if (image.Isthumbnail == 1)
                                {
                                    thumbnailDeleted = true;
                                }
                                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }
                                _context.productImages.Remove(image);
                            }
                        }
                    }

                    // Add new images at deleted indices
                    if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product_img");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        bool hasThumbnail = existingImages.Any(pi => pi.Isthumbnail == 1 && !imagesToDelete.Contains(pi.Id));
                        int currentIndex = 0;
                        int newImageIndex = 0;

                        foreach (var imageFile in imageFiles)
                        {
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                // Determine the index for the new image
                                int targetIndex;
                                if (deletedIndices.Any() && currentIndex < deletedIndices.Count)
                                {
                                    targetIndex = deletedIndices[currentIndex];
                                    currentIndex++;
                                }
                                else
                                {
                                    targetIndex = existingImages.Count + newImageIndex;
                                }
                                newImageIndex++;

                                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                                string extension = Path.GetExtension(imageFile.FileName);
                                string uniqueFileName = fileName + extension;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                // Handle duplicate file names
                                int counter = 1;
                                while (System.IO.File.Exists(filePath))
                                {
                                    uniqueFileName = $"{fileName}_{counter}{extension}";
                                    filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                    counter++;
                                }

                                // Save the file
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(fileStream);
                                }

                                // Save image record to database
                                var productImage = new ProductImage
                                {
                                    ImageUrl = $"/product_img/{uniqueFileName}",
                                    ProductId = product.Id,
                                    Isthumbnail = (!hasThumbnail && targetIndex == 0) || (thumbnailDeleted && targetIndex == 0) ? 1 : 0
                                };

                                _context.productImages.Add(productImage);
                            }
                        }
                    }

                    // If thumbnail was deleted and no new images were added, set a new thumbnail
                    if (thumbnailDeleted && !imageFiles.Any(f => f != null && f.Length > 0))
                    {
                        var remainingImages = await _context.productImages
                            .Where(pi => pi.ProductId == product.Id && !imagesToDelete.Contains(pi.Id))
                            .OrderBy(pi => pi.Id)
                            .FirstOrDefaultAsync();
                        if (remainingImages != null)
                        {
                            remainingImages.Isthumbnail = 1;
                            _context.Update(remainingImages);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error handling image files for product");
                    ModelState.AddModelError("imageFiles", "Lỗi khi xử lý hình ảnh. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while updating product");
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
                    hasValidationErrors = true;
                }
            }

            // Repopulate dropdowns
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
            return View(product);
        }

        // GET: ProductStaff/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: ProductStaff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var product = await _context.products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                // Delete associated images from storage
                foreach (var image in product.Images)
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.products.Remove(product);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(long id)
        {
            return _context.products.Any(e => e.Id == id);
        }
    }
}