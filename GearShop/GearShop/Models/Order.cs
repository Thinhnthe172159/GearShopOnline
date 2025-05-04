using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GearShop.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public string CustomerId { get; set; } = null!;
        public decimal SoldPrice { get; set; }
        [MaxLength(100)]
        public string? OrderCode { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreateDate { get; set; } = DateTime.Now;
        // Lý do phản hồi
        public string? ReturnReason { get; set; }
        // Phản hồi của shop
        public string? ResponseReturn { get; set; }
        public DateTime? Requestdate { get; set; }
        public DateTime? ReviceDate { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;
        [ForeignKey(nameof(CustomerId))]
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
