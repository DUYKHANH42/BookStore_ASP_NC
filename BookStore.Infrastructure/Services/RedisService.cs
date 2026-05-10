using BookStore.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Redis");
            _redis = ConnectionMultiplexer.Connect(connectionString ?? "localhost:6379");
            _db = _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            
            // If T is primitive like int or string, we should handle it properly, 
            // but for safety we can just serialize everything.
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

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints[0]);
            var keys = server.Keys(pattern: pattern);
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
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

        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
    }
}
