using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.Controllers
{
    [Authorize] // Yêu cầu đăng nhập cho tất cả các action trong controller này
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        [AllowAnonymous] // Cho phép tất cả mọi người truy cập
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        [Authorize(Roles = SD.Role_Admin)] // Chỉ Admin mới có quyền thêm sản phẩm
        public async Task<IActionResult> Add()
        {
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }

            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng không thể âm.");
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }

            if (imageUrl != null)
            {
                var imagePath = await SaveImage(imageUrl);
                if (imagePath == null)
                {
                    ModelState.AddModelError("ImageUrl", "Chỉ nhận file đuôi .jpg, .jpeg, .png, .gif");
                    return View(product);
                }
                product.ImageUrl = imagePath;
            }

            await _productRepository.AddAsync(product);
            TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }

            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null) return NotFound();

            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng không thể âm.");
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Quantity = product.Quantity;

            if (imageUrl != null)
            {
                existingProduct.ImageUrl = await SaveImage(imageUrl);
            }

            try
            {
                await _productRepository.UpdateAsync(existingProduct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật sản phẩm: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật sản phẩm. Vui lòng thử lại.");
                ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
                return View(product);
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            await _productRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string query) // tìm kiếm sản phẩm theo từ khóa
        {
            if (string.IsNullOrWhiteSpace(query)) // kiểm tra nếu thuộc tính query null hoặc rỗng thì trả về NotFound
            {
                return NotFound();
            }

            var products = await _productRepository.SearchProductsAsync(query); // tìm kiếm sản phẩm theo từ khóa
            return View("Index", products); // trả về danh sách sản phẩm tìm được
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var products = await _productRepository.SearchProductsAsync(term);
            var suggestions = products.Select(p => new
            {
                id = p.Id,
                label = p.Name
            }).ToList();

            return Json(suggestions);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLower();

            if (!validExtensions.Contains(fileExtension))
            {
                return null;
            }

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", uniqueFileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/Images/" + uniqueFileName;
        }
    }
}
