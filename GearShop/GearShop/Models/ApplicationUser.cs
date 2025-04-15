using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace GearShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Commune { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public string? ResetCode { get; set; }
        public DateTime? DateTimeGetCode { get; set; }
        public int status { get; set; }

    }
}
