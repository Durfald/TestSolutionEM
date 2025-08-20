using ApiServerWarframe.Services.Storage.MemoryStorage;
using System.Diagnostics;
using TestSolution.Services.Parser;

namespace TestSolution.Services.Ads
{
    public interface IAdvertisingService
    {
        void LoadFromFile(string filePath);
        List<string> Search(string location);
    }

    public class AdvertisingService : IAdvertisingService
    {
        private readonly IFileParser _fileParser;
        private readonly MemoryCacheService _cache;
        private readonly ILogger<AdvertisingService> _logger;
        private const string CachePrefix = "adplatforms:";

        public AdvertisingService(
            IFileParser fileParser,
            MemoryCacheService cache,
            ILogger<AdvertisingService> logger)
        {
            _fileParser = fileParser;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Загружает рекламные площадки из файла и кладёт их в кэш
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            _logger.LogInformation("Загрузка рекламных площадок из файла {FilePath}", filePath);

            var platforms = _fileParser.ParseFromFile(filePath);
            // очищаем старые данные
            _cache.RemoveByPrefix(CachePrefix);

            foreach (var platform in platforms)
            {
                foreach (var location in platform.Locations)
                {
                    var key = $"{CachePrefix}{location}";
                    var list = _cache.Get<List<string>>(key) ?? new List<string>();

                    list.Add(platform.AdvertisingPlatform);

                    _cache.Set(key, list);
                }
            }

            _logger.LogInformation("Загружено {Count} площадок", platforms.Count);
        }

        /// <summary>
        /// Ищет рекламные площадки для указанной локации (учитывая вложенность)
        /// </summary>
        public List<string> Search(string location)
        {
            var result = new List<string>();

            var parts = location.Trim('/').Split('/');
            for (int i = parts.Length; i > 0; i--)
            {
                var prefix = "/" + string.Join("/", parts.Take(i));
                var key = $"{CachePrefix}{prefix}";

                var platforms = _cache.Get<List<string>>(key);
                if (platforms != null)
                {
                    result.AddRange(platforms);
                }
            }

            var distinctResult = result.Distinct().ToList();
            _logger.LogInformation("Поиск для {Location}: найдено {Count} площадок", location, distinctResult.Count);
            return distinctResult;
        }
    }
}
