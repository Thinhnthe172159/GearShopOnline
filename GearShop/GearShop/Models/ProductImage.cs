using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GearShop.Models
{
    public class ProductImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string ImageUrl { get; set; } = null!;
        public long ProductId { get; set; }
        public int Isthumbnail { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;

        public override string ToString()
        {
            return $"Ảnh sản phẩm [Mã ảnh={Id}, Đường dẫn ảnh={ImageUrl}, Mã sản phẩm={ProductId}, " +
                   $"Là ảnh đại diện={(Isthumbnail == 1 ? "Có" : "Không")}]";
        }

    }
}
