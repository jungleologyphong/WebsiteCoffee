using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.Context;
using WebsiteCoffeeShop.DTO;
using WebsiteCoffeeShop.IRepository;

namespace WebsiteCoffeeShop.Repositories
{
    public class StatisticsRepository: IStatisticsRepository
    {
        private readonly ApplicationDbContext _context;
        public StatisticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<OrderStatisticDTO> GetStatisticsAsync()
        {
            var totalOrders = await _context.Orders.CountAsync();

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalPrice - o.DiscountFromPoints);

            var ordersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == DateTime.Today);

            var orderByStatus = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            var revenueByDates = await _context.Orders
                .Where(o => o.Status == "Completed" && o.OrderDate >= DateTime.Today.AddDays(-7))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueByDateDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice - x.DiscountFromPoints)
                }).ToListAsync();

            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => od.Product.Name)
                .Select(g => new TopProductDTO
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            return new OrderStatisticDTO
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                OrdersToday = ordersToday,
                OrderByStatus = orderByStatus,
                RevenueByDates = revenueByDates,
                TopProducts = topProducts
            };
        }
    }
}
