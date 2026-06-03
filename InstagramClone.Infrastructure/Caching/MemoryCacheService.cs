using InstagramClone.Application.Interfaces.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace InstagramClone.Infrastructure.Caching{
    public class MemoryCacheService(IDistributedCache cache) : ICacheService
    {
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedData = await cache.GetStringAsync(key);
            if(cachedData != null)
            {
                return JsonSerializer.Deserialize<T>(cachedData);
            }

            var result = await factory();
            if (result != null)
            {
                var option = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15)
                };

                await cache.SetStringAsync(key, JsonSerializer.Serialize(result), option);
            }

            return result;
        }

        public async Task RemoveAsync(string key) => await cache.RemoveAsync(key);

        public async Task<string> GetScopeVersionAsync(string scopeKey)
        {
            var v = await cache.GetStringAsync(scopeKey);
            return v ?? "0";
        }

        public async Task BumpScopeVersionAsync(string scopeKey)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            };
            await cache.SetStringAsync(scopeKey, Guid.NewGuid().ToString("N"), options);
        }
    }
}
