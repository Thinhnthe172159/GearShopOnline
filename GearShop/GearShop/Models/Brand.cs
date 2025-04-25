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
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int Status { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();

        public override string ToString()
        {
            return $"Thương hiệu [Mã thương hiệu={Id}, Tên thương hiệu={BrandName}, Ngày tạo={CreateDate:dd/MM/yyyy}, " +
                   $"Người tạo={CreatedBy}, Ngày chỉnh sửa={ModifiedDate:dd/MM/yyyy}, " +
                   $"Người chỉnh sửa={ModifiedBy}, Trạng thái={Status}]";
        }

    }
}

