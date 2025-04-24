using GearShop.Data;
using GearShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace GearShop.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _dbContext;

        public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> settings, ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            _apiKey = settings.Value.ApiKey;
            _dbContext = dbContext;
        }

        public async Task<(string HtmlResponse, string? ImageUrl)> GenerateContentAsync(string prompt)
        {
            prompt = prompt.Trim();

            // Try to extract JSON filter for GearShop-specific queries
            var jsonFilter = await ExtractFilterFromPromptAsync(prompt);
            if (jsonFilter.HasValue)
            {
                return await GetProductInfoFromFilter(jsonFilter.Value);
            }

            // Check if prompt is related to GearShop
            if (prompt.ToLower().Contains("gearshop") || prompt.ToLower().Contains("sản phẩm") ||
                prompt.ToLower().Contains("thương hiệu") || prompt.ToLower().Contains("loại"))
            {
                var productInfo = await GetProductInfoAsync(prompt);
                if (productInfo.HasValue)
                {
                    return (productInfo.Value.HtmlResponse, productInfo.Value.ImageUrl);
                }
            }

            // Handle non-GearShop queries with a general prompt
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var generalPrompt = $@"Bạn là một trợ lý AI thân thiện và thông minh, có thể trả lời các câu hỏi đa dạng bằng tiếng Việt. Cung cấp câu trả lời chính xác, hữu ích và tự nhiên. Nếu câu hỏi liên quan đến GearShop, hãy lưu ý rằng bạn có thể truy cập thông tin sản phẩm, thương hiệu và loại sản phẩm từ cửa hàng GearShop, nhưng câu hỏi hiện tại không yêu cầu thông tin này.

Câu hỏi: {prompt}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = generalPrompt } }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var generatedText = jsonResponse
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            // Convert plain text response to HTML
            var htmlResponse = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết quả từ GearShop</title>
    <script src=""https://cdn.tailwindcss.com""></script>
</head>
<body class=""bg-gray-100 font-sans"">
    <div class=""container mx-auto p-6"">
        <div class=""bg-white rounded-lg shadow-md p-6"">
            <h1 class=""text-2xl font-bold mb-4 text-light"">Gear Shop AI</h1>
            <p class=""text-gray-700 whitespace-pre-wrap"">{System.Web.HttpUtility.HtmlEncode(generatedText)}</p>
        </div>
    </div>
