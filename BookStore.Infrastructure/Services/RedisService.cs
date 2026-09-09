using BookStore.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly IDatabase? _db;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RedisService> _logger;
        private readonly bool _isConnected;

        public RedisService(IConfiguration configuration, IMemoryCache memoryCache, ILogger<RedisService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;

            try
            {
                var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 3000;
                options.SyncTimeout = 3000;

                _redis = ConnectionMultiplexer.Connect(options);
                _db = _redis.GetDatabase();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối đến Redis server. Hệ thống sẽ tự động fallback sang MemoryCache.");
                _isConnected = false;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_isConnected && _db != null)
                {
                    var value = await _db.StringGetAsync(key);
                    if (!value.IsNullOrEmpty)
                    {
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)value.ToString();
                        }
                        if (typeof(T) == typeof(int))
                        {
                            if (int.TryParse(value, out int result))
                                return (T)(object)result;
                            return default;
                        }
                        return JsonSerializer.Deserialize<T>(value!);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi truy xuất key {Key} từ Redis. Đang thử fallback sang MemoryCache.", key);
            }

            // Fallback sang MemoryCache
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }

            return default;
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                if (_isConnected && _db != null)
                {
                    await _db.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xóa key {Key} từ Redis.", key);
            }

            _memoryCache.Remove(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                if (_isConnected && _redis != null && _db != null)
                {
                    var endpoints = _redis.GetEndPoints();
                    if (endpoints.Length > 0)
                    {
                        var server = _redis.GetServer(endpoints[0]);
                        var keys = server.Keys(pattern: pattern);
                        foreach (var key in keys)
                        {
                            await _db.KeyDeleteAsync(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xóa keys theo pattern {Pattern} từ Redis.", pattern);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            // Luôn lưu vào MemoryCache để đảm bảo tính sẵn sàng
            if (expiry.HasValue)
            {
                _memoryCache.Set(key, value, expiry.Value);
            }
            else
            {
                _memoryCache.Set(key, value);
            }

            try
            {
                if (_isConnected && _db != null)
                {
                    string serializedValue;
                    if (typeof(T) == typeof(string) || typeof(T) == typeof(int))
                    {
                        serializedValue = value?.ToString() ?? "";
                    }
                    else
                    {
                        serializedValue = JsonSerializer.Serialize(value);
                    }

                    if (expiry.HasValue)
                    {
                        await _db.StringSetAsync(key, serializedValue, expiry.Value);
                    }
                    else
                    {
                        await _db.StringSetAsync(key, serializedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lưu key {Key} vào Redis. Đã lưu thành công vào MemoryCache.", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (_isConnected && _db != null)
                {
                    if (await _db.KeyExistsAsync(key))
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi kiểm tra key {Key} trên Redis.", key);
            }

            return _memoryCache.TryGetValue(key, out _);
        }
    }
}
