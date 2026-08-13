using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly QueueStorageService _queueService;

        public OrderController(QueueStorageService queueService)
        {
            _queueService = queueService;
        }

        // GET: /Order  - shows current queue messages (peek, does not remove them)
        public async Task<IActionResult> Index()
        {
            var messages = await _queueService.PeekMessagesAsync(20);
            return View(messages);
        }

        // GET: /Order/Create
        public IActionResult Create() => View();

        // POST: /Order/Create - e.g. "Processing order #1023", "imageName: shoe123.jpg"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string messageText)
        {
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                await _queueService.SendMessageAsync(messageText);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Order/ProcessNext - simulates a worker picking up and completing the next message
        [HttpPost]
        public async Task<IActionResult> ProcessNext()
        {
            await _queueService.ReceiveAndDeleteNextMessageAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
