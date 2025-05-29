using System.ComponentModel.DataAnnotations;

namespace WebsiteCoffeeShop.Models
{
    public class CartItem
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }

        // 📌 Số lượng tồn kho của sản phẩm
        public int StockQuantity { get; set; }

        // 📌 Ẩn số lượng nếu hết hàng
        public bool ShowQuantity => StockQuantity > 0;
    }
}
