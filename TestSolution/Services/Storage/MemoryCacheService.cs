using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace ApiServerWarframe.Services.Storage.MemoryStorage
{
    /// <summary>
    /// Сервис-обёртка над IMemoryCache с поддержкой префиксных операций и автоматическим трекингом ключей.
    /// </summary>
    public class MemoryCacheService
    {
        private readonly ConcurrentDictionary<string, byte> _keys = new();
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService>? _logger;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(6);

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService>? logger = null)
        {
            _cache = cache;
            _logger = logger;
        }

        #region === Основные операции Get / Set ===

        /// <summary>
        /// Получить значение из кэша по ключу.
        /// </summary>
        public T? Get<T>(string key)
        {
            var exists = _cache.TryGetValue(key, out var value);
            _logger?.LogDebug($"Cache GET [{key}] => {(exists ? "HIT" : "MISS")}");
            return (T?)value;
        }

        /// <summary>
        /// Получить значение из кэша или добавить новое, если отсутствует.
        /// </summary>
        public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                _logger?.LogDebug($"Cache HIT [{key}]");
                return (T)existing!;
            }

            _logger?.LogDebug($"Cache MISS [{key}] => generating...");
            var value = factory();
            Set(key, value, expiration);
            return value;
        }

        /// <summary>
        /// Асинхронно получить значение из кэша или добавить новое, если отсутствует.
        /// </summary>
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                _logger?.LogDebug($"Cache HIT [{key}]");
                return (T)existing!;
            }

            _logger?.LogDebug($"Cache MISS [{key}] => generating async...");
            var value = await factory();
            Set(key, value, expiration);
            return value;
        }

        /// <summary>
        /// Добавить или обновить значение в кэше.
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = expiration ?? _defaultExpiration
            };

            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                var keyString = k?.ToString() ?? string.Empty;
                _keys.TryRemove(keyString, out _);
                _logger?.LogDebug($"Cache EVICTED [{keyString}], reason: {reason}. Key удалён из _keys.");
            });

            _cache.Set(key, value!, options);
            _keys.TryAdd(key, 0);

            _logger?.LogDebug($"Cache SET [{key}] => {typeof(T).Name}");
        }

        /// <summary>
        /// Обновить TTL (время жизни) записи в кэше по ключу.
        /// </summary>
        public void Refresh<T>(string key, TimeSpan? newExpiration = null)
        {
            if(Exists(key))
            {
                var value = Get<T>(key); Set(key, value, newExpiration);
                _logger?.LogDebug($"Cache REFRESH [{key}] на {newExpiration ?? _defaultExpiration}");
            }
        }

        #endregion

        #region === Удаление записей ===

        /// <summary>
        /// Удалить значение из кэша по ключу.
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key,out _);
            _logger?.LogDebug($"Cache REMOVE [{key}]");
        }

        /// <summary>
        /// Удалить все записи, ключ которых начинается с заданного префикса.
        /// </summary>
        public void RemoveByPrefix(string prefix)
        {
            var toRemove = _keys.Keys.Where(k => k.StartsWith(prefix)).ToList();

            foreach (var key in toRemove)
                Remove(key);
        }

        /// <summary>
        /// Удалить все записи из кэша.
        /// </summary>
        public void RemoveAll()
        {
            foreach (var key in _keys.ToList())
            {
                Remove(key.Key);
            }
        }

        #endregion

        #region === Работа с префиксами и ключами ===

        /// <summary>
        /// Получить все ключи из кэша, начинающиеся с указанного префикса.
        /// </summary>
        public List<string> GetKeysByPrefix(string prefix)
        {
            var matchingKeys = _keys.Where(k => k.Key.StartsWith(prefix))
                        .Select(k => k.Key)
                        .ToList();
            _logger?.LogDebug($"Found {matchingKeys.Count} keys by prefix '{prefix}'");
            return matchingKeys;
        }

        /// <summary>
        /// Обновить TTL всех записей, ключ которых начинается с заданного префикса.
        /// </summary>
        public void RefreshByPrefix(string prefix, TimeSpan? expiration = null)
        {
            var keys = GetKeysByPrefix(prefix);
            foreach (var key in keys)
            {
                var value = _cache.Get(key);
                if (value != null)
                {
                    Set(key, value, expiration);
                    _logger?.LogDebug($"Refreshed [{key}] to TTL: {expiration ?? _defaultExpiration}");
                }
            }

            _logger?.LogDebug($"Refreshed {keys.Count} keys by prefix '{prefix}'");
        }

        /// <summary>
        /// Удалить устаревшие ключи, которые больше не присутствуют в кэше.
        /// </summary>
        public void CleanUpKeys()
        {
            var keysToRemove = _keys.Where(key => !_cache.TryGetValue(key, out _)).ToList();

            foreach (var key in keysToRemove)
            {
                _keys.TryRemove(key.Key, out _);
                _logger?.LogDebug($"CleanUpKeys: Удалён устаревший ключ [{key}] из _keys");
            }
        }

        #endregion

        #region === Работа со словарями в кэше ===

        /// <summary>
        /// Добавить или обновить значение по ключу внутри словаря, хранящегося в кэше.
        /// </summary>
        public void AddOrUpdateInDictionary<TKey, TValue>(string cacheKey, TKey itemKey, TValue itemValue)
        {
            var dict = Get<Dictionary<TKey, TValue>>(cacheKey);
            if (dict == null)
            {
                dict = new Dictionary<TKey, TValue>();
                Set(cacheKey, dict);
            }

            dict[itemKey] = itemValue;
            _logger?.LogDebug($"Dictionary[{cacheKey}][{itemKey}] = {itemValue?.ToString() ?? "null"}");
        }

        /// <summary>
        /// Получить значение из словаря в кэше по ключу словаря.
        /// </summary>
        public TValue? GetFromDictionary<TKey, TValue>(string cacheKey, TKey itemKey)
        {
            var dict = Get<Dictionary<TKey, TValue>>(cacheKey);
            if (dict != null && dict.TryGetValue(itemKey, out var value))
            {
                _logger?.LogDebug($"Dictionary[{cacheKey}][{itemKey}] HIT");
                return value;
            }

            _logger?.LogDebug($"Dictionary[{cacheKey}][{itemKey}] MISS");
            return default;
        }

        #endregion

        #region === Проверки и утилиты ===

        /// <summary>
        /// Проверить, существует ли значение по ключу в кэше.
        /// </summary>
        public bool Exists(string key)
        {
            bool exists = _cache.TryGetValue(key, out _);
            _logger?.LogDebug($"Cache EXISTS [{key}] => {(exists ? "YES" : "NO")}");
            return exists;
        }

        #endregion
    }
}