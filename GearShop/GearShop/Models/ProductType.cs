using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Identity.Client;

namespace GearShop.Models
{
    public class ProductType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string TypeName { get; set; } = null!;
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? MofifiedBy { get; set; }
        public int Status { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
    }
}
