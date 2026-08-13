using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class LogFilesController : Controller
    {
        private readonly FileShareStorageService _fileService;

        public LogFilesController(FileShareStorageService fileService)
        {
            _fileService = fileService;
        }

        // GET: /LogFiles
        public IActionResult Index()
        {
            var files = _fileService.ListLogFiles();
            return View(files);
        }

        // GET: /LogFiles/Create
        public IActionResult Create() => View();

        // POST: /LogFiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile logFile)
        {
            if (logFile != null && logFile.Length > 0)
            {
                using var stream = logFile.OpenReadStream();
                await _fileService.UploadLogFileAsync(stream, logFile.FileName);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /LogFiles/Download?fileName=...
        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _fileService.DownloadLogFileAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }
    }
}
