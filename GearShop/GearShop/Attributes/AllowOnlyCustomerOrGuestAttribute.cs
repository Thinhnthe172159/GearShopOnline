using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public class AllowOnlyCustomerOrGuestAttribute : TypeFilterAttribute
{
    public AllowOnlyCustomerOrGuestAttribute() : base(typeof(AllowOnlyCustomerOrGuestFilter))
    {
    }

    private class AllowOnlyCustomerOrGuestFilter : IAsyncAuthorizationFilter
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AllowOnlyCustomerOrGuestFilter(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity.IsAuthenticated)
            {
                var identityUser = await _userManager.GetUserAsync(user);
                var roles = await _userManager.GetRolesAsync(identityUser);

                if (roles.Contains("Admin") || roles.Contains("Staff"))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                }
            }
            // else: guest => cho vào
        }
    }
}
