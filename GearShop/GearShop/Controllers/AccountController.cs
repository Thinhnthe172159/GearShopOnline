using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using GearShop.Models;
using Microsoft.AspNetCore.Authentication;
using System;

namespace GearShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {

            // Xử lý lỗi từ nhà cung cấp (ví dụ: người dùng từ chối đăng nhập)
            if (remoteError != null)
            {
                TempData["Error"] = remoteError == "access_denied"
                    ? "Bạn đã hủy đăng nhập bằng Facebook. Vui lòng thử lại hoặc sử dụng phương thức khác."
                    : $"Lỗi từ Facebook: {remoteError}. Vui lòng thử lại.";
                return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = returnUrl });
            }

            // Lấy thông tin đăng nhập từ nhà cung cấp
            ExternalLoginInfo info = null;
            try
            {
                info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    TempData["Noti"] = "Không thể lấy thông tin đăng nhập từ Facebook. Vui lòng thử lại.";
                    return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = returnUrl });
                }
            }
            catch (AuthenticationFailureException ex)
            {
                TempData["Noti"] = "Đăng nhập bằng Facebook thất bại. Vui lòng thử lại.";
                return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                TempData["Noti"] = $"Đã xảy ra lỗi: {ex.Message}. Vui lòng thử lại.";
                return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = returnUrl });
            }

            // Thử đăng nhập với thông tin bên ngoài
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return returnUrl != null ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
            }

            // Nếu người dùng chưa có tài khoản, tạo mới
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };
            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return returnUrl != null ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
            }

            // Nếu tạo tài khoản thất bại, hiển thị lỗi
            TempData["Noti"] = "Không thể tạo tài khoản. Vui lòng thử lại.";
            foreach (var error in createResult.Errors)
            {
                TempData["Noti"] += $" {error.Description}";
            }
            return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = returnUrl });
        }
    }
}