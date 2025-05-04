using GearShop.Controllers;
using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class PaymentController : Controller
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public PaymentController(IConfiguration config, ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _config = config;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreatePayment(string userId, List<long> itemCarts, string? code)
    {
        if (itemCarts == null || !itemCarts.Any())
        {
            TempData["Noti"] = "Bạn chưa chọn sản phẩm nào để thanh toán!";
            return RedirectToAction(nameof(Index), "CustomerCarts", new { Id = userId });
        }
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            var Users = await _context.ApplicationUsers.FindAsync(user.Id);
            if (Users != null)
            {
                if (string.IsNullOrEmpty(Users.City) || string.IsNullOrEmpty(Users.District) || string.IsNullOrEmpty(Users.Commune) || string.IsNullOrEmpty(Users.PhoneNumber))
                {
                    TempData["Noti"] = "Bạn chưa thêm thông tin về địa chỉ và liên lạc";
                    return Redirect("/Identity/Account/Manage");
                }
            }
        }

        decimal amount = 0;
        string orderCode = Guid.NewGuid().ToString();
        decimal discount = 0;

        // Kiểm tra mã giảm giá
        if (!string.IsNullOrEmpty(code))
        {
            var coupon = _context.coupons.FirstOrDefault(c =>
                c.Code == code &&
                c.status == 1 &&
                c.DateCreate < DateTime.Now &&
                DateTime.Now < c.DateEnd);

            if (coupon != null)
            {
                discount = coupon.discout;
            }
        }

        foreach (var item in itemCarts)
        {
            var itemCart = _context.carts
                .Include(c => c.Product)
                .FirstOrDefault(a => a.Id == item);

            if (itemCart != null)
            {
                var order = new Order
                {
                    ProductId = itemCart.ProductId,
                    Quantity = itemCart.Quantity,
                    CustomerId = itemCart.UserId,
                    OrderCode = orderCode,
                    SoldPrice = (itemCart.Product.Price * itemCart.Quantity)
                                - ((discount / 100) * (itemCart.Product.Price * itemCart.Quantity))
                };

                amount += (long)order.SoldPrice;
                _context.orders.Add(order);
                _context.carts.Remove(itemCart);
            }
        }

        await _context.SaveChangesAsync();

        string? vnp_TmnCode = _config["VnPay:TmnCode"];
        string? vnp_HashSecret = _config["VnPay:HashSecret"];
        string? vnp_Url = _config["VnPay:Url"];
        string? vnp_ReturnUrl = _config["VnPay:ReturnUrl"];

        var pay = new VnPayLibrary();
        pay.AddRequestData("vnp_Version", "2.1.0");
        pay.AddRequestData("vnp_Command", "pay");
        pay.AddRequestData("vnp_TmnCode", vnp_TmnCode ?? "");
        pay.AddRequestData("vnp_Amount", (amount * 100L).ToString());
        pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        pay.AddRequestData("vnp_CurrCode", "VND");
        pay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        pay.AddRequestData("vnp_Locale", "vn");
        pay.AddRequestData("vnp_OrderInfo", orderCode);
        pay.AddRequestData("vnp_OrderType", "other");
        pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl?.Trim() ?? "");
        pay.AddRequestData("vnp_TxnRef", orderCode);

        var paymentUrl = pay.CreateRequestUrl(vnp_Url?.Trim() ?? "", vnp_HashSecret?.Trim() ?? "");
        return Redirect(paymentUrl);
    }

    public async Task<IActionResult> PaymentResult()
    {
        var responseData = Request.Query;
        var vnpay = new VnPayLibrary();
        string? orderInfo = responseData["vnp_OrderInfo"];
        if (responseData != null)
        {
            string? transactionStatus = responseData["vnp_TransactionStatus"];
            if (!string.IsNullOrEmpty(orderInfo) && transactionStatus == "00")
            {
                var Orders = _context.orders.Include(o => o.Product).Where(o => o.OrderCode == orderInfo).ToList();
                foreach (var item in Orders)
                {
                    var product = _context.products.Find(item.Product.Id);
                    if (product != null)
                    {
                        product.Quantity -= item.Quantity;
                        _context.products.Update(product);
                    }
                    item.Status = 2;// Trạng Thái thanh toán thành công!
                }
                await _context.SaveChangesAsync();
            }
        }
        return View();
    }


    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> RePayment(long OrderId)
    {
        var orderItem = _context.orders.Find(OrderId);
        decimal amount = 0;
        string orderCode = Guid.NewGuid().ToString();
        if (orderItem != null)
        {
            orderItem.OrderCode = orderCode;
            orderItem.CreateDate = DateTime.Now;
            amount = orderItem.SoldPrice;
            _context.orders.Update(orderItem);
        }
        await _context.SaveChangesAsync();

        string? vnp_TmnCode = _config["VnPay:TmnCode"];
        string? vnp_HashSecret = _config["VnPay:HashSecret"];
        string? vnp_Url = _config["VnPay:Url"];
        string? vnp_ReturnUrl = _config["VnPay:ReturnUrl"];

        var pay = new VnPayLibrary();
        pay.AddRequestData("vnp_Version", "2.1.0");
        pay.AddRequestData("vnp_Command", "pay");
        pay.AddRequestData("vnp_TmnCode", vnp_TmnCode ?? "");
        pay.AddRequestData("vnp_Amount", (amount * 100L).ToString());
        pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        pay.AddRequestData("vnp_CurrCode", "VND");
        pay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        pay.AddRequestData("vnp_Locale", "vn");
        pay.AddRequestData("vnp_OrderInfo", orderCode);
        pay.AddRequestData("vnp_OrderType", "other");
        pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl?.Trim() ?? "");
        pay.AddRequestData("vnp_TxnRef", orderCode);

        var paymentUrl = pay.CreateRequestUrl(vnp_Url?.Trim() ?? "", vnp_HashSecret?.Trim() ?? "");
        return Redirect(paymentUrl);
    }

 
}
