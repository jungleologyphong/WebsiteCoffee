using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;

namespace WebsiteCoffeeShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public HomeController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products.Where(p => p.Quantity > 0)); // Chỉ hiển thị sản phẩm còn hàng
        }

        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                TempData["ErrorMessage"] = "Keyword cannot be empty";
                return RedirectToAction("Index");
            }

            var products = await _productRepository.SearchProductsAsync(keyword);
            return View("Index", products.Where(p => p.Quantity > 0));
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            if (ModelState.IsValid)
            {
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
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null) return NotFound();

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;

                if (imageUrl != null)
                {
                    var imagePath = await SaveImage(imageUrl);
                    if (imagePath == null)
                    {
                        ModelState.AddModelError("ImageUrl", "Chỉ nhận file đuôi .jpg, .jpeg, .png, .gif");
                        return View(product);
                    }
                    existingProduct.ImageUrl = imagePath;
                }

                await _productRepository.UpdateAsync(existingProduct);
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            ViewBag.CategoryName = category != null ? category.Name : "Unknown";

            return View(product);
        }

        public async Task<IActionResult> LoadMoreProducts(int page = 1, int pageSize = 6)
        {
            var paginatedProducts = await _productRepository.GetPaginatedProductsAsync(page, pageSize);
            return !paginatedProducts.Any() ? Content("") : PartialView("_ProductPartial", paginatedProducts);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            if (image == null || image.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension)) return null;

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/Images/" + fileName;
        }
    }
}
