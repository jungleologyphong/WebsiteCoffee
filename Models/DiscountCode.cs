using System.ComponentModel.DataAnnotations;

namespace WebsiteCoffeeShop.Models
{
    public class DiscountCode
    {
        public DiscountCode()
        {
            // Initialize Orders collection to empty to avoid null reference
            Orders = new List<Order>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int DiscountPercent { get; set; }

        public double DiscountAmount { get; set; } // Add this property

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsPercentage { get; set; } // Add this property

        // Navigation property (optional)
        public ICollection<Order> Orders { get; set; }
    }
}
