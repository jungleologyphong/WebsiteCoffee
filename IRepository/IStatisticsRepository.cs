using WebsiteCoffeeShop.DTO;

namespace WebsiteCoffeeShop.IRepository
{
    public interface IStatisticsRepository
    {
        Task<OrderStatisticDTO> GetStatisticsAsync();

    }
}
