using System.ComponentModel.DataAnnotations;

namespace WebsiteCoffeeShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(20000, 200000, ErrorMessage = "Giá phải từ 20,000 đến 200,000.")]
        public decimal Price { get; set; }

        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductImage>? Images { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // 📌 Thêm số lượng sản phẩm
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không thể âm")]
        public int Quantity { get; set; } = 0; // Mặc định số lượng là 0
    }

}
