using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace GearShop.Controllers
{
    [Authorize(Roles = "Staff")]
    public class ProductStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductStaffController> _logger;
        private const int PageSize = 10;

        public ProductStaffController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<ProductStaffController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: ProductStaff
        public async Task<IActionResult> Index(int? page, long? brandId, long? typeId, string sortOrder)
        {
            int pageNumber = page ?? 1;

            // Lưu các tham số để sử dụng trong View
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["SelectedBrand"] = brandId;
            ViewData["SelectedType"] = typeId;

            // Load danh sách thương hiệu và loại sản phẩm cho combobox
            ViewData["Brands"] = new SelectList(_context.brands, "Id", "BrandName");
            ViewData["ProductTypes"] = new SelectList(_context.productTypes, "Id", "TypeName");

            // Query cơ bản
            var products = _context.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .AsQueryable();

            // Lọc theo thương hiệu
            if (brandId.HasValue && brandId > 0)
            {
                products = products.Where(p => p.BrandId == brandId);
            }

            // Lọc theo loại sản phẩm
            if (typeId.HasValue && typeId > 0)
            {
                products = products.Where(p => p.ProductTypeId == typeId);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name":
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "quantity":
                    products = products.OrderBy(p => p.Quantity);
                    break;
                case "quantity_desc":
                    products = products.OrderByDescending(p => p.Quantity);
                    break;
                case "price":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "brand":
                    products = products.OrderBy(p => p.Brand.BrandName);
                    break;
                case "brand_desc":
                    products = products.OrderByDescending(p => p.Brand.BrandName);
                    break;
                case "type":
                    products = products.OrderBy(p => p.ProductType.TypeName);
                    break;
                case "type_desc":
                    products = products.OrderByDescending(p => p.ProductType.TypeName);
                    break;
                case "status":
                    products = products.OrderBy(p => p.Status);
                    break;
                case "status_desc":
                    products = products.OrderByDescending(p => p.Status);
                    break;
                default:
                    products = products.OrderBy(p => p.Id);
                    break;
            }

            var productList = await products.ToListAsync();
            var pagedProducts = productList.ToPagedList(pageNumber, PageSize);

            return View(pagedProducts);
        }

        // GET: ProductStaff/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.products
                .Include(p => p.Images)
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
            // Clear ModelState errors for navigation properties
            ModelState.Remove("Brand");
            ModelState.Remove("ProductType");

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
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    if (imageFiles != null && imageFiles.Any(f => f != null && f.Length > 0))
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product_img");
                        if (!Directory.Exists(uploadsFolder))
                        {
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
                    }

                    TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (IOException)
                {
                    ModelState.AddModelError("imageFiles", "Lỗi khi lưu hình ảnh. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu sản phẩm vào cơ sở dữ liệu. Vui lòng kiểm tra dữ liệu và thử lại.");
                    hasValidationErrors = true;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
                    hasValidationErrors = true;
                }
            }

            // Repopulate dropdowns
            ViewData["BrandId"] = new SelectList(_context.brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(_context.productTypes, "Id", "TypeName", product.ProductTypeId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,ProductName,BrandId,ProductTypeId,Description,Quantity,Price,InServiceDate,InStockDate,CreatedDate,CreatedBy,ModifiedDate,ModifiedBy,Status")] Product product, List<IFormFile> imageFiles, long[] imagesToDelete)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            bool hasValidationErrors = false;

            // Clear ModelState errors for navigation properties
            ModelState.Remove("Brand");
            ModelState.Remove("ProductType");

            // Validate Description
            if (string.IsNullOrWhiteSpace(product.Description))
            {
                ModelState.AddModelError("Description", "Miêu tả là bắt buộc.");
                hasValidationErrors = true;
            }

            // Validate BrandId and ProductTypeId
            if (!product.BrandId.HasValue || product.BrandId <= 0)
            {
                ModelState.AddModelError("BrandId", "Vui lòng chọn một thương hiệu.");
                hasValidationErrors = true;
            }
            if (!product.ProductTypeId.HasValue || product.ProductTypeId <= 0)
            {
                ModelState.AddModelError("ProductTypeId", "Vui lòng chọn một loại sản phẩm.");
                hasValidationErrors = true;
            }

            // Validate Status
            if (product.Status != 0 && product.Status != 1)
            {
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ. Vui lòng chọn Đang bán hoặc Ngừng bán.");
                hasValidationErrors = true;
            }

            // Load Brand and ProductType to satisfy navigation properties
            if (!hasValidationErrors && product.BrandId.HasValue && product.ProductTypeId.HasValue)
            {
                var brand = await _context.brands.FindAsync(product.BrandId.Value);
                var productType = await _context.productTypes.FindAsync(product.ProductTypeId.Value);

                if (brand == null)
                {
                    ModelState.AddModelError("BrandId", "Thương hiệu không tồn tại.");
                    hasValidationErrors = true;
                }
                else
                {
                    product.Brand = brand;
                }

                if (productType == null)
                {
                    ModelState.AddModelError("ProductTypeId", "Loại sản phẩm không tồn tại.");
                    hasValidationErrors = true;
                }
                else
                {
                    product.ProductType = productType;
                }
            }

            // Validate image files and count
            var existingImages = await _context.productImages.Where(pi => pi.ProductId == product.Id).ToListAsync();
            var newImageCount = imageFiles?.Count(f => f != null && f.Length > 0) ?? 0;
            var remainingImageCount = existingImages.Count - (imagesToDelete?.Length ?? 0);

            if (remainingImageCount + newImageCount > 5)
            {
                ModelState.AddModelError("imageFiles", "Bạn chỉ có thể có tối đa 5 hình ảnh.");
                hasValidationErrors = true;
            }

            // Validate new image files
            if (newImageCount > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB

                foreach (var imageFile in imageFiles.Where(f => f != null && f.Length > 0))
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

            if (!hasValidationErrors && ModelState.IsValid)
            {
                try
                {
                    // Update product information
                    product.ModifiedDate = DateTime.Now;
                    product.ModifiedBy = User.Identity?.Name ?? "Anonymous";
                    _context.Update(product);

                    // Handle image deletion
                    if (imagesToDelete != null && imagesToDelete.Any())
                    {
                        foreach (var imageId in imagesToDelete)
                        {
                            var image = existingImages.FirstOrDefault(pi => pi.Id == imageId);
                            if (image != null)
                            {
                                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }
                                _context.productImages.Remove(image);
                            }
                        }
                    }

                    // Handle new image uploads
                    if (newImageCount > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "product_img");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        bool hasThumbnail = existingImages.Any(pi => pi.Isthumbnail == 1 && !imagesToDelete.Contains(pi.Id));
                        int newImageIndex = 0;

                        foreach (var imageFile in imageFiles.Where(f => f != null && f.Length > 0))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                            string extension = Path.GetExtension(imageFile.FileName);
                            string uniqueFileName = $"{fileName}{extension}";
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            int counter = 1;
                            while (System.IO.File.Exists(filePath))
                            {
                                uniqueFileName = $"{fileName}_{counter}{extension}";
                                filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                counter++;
                            }

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }

                            var productImage = new ProductImage
                            {
                                ImageUrl = $"/product_img/{uniqueFileName}",
                                ProductId = product.Id,
                                Isthumbnail = (!hasThumbnail && newImageIndex == 0) ? 1 : 0
                            };
                            _context.productImages.Add(productImage);
                            newImageIndex++;
                        }
                    }

                    // Update thumbnail if necessary
                    if (existingImages.Any(pi => pi.Isthumbnail == 1 && imagesToDelete.Contains(pi.Id)))
                    {
                        var newThumbnail = await _context.productImages
                            .Where(pi => pi.ProductId == product.Id && !imagesToDelete.Contains(pi.Id))
                            .OrderBy(pi => pi.Id)
                            .FirstOrDefaultAsync();
                        if (newThumbnail != null)
                        {
                            newThumbnail.Isthumbnail = 1;
                            _context.Update(newThumbnail);
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
                    ModelState.AddModelError("", "Đã xảy ra lỗi đồng bộ hóa. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.");
                    hasValidationErrors = true;
                }
            }

            // Repopulate dropdowns and return view with errors
            var brands = await _context.brands.ToListAsync();
            var productTypes = await _context.productTypes.ToListAsync();
            ViewData["BrandId"] = new SelectList(brands, "Id", "BrandName", product.BrandId);
            ViewData["ProductTypeId"] = new SelectList(productTypes, "Id", "TypeName", product.ProductTypeId);
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
                .Include(p => p.Images)
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