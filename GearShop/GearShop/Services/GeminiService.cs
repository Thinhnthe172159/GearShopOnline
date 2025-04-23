using GearShop.Data;
using GearShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

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

        public async Task<(string Response, string? ImageUrl)> GenerateContentAsync(string prompt)
        {
            // Kiểm tra xem prompt có liên quan đến sản phẩm hoặc cửa hàng không
            var productInfo = await GetProductInfoAsync(prompt);
            if (productInfo.HasValue)
            {
                return (productInfo.Value.Response, productInfo.Value.ImageUrl);
            }

            // Nếu không liên quan đến sản phẩm, gọi Gemini API
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
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

            return (generatedText, null);
        }

        private async Task<(string Response, string? ImageUrl)?> GetProductInfoAsync(string prompt)
        {
            prompt = prompt.ToLower().Trim();

  
            string shopContext = "Bạn là trợ lý cho cửa hàng online GearShop. \r\nDữ liệu gồm các bảng sau với ánh xạ tiếng Việt:\r\n- Product (Sản phẩm): Id, ProductName (Tên sản phẩm), BrandId (Mã thương hiệu), ProductTypeId (Mã loại sản phẩm), Quantity (Số lượng), Price (Giá), Description (Mô tả), InServiceDate (Ngày đưa vào sử dụng), InStockDate (Ngày nhập kho).\r\n- Brand (Thương hiệu): Id, BrandName (Tên thương hiệu).\r\n- ProductType (Loại sản phẩm): Id, TypeName (Tên loại sản phẩm).\r\n- ProductImage (Hình ảnh sản phẩm): Id, ImageUrl (Đường dẫn ảnh), ProductId (Mã sản phẩm), Isthumbnail (Ảnh đại diện, 1 là ảnh chính).\r\nTừ câu hỏi tiếng Việt của khách, trả về JSON hợp lệ:\r\n- Nếu là câu hỏi về sản phẩm, trả về điều kiện lọc, ví dụ: { 'Category': 'chuột', 'IsWireless': true, 'MaxPrice': 500000 }.\r\n  - 'Category' ánh xạ với TypeName (Tên loại sản phẩm, ví dụ: 'chuột', 'điện thoại', 'tai nghe').\r\n  - 'MaxPrice' ánh xạ với Price (Giá, ví dụ: 500000).\r\n  - 'IsWireless' tìm trong ProductName hoặc Description chứa 'không dây' hoặc 'wireless'.\r\n  - Nếu có từ khóa 'thương hiệu', lọc theo BrandName (ví dụ: 'Apple', 'Logitech')."; 
            if (prompt.Contains("cửa hàng") || prompt.Contains("shop"))
            {

                shopContext = ExtractShopName(prompt) ?? shopContext;
            }

            // Ánh xạ tiếng Việt sang tiếng Anh
            var keywordMap = new Dictionary<string, string>
            {
                { "sản phẩm", "ProductName" },
                { "thương hiệu", "BrandName" },
                { "loại", "TypeName" },
                { "giá", "Price" },
                { "mô tả", "Description" }
            };

            // Tìm từ khóa liên quan đến sản phẩm
            string searchTerm = prompt;
            string? fieldToSearch = null;

            foreach (var kvp in keywordMap)
            {
                if (prompt.Contains(kvp.Key))
                {
                    fieldToSearch = kvp.Value;
                    // Loại bỏ từ khóa tiếng Việt để lấy giá trị tìm kiếm
                    searchTerm = prompt.Replace(kvp.Key, "").Trim();
                    break;
                }
            }

            // Nếu không tìm thấy từ khóa cụ thể, tìm kiếm chung
            if (string.IsNullOrEmpty(fieldToSearch))
            {
                searchTerm = prompt.Replace("cửa hàng", "").Replace("shop", "").Trim();
            }

            // Tạo truy vấn cơ bản
            var query = _dbContext.products
                .Include(p => p.ProductType)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .AsQueryable();

            // Áp dụng bộ lọc dựa trên fieldToSearch
            if (!string.IsNullOrEmpty(searchTerm))
            {
                switch (fieldToSearch)
                {
                    case "ProductName":
                        query = query.Where(p => EF.Functions.Like(p.ProductName.ToLower(), $"%{searchTerm}%"));
                        break;
                    case "BrandName":
                        query = query.Where(p => EF.Functions.Like(p.Brand.BrandName.ToLower(), $"%{searchTerm}%"));
                        break;
                    case "TypeName":
                        query = query.Where(p => EF.Functions.Like(p.ProductType.TypeName.ToLower(), $"%{searchTerm}%"));
                        break;
                    case "Price":
                        if (decimal.TryParse(searchTerm, out decimal price))
                            query = query.Where(p => p.Price <= price);
                        break;
                    case "Description":
                        query = query.Where(p => p.Description != null && EF.Functions.Like(p.Description.ToLower(), $"%{searchTerm}%"));
                        break;
                    default:
                        // Tìm kiếm chung trên nhiều trường
                        query = query.Where(p => EF.Functions.Like(p.ProductName.ToLower(), $"%{searchTerm}%") ||
                                                EF.Functions.Like(p.Brand.BrandName.ToLower(), $"%{searchTerm}%") ||
                                                EF.Functions.Like(p.ProductType.TypeName.ToLower(), $"%{searchTerm}%"));
                        break;
                }
            }

            var product = await query.FirstOrDefaultAsync();
            if (product == null)
                return null;

            // Lấy ảnh thumbnail (nếu có)
            var thumbnail = product.Images.FirstOrDefault(i => i.Isthumbnail == 1)?.ImageUrl;

            // Tạo phản hồi chi tiết
            var response = $"Cửa hàng: {shopContext}\n" +
                           $"Sản phẩm: {product.ProductName}\n" +
                           $"Thương hiệu: {product.Brand.BrandName}\n" +
                           $"Loại: {product.ProductType.TypeName}\n" +
                           $"Giá: {product.Price:C}\n" +
                           $"Số lượng tồn: {product.Quantity}\n" +
                           $"Mô tả: {product.Description ?? "Không có mô tả"}\n" +
                           $"Ngày nhập kho: {product.InStockDate:dd/MM/yyyy}";

            return (response, thumbnail);
        }

        private string? ExtractShopName(string prompt)
        {
            // Logic để trích xuất tên cửa hàng từ prompt
            // Ví dụ: nếu prompt chứa "shop Apple", trả về "Apple"
            // Hiện tại trả về null vì giả định chỉ có GearShop
            return null;
        }
    }
}
