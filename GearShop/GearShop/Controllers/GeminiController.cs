using GearShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace GearShop
{
    public class GeminiController : Controller
    {

            private readonly GeminiService _geminiService;

            public GeminiController(GeminiService geminiService)
            {
                _geminiService = geminiService;
            }

            // GET: /Gemini/Index
            public IActionResult Index()
            {
                return View();
            }

            // POST: /Gemini/Generate (for traditional form submission)
            [HttpPost]
            public async Task<IActionResult> Generate(string prompt)
            {
                if (string.IsNullOrEmpty(prompt))
                {
                    ViewBag.Error = "Vui lòng nhập nội dung.";
                    return View("Index");
                }

                try
                {
                    var (result, _) = await _geminiService.GenerateContentAsync(prompt);
                    ViewBag.Result = result;
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Lỗi khi gọi Gemini API: {ex.Message}";
                }

                return View("Index");
            }

            // POST: /Gemini/GenerateAjax (for AJAX requests)
            [HttpPost]
            public async Task<IActionResult> GenerateAjax([FromBody] GenerateRequest request)
            {
                if (request == null || string.IsNullOrEmpty(request.Prompt))
                {
                    return BadRequest(new { success = false, error = "Yêu cầu không hợp lệ hoặc thiếu nội dung." });
                }

                try
                {
                    var (result, imageUrl) = await _geminiService.GenerateContentAsync(request.Prompt);
                    return Ok(new { success = true, result, imageUrl });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, error = $"Lỗi khi gọi Gemini API: {ex.Message}" });
                }
            }
        }

        public class GenerateRequest
        {
            public string Prompt { get; set; }
        }
    }
