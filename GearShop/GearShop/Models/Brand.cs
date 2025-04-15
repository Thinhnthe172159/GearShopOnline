using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Identity.Client;

namespace GearShop.Models
{
    public class Brand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string BrandName { get; set; } = null!;
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = null!;
        public DateTime ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int Status { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

