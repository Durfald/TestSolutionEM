using TestSolution.Models;

namespace TestSolution.Services.Parser
{
    public class FileParser : IFileParser
    {
        private readonly ILogger<FileParser> _logger;

        public FileParser(ILogger<FileParser> logger)
        {
            _logger = logger;
        }

        public List<AdvertisingData> ParseFromFile(string filePath)
        {
            var result = new List<AdvertisingData>();

            if (!File.Exists(filePath))
            {
                _logger.LogError("Файл {FilePath} не найден", filePath);
                throw new FileNotFoundException($"Файл не найден: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);

            foreach (var (line, index) in lines.Select((val, i) => (val, i + 1)))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogWarning("Строка {Index} пустая — пропускаем", index);
                    continue;
                }

                var parts = line.Split(':', 2);
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Строка {Index} имеет некорректный формат: {Line}", index, line);
                    continue;
                }

                var platform = parts[0].Trim();
                var locations = parts[1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToArray();

                if (string.IsNullOrWhiteSpace(platform))
                {
                    _logger.LogWarning("Строка {Index} — пустое имя площадки: {Line}", index, line);
                    continue;
                }

                if (locations.Length == 0)
                {
                    _logger.LogWarning("Строка {Index} — отсутствуют локации: {Line}", index, line);
                    continue;
                }

                result.Add(new AdvertisingData
                {
                    AdvertisingPlatform = platform,
                    Locations = locations
                });

                _logger.LogInformation("Добавлена площадка {Platform} с {Count} локациями",
                    platform, locations.Length);
            }

            _logger.LogInformation("Успешно загружено {Count} рекламных площадок из файла {FilePath}",
                result.Count, filePath);

            return result;
        }
    }

    public interface IFileParser
    {
        List<AdvertisingData> ParseFromFile(string filePath);
    }
}