</body>
</html>";

            return (htmlResponse, null);
        }

        private async Task<(string HtmlResponse, string? ImageUrl)?> GetProductInfoAsync(string prompt)
        {
            prompt = prompt.ToLower().Trim();

            // Fetch all data from Product, ProductType, and Brand tables
            var products = await _dbContext.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .Include(p => p.Images) // Ensure Images are included
                .ToListAsync();
            var productStrings = products.Select(p => p.ToString()).ToList();

            var productImages = await _dbContext.productImages.ToListAsync();
            var productImageString = productImages.Select(p => p.ToString()).ToList();

            var productTypes = await _dbContext.productTypes.ToListAsync();
            var productTypeStrings = productTypes.Select(pt => pt.ToString()).ToList();

            var brands = await _dbContext.brands.ToListAsync();
            var brandStrings = brands.Select(b => b.ToString()).ToList();

            // Combine data into a structured format for the prompt
            var dataContext = $@"Danh sách dữ liệu GearShop:

1. Sản phẩm:
{string.Join("\n", productStrings)}

2. Loại sản phẩm:
{string.Join("\n", productTypeStrings)}

3. Thương hiệu:
{string.Join("\n", brandStrings)}

4. Ảnh sản phẩm:
{string.Join("\n", productImageString)}
";

            // Define the system prompt with GearShop context and data
            string shopContext = $@"Bạn là trợ lý cho cửa hàng online GearShop. Nhiệm vụ chính của bạn là hỗ trợ khách hàng bằng cách cung cấp thông tin chính xác về sản phẩm, thương hiệu và loại sản phẩm dựa trên dữ liệu cửa hàng. Trả lời bằng tiếng Việt, thân thiện và chuyên nghiệp.

Dữ liệu được cung cấp dưới dạng chuỗi từ các bảng sau, sử dụng phương thức ToString:
- Product (Sản phẩm): Định dạng 'Sản phẩm [Mã sản phẩm={{Id}}, Tên sản phẩm={{ProductName}}, Thương hiệu={{Brand?.BrandName}}, Loại sản phẩm={{ProductType?.TypeName}}, Mô tả={{Description}}, Số lượng={{Quantity}}, Giá={{Price}}]'
- ProductType (Loại sản phẩm): Định dạng 'Loại sản phẩm [Mã loại={{Id}}, Tên loại={{TypeName}}, Ngày tạo={{DateTime:dd/MM/yyyy}}, Người tạo={{CreatedBy}}, Hình ảnh={{ImageUrl}}, Ngày chỉnh sửa={{ModifiedDate:dd/MM/yyyy}}, Người chỉnh sửa={{MofifiedBy}}, Trạng thái={{Status}}]'
- Brand (Thương hiệu): Định dạng 'Thương hiệu [Mã thương hiệu={{Id}}, Tên thương hiệu={{BrandName}}, Ngày tạo={{CreateDate:dd/MM/yyyy}}, Người tạo={{CreatedBy}}, Ngày chỉnh sửa={{ModifiedDate:dd/MM/yyyy}}, Người chỉnh sửa={{ModifiedBy}}, Trạng thái={{Status}}]'
- ProductImage (Ảnh sản phẩm): Định dạng 'Ảnh sản phẩm [[Mã ảnh={{Id}}, Đường dẫn ảnh={{ImageUrl}}, Mã sản phẩm={{ProductId}}]'
{dataContext}

Hướng dẫn:
1. Phân tích câu hỏi để xác định nếu liên quan đến sản phẩm, thương hiệu hoặc loại sản phẩm.
2. Lọc dữ liệu từ danh sách trên dựa trên câu hỏi:
   - Tìm kiếm sản phẩm: Tìm trong ProductName, Description, BrandName, hoặc TypeName.
   - Tìm kiếm thương hiệu: Tìm trong BrandName và liệt kê sản phẩm liên quan.
   - Tìm kiếm loại sản phẩm: Tìm trong TypeName và liệt kê sản phẩm liên quan.
   - Áp dụng bộ lọc như giá (giá <= X), không dây (chứa 'không dây' hoặc 'wireless').
3. Trả lời với định dạng:
   - Nếu tìm thấy: Liệt kê chuỗi ToString của các bản ghi phù hợp.
   - Nếu không tìm thấy: 'Không tìm thấy sản phẩm, thương hiệu hoặc loại sản phẩm phù hợp.'
4. Nếu câu hỏi không rõ, yêu cầu làm rõ: 'Bạn muốn tìm sản phẩm, thương hiệu hay loại sản phẩm nào? Vui lòng cung cấp thêm thông tin.'

Câu hỏi: {prompt}";

            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = shopContext } }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var generatedText = jsonResponse
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            // Extract thumbnail if response references a product
            string? thumbnail = null;
            if (products.Any() && generatedText.Contains("Sản phẩm"))
            {
                var firstProduct = products.FirstOrDefault(p => generatedText.Contains(p.ProductName));
                if (firstProduct != null)
                {
                    var thumbnailImage = firstProduct.Images.FirstOrDefault(i => i.Isthumbnail == 1);
                    if (thumbnailImage != null && !string.IsNullOrEmpty(thumbnailImage.ImageUrl))
                    {
                        thumbnail = thumbnailImage.ImageUrl;
                        // Optionally validate the URL
                        if (!Uri.IsWellFormedUriString(thumbnail, UriKind.Absolute))
                        {
                            // If the URL is relative, prepend a base URL (adjust as needed)
                            thumbnail = $"{thumbnail.TrimStart('/')}";
                        }
                    }
                }
            }

            // Convert response to HTML
            var htmlResponse = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết quả từ GearShop</title>
    <script src=""https://cdn.tailwindcss.com""></script>
</head>
<body class=""bg-gray-100 font-sans"">
    <div class=""container mx-auto p-6"">
        <div class=""bg-white rounded-lg shadow-md p-6"">
            <h1 class=""text-2xl font-bold mb-4 text-gray-800"">Kết quả tìm kiếm GearShop</h1>
            {(thumbnail != null ? $@"<img src=""{thumbnail}"" alt=""Hình ảnh sản phẩm"" class=""w-full max-w-xs rounded-lg mb-4 object-cover"" onerror=""this.style.display='none'"">" : "<p class=\"text-gray-700\">Không có hình ảnh sản phẩm.</p>")}
            <div class=""text-gray-700 whitespace-pre-wrap"">{System.Web.HttpUtility.HtmlEncode(generatedText)}</div>
        </div>
    </div>
