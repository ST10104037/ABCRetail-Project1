using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly BlobStorageService _blobService;
        private readonly QueueStorageService _queueService;
        private readonly string _tableName;

        public ProductController(TableStorageService tableService, BlobStorageService blobService,
            QueueStorageService queueService, IConfiguration config)
        {
            _tableService = tableService;
            _blobService = blobService;
            _queueService = queueService;
            _tableName = config["AzureStorage:TableName_Products"] ?? "Products";
        }

        // GET: /Product
        public async Task<IActionResult> Index()
        {
            var products = await _tableService.GetAllEntitiesAsync<ProductEntity>(_tableName);

            // Attach a viewable image URL for each product that has one
            ViewBag.ImageUrls = products
                .Where(p => !string.IsNullOrEmpty(p.ImageBlobName))
                .ToDictionary(p => p.RowKey, p => _blobService.GetBlobUrl(p.ImageBlobName!));

            return View(products);
        }

        // GET: /Product/Create
        public IActionResult Create() => View();

        // POST: /Product/Create  (handles product info + image upload together)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEntity product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                var blobName = await _blobService.UploadFileAsync(stream, imageFile.FileName, imageFile.ContentType);
                product.ImageBlobName = blobName;
            }

            await _tableService.AddEntityAsync(_tableName, product);

            // Notify the order/inventory queue that a new product (with image) was added
            await _queueService.SendMessageAsync(
                $"New product added: '{product.ProductName}' | image: {product.ImageBlobName ?? "none"} | stock: {product.StockQuantity}");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Product/Delete?partitionKey=...&rowKey=...
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableService.DeleteEntityAsync(_tableName, partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
