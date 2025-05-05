

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using GearShop.Data;
using GearShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GearShop.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Nhập số điện thoại")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Nhập họ và tên")]
            public string? FullName { get; set; }

            [Required]
            [Display(Name = "Chọn thành phố")]
            public string? City { get; set; }

            [Required]
            [Display(Name = "Chọn quận huyện")]
            public string? District { get; set; }

            [Required]
            [Display(Name = "Chọn xã phường")]
            public string? Commune { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var appUser = await _context.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == user.Id);

            if (appUser != null && userName != null)
            {
                Username = userName;
                Input = new InputModel
                {
                    PhoneNumber = phoneNumber,
                    City = appUser.City,
                    District = appUser.District,
                    Commune = appUser.Commune,
                    FullName = appUser.FullName
                };
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update ApplicationUsers table
            var appUser = await _context.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == user.Id);
            if (appUser != null)
            {
                appUser.City = Input.City;
                appUser.District = Input.District;
                appUser.Commune = Input.Commune;
                appUser.FullName = Input.FullName;
                _context.ApplicationUsers.Update(appUser);
                await _context.SaveChangesAsync();
            }
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
