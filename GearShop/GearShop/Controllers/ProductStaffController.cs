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
            var applicationDbContext = _context.products.Include(p => p.Brand).Include(p => p.ProductType);
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
            ViewBag.BrandId = new SelectList(_context.brands, "Id", "BrandName");
            ViewBag.ProductTypeId = new SelectList(_context.productTypes, "Id", "TypeName");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product, List<IFormFile> imageFiles)
        {
            bool hasValidationErrors = false;
            List<string> uploadedFileNames = new List<string>();

            // Collect file names for feedback only if there are validation errors
            if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
            {
                uploadedFileNames = imageFiles.Where(f => f != null && f.Length > 0).Select(f => f.FileName).ToList();
            }

            // Validate Description
            if (string.IsNullOrWhiteSpace(product.Description))
            {
                ModelState.AddModelError("Description", "Miêu tả là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate at least one image is uploaded
            if (imageFiles == null || !imageFiles.Any(f => f != null && f.Length > 0))
            {
                ModelState.AddModelError("imageFiles", "Vui lòng tải lên ít nhất một hình ảnh.");
                hasValidationErrors = true;
            }
            else
            {
                // Validate image count
                if (imageFiles.Count(f => f != null && f.Length > 0) > 5)
                {
                    ModelState.AddModelError("imageFiles", "Bạn chỉ có thể tải lên tối đa 5 hình ảnh.");
                    hasValidationErrors = true;
                }

                // Validate image files
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
                    // Save the product
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    // Handle image uploads
                    if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                        // Create the folder if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        for (int i = 0; i < imageFiles.Count; i++)
                        {
                            var imageFile = imageFiles[i];
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                // Use the original file name
                                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                                string extension = Path.GetExtension(imageFile.FileName);
                                string uniqueFileName = fileName + extension;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                // Handle duplicate file names by appending a counter
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
                                    ImageUrl = $"/images/products/{uniqueFileName}",
                                    ProductId = product.Id,
                                    Isthumbnail = i == 0 ? 1 : 0
                                };

                                _context.productImages.Add(productImage);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error saving image files for product");
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

            // Only set ViewData["UploadedFileNames"] if there are image-related validation errors
            if (ModelState.ContainsKey("imageFiles") && ModelState["imageFiles"].Errors.Any())
            {
                ViewData["UploadedFileNames"] = uploadedFileNames;
            }

            // Repopulate dropdowns
            ViewBag.BrandId = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewBag.ProductTypeId = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
            return View(product);
        }


        // GET: ProductStaff/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
            return View(product);
        }

        // POST: ProductStaff/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
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
                return RedirectToAction(nameof(Index));
            }
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
            var product = await _context.products.FindAsync(id);
            if (product != null)
            {
                _context.products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(long id)
        {
            return _context.products.Any(e => e.Id == id);
        }
    }
}
