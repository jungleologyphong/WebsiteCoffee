using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.Models
{
    // Đơn hàng
    // Đơn hàng có thể có nhiều chi tiết đơn hàng (OrderDetail)
    // Mỗi chi tiết đơn hàng liên kết với một sản phẩm (Product)
    // Mỗi đơn hàng thuộc về một người dùng (ApplicationUser)
    // Có thể áp dụng mã giảm giá (DiscountCode) cho đơn hàng
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalPrice { get; set; }
        public decimal DiscountFromPoints { get; set; } = 0;
        public string ShippingAddress { get; set; }
        public string Notes { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }

        public string PaymentMethod { get; set; }
        public string Status { get; set; } = "Pending";

        public int RewardPointsEarned { get; set; } = 0;
        public int RewardPointsUsed { get; set; } = 0;

        // 🔥 Liên kết với DiscountCode
        public int? DiscountCodeId { get; set; } // Có thể null nếu không áp mã

        [ForeignKey("DiscountCodeId")]
        [ValidateNever]
        public DiscountCode DiscountCode { get; set; }
    }
}
