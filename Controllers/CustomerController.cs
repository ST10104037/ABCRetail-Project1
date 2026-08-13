using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly IConfiguration _config;
        private readonly string _tableName;

        public CustomerController(TableStorageService tableService, IConfiguration config)
        {
            _tableService = tableService;
            _config = config;
            _tableName = _config["AzureStorage:TableName_Customers"] ?? "CustomerProfiles";
        }

        // GET: /Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _tableService.GetAllEntitiesAsync<CustomerProfileEntity>(_tableName);
            return View(customers);
        }

        // GET: /Customer/Create
        public IActionResult Create() => View();

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfileEntity customer)
        {
            if (!ModelState.IsValid) return View(customer);

            await _tableService.AddEntityAsync(_tableName, customer);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Delete?partitionKey=...&rowKey=...
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableService.DeleteEntityAsync(_tableName, partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