</body>
</html>";

            return (htmlResponse, thumbnail);
        }

        private async Task<(string HtmlResponse, string? ImageUrl)> GetProductInfoFromFilter(JsonElement json)
        {
            var query = _dbContext.products
                .Include(p => p.Brand)
                .Include(p => p.ProductType)
                .Include(p => p.Images)
                .AsQueryable();

            if (json.TryGetProperty("Category", out var category))
            {
                var categoryStr = category.GetString()?.ToLower();
                query = query.Where(p => p.ProductType.TypeName.ToLower().Contains(categoryStr));
            }

            if (json.TryGetProperty("MaxPrice", out var maxPrice))
            {
                if (maxPrice.TryGetDecimal(out decimal max))
                    query = query.Where(p => p.Price <= max);
            }

            if (json.TryGetProperty("IsWireless", out var wireless) && wireless.GetBoolean())
            {
                query = query.Where(p => p.ProductName.ToLower().Contains("không dây") ||
                                         p.Description.ToLower().Contains("không dây") ||
                                         p.ProductName.ToLower().Contains("wireless") ||
                                         p.Description.ToLower().Contains("wireless"));
            }

            if (json.TryGetProperty("Brand", out var brand))
            {
                var brandStr = brand.GetString()?.ToLower();
                query = query.Where(p => p.Brand.BrandName.ToLower().Contains(brandStr));
            }

            var result = await query.FirstOrDefaultAsync();
            if (result == null)
            {
                var htmlResponse = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết quả từ GearShop</title>
    <script src=""https://cdn.tailwindcss.com""></script>
</head>
<body class=""bg-gray-100 font-sans"">
    <div class=""container mx-auto p-6"">
        <div class=""bg-white rounded-lg shadow-md p-6"">
            <h1 class=""text-2xl font-bold mb-4 text-gray-800"">Kết quả tìm kiếm</h1>
            <p class=""text-gray-700"">Không tìm thấy sản phẩm phù hợp.</p>
        </div>
    </div>
</body>
</html>";
                return (htmlResponse, null);
            }

            var image = result.Images.FirstOrDefault(i => i.Isthumbnail == 1)?.ImageUrl;
            if (!string.IsNullOrEmpty(image) && !Uri.IsWellFormedUriString(image, UriKind.Absolute))
            {
                // If the URL is relative, prepend a base URL (adjust as needed)
                image = $"https://your-base-url/{image.TrimStart('/')}";
            }

            var htmlContent = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết quả từ GearShop</title>
    <script src=""https://cdn.tailwindcss.com""></script>
</head>
<body class=""bg-gray-100 font-sans"">
    <div class=""container mx-auto p-6"">
        <div class=""bg-white rounded-lg shadow-md p-6"">
            <h1 class=""text-2xl font-bold mb-4 text-gray-800"">Thông tin sản phẩm</h1>
            {(image != null ? $@"<img src=""{image}"" alt=""{System.Web.HttpUtility.HtmlEncode(result.ProductName)}"" class=""w-full max-w-xs rounded-lg mb-4 object-cover"" onerror=""this.style.display='none'"">" : "<p class=\"text-gray-700\">Không có hình ảnh sản phẩm.</p>")}
            <h2 class=""text-xl font-semibold text-gray-800"">{System.Web.HttpUtility.HtmlEncode(result.ProductName)}</h2>
            <p class=""text-gray-700"">Giá: {result.Price:C}</p>
            <p class=""text-gray-700"">Thương hiệu: {System.Web.HttpUtility.HtmlEncode(result.Brand.BrandName)}</p>
            <p class=""text-gray-700"">Loại: {System.Web.HttpUtility.HtmlEncode(result.ProductType.TypeName)}</p>
        </div>
    </div>
</body>
</html>";

            return (htmlContent, image);
        }

        private async Task<JsonElement?> ExtractFilterFromPromptAsync(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"Hãy phân tích câu hỏi tiếng Việt và trả về JSON với các thuộc tính sau nếu có: " +
                                       $"Category (loại sản phẩm), MaxPrice (giá tối đa), IsWireless (true nếu sản phẩm là không dây), Brand (thương hiệu).\n\n" +
                                       $"Ví dụ đầu ra: {{ \"Category\": \"chuột\", \"MaxPrice\": 500000, \"IsWireless\": true }}\n\nCâu hỏi: {prompt}"
                            }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var text = json.GetProperty("candidates")[0]
                           .GetProperty("content")
                           .GetProperty("parts")[0]
                           .GetProperty("text")
                           .GetString();

            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(text);
                return parsed;
            }
            catch
            {
                return null;
            }
        }
    }
}