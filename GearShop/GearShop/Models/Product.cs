using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Identity.Client;

namespace GearShop.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [StringLength(350)]
        public string ProductName { get; set; } = null!;
        public int? BrandId { get; set; }
        public int? ProductTypeId { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime InServiceDate { get; set; }
        public DateTime InStockDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = null!;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int Status { get; set; }
        [ForeignKey(nameof(BrandId))]
        public virtual Brand Brand { get; set; } = null!;
        [ForeignKey(nameof(ProductTypeId))]
        public virtual ProductType ProductType { get; set; } = null!;
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public override string ToString()
        {
            return $"Sản phẩm [Mã sản phẩm={Id}, Tên sản phẩm={ProductName}, Thương hiệu={Brand?.BrandName}, " +
                   $"Loại sản phẩm={ProductType?.TypeName}, Mô tả={Description}, Số lượng={Quantity}, " +
                   $"Giá={Price}";
        }


    }
}
