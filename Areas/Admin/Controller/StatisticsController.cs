using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using WebsiteCoffeeShop.DTO.Chart;
using WebsiteCoffeeShop.IRepository;

namespace WebsiteCoffeeShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private IStatisticsRepository _statisticsRepository;

        // GET: StatisticsController
        public StatisticsController(IStatisticsRepository statisticsRepository)
        {
            _statisticsRepository = statisticsRepository;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var stats = await _statisticsRepository.GetStatisticsAsync();


            List<DataPoint> orderByStatus = stats.OrderByStatus
                .Select(kv => new DataPoint(kv.Key, kv.Value))
                .ToList();

            List<DataPoint> revenuePoints = stats.RevenueByDates
                .Select(r => new DataPoint(r.Date.ToString("dd/MM"), (double)r.Revenue))
                .ToList();

            List<DataPoint> topProductPoints = stats.TopProducts
                .Select(p => new DataPoint(p.ProductName, (double)p.QuantitySold))
                .ToList();


            ViewBag.TopProductPoints = JsonConvert.SerializeObject(topProductPoints);
            ViewBag.RevenuePoints = JsonConvert.SerializeObject(revenuePoints);
            ViewBag.OrderByStatus = JsonConvert.SerializeObject(orderByStatus);


            if (stats == null)
            {
                throw new Exception("Stats is null!");
            }

            return View(stats);
        }
    }
}
