using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebsiteCoffeeShop.Models
{
    public class RewardPoints
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public int Points { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }
    }
}
