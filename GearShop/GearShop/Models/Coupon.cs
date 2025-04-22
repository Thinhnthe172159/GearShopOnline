using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GearShop.Models
{
    [Table("Coupon")]
    public class Coupon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(100)]
        public string Code { get; set; } = null!;
        public DateTime DateCreate { get; set; }
        public DateTime DateEnd { get; set; }
        public decimal discout { get; set; }
        public int status { get; set; }

    }
}
