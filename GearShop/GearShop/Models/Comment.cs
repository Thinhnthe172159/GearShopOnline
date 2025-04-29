using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace GearShop.Models
{
    [PrimaryKey(nameof(OrderId), nameof(ProductId))]
    public class Comment
    {
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int Rate { get; set; }
        public string? CustomerName { get; set; }
        [StringLength(1000)]
        public string? Message { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;
    }
}
