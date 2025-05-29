namespace WebsiteCoffeeShop.DTO
{
    public class OrderStatisticDTO
    {
        public int TotalOrders { get; set; }
        public double TotalRevenue { get; set; }
        public int OrdersToday { get; set; }
        public Dictionary<string, int> OrderByStatus { get; set; } = new(); // nên có default
        public List<RevenueByDateDTO> RevenueByDates { get; set; }
        public List<TopProductDTO> TopProducts { get; set; }
    }
}
