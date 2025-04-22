using GearShop.Data;
using Microsoft.AspNetCore.Mvc;

public class PaymentController : Controller
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;

    public PaymentController(IConfiguration config, ApplicationDbContext context)
    {
        _config = config;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CreatePayment(string userId,List<int> itemCarts,string orderId, decimal amount)
    {
        string vnp_TmnCode = _config["VnPay:TmnCode"];
        string vnp_HashSecret = _config["VnPay:HashSecret"];
        string vnp_Url = _config["VnPay:Url"];
        string vnp_ReturnUrl = _config["VnPay:ReturnUrl"];

        var pay = new VnPayLibrary();
        pay.AddRequestData("vnp_Version", "2.1.0");
        pay.AddRequestData("vnp_Command", "pay");
        pay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
        pay.AddRequestData("vnp_Amount", ((int)(amount * 100)).ToString());
        pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        pay.AddRequestData("vnp_CurrCode", "VND");
        pay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString());
        pay.AddRequestData("vnp_Locale", "vn");
        pay.AddRequestData("vnp_OrderInfo", $"{orderId}");
        pay.AddRequestData("vnp_OrderType", "other");
        pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
        pay.AddRequestData("vnp_TxnRef", orderId);

        var paymentUrl = pay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        return Redirect(paymentUrl);
    }

    public IActionResult PaymentResult()
    {
        return View();
    }
}
