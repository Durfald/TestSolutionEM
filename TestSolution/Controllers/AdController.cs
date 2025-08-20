using Microsoft.AspNetCore.Mvc;
using TestSolution.Services.Ads;

namespace TestSolution.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdController : ControllerBase
    {
        private readonly IAdvertisingService _advertisingService;
        private readonly ILogger<AdController> _logger;

        public AdController(IAdvertisingService advertisingService, ILogger<AdController> logger)
        {
            _advertisingService = advertisingService;
            _logger = logger;
        }

        /// <summary>
        /// Перезагрузка данных рекламных площадок из файла
        /// </summary>
        [HttpPost("reload")]
        public IActionResult ReloadData([FromQuery] string fileName = "ad.txt")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return StatusCode(400, new {Message = "Не указано имя файла." });

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ADs", fileName);

            if (!System.IO.File.Exists(filePath))
                return StatusCode(500, new { Message = $"Файл не найден: {filePath}" });

            try
            {
                _advertisingService.LoadFromFile(filePath);
                _logger.LogInformation("Данные рекламных площадок успешно перезагружены из {FilePath}", filePath);
                return Ok(new { Message = "Данные успешно загружены" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке данных из файла {FilePath}", filePath);
                return StatusCode(500, new { Message = "Ошибка при загрузке данных", Details = ex.Message });
            }
        }

        /// <summary>
        /// Получение списка рекламных площадок для указанной локации
        /// </summary>
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return StatusCode(400, new {Message = "Не указана локация для поиска." });

            try
            {
                var platforms = _advertisingService.Search(location);
                return Ok(new { Location = location, Platforms = platforms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске площадок для локации {Location}", location);
                return StatusCode(500, new { Message = "Ошибка при поиске", Details = ex.Message });
            }
        }
    }
}
