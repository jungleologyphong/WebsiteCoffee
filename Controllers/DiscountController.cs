using Microsoft.AspNetCore.Mvc;
using WebsiteCoffeeShop.IRepository;

[ApiController]
[Route("DiscountCode")]
public class DiscountCodeController : ControllerBase
{
    private readonly IDiscountCodeRepository _discountRepo;

    public DiscountCodeController(IDiscountCodeRepository discountRepo)
    {
        _discountRepo = discountRepo;
    }

    [HttpGet("VerifyDiscountCode")]
    public async Task<IActionResult> VerifyDiscountCode(string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { success = false, message = "Mã không hợp lệ." });
            }

            var discount = await _discountRepo.GetByCodeAsync(code.ToLower());

            if (discount == null || !discount.IsActive || discount.ExpiryDate < DateTime.UtcNow)
            {
                return NotFound(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            return Ok(new
            {
                success = true,
                id = discount.Id,
                code = discount.Code,
                isPercentage = discount.IsPercentage,
                value = discount.IsPercentage ? discount.DiscountPercent : discount.DiscountAmount
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi xảy ra khi xác minh mã giảm giá: " + ex.Message);
            throw;
        }

    }
}