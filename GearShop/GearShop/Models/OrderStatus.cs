namespace GearShop.Models
{
    public class OrderStatus
    {
        public int Id { get; set; } = 0;
        public string? Status { get; set; }

        public OrderStatus()
        {

        }
        public List<OrderStatus> GetAllStatus()
        {
            List<OrderStatus> list = new List<OrderStatus>();
            list.Add(new OrderStatus { Id = 0, Status = "✖️ Đơn hàng bị hủy" });
            list.Add(new OrderStatus { Id = 1, Status = "💳 Chờ Thanh toán" });
            list.Add(new OrderStatus { Id = 2, Status = "🚛 Chờ giao hàng" });
            list.Add(new OrderStatus { Id = 3, Status = "🚛 Đang vận chuyển" });
            list.Add(new OrderStatus { Id = 4, Status = "✔️ Đã nhận hàng" });
            return list;
        }

    }
}
