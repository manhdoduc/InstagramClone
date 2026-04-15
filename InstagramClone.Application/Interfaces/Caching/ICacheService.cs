namespace InstagramClone.Application.Interfaces.Caching
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        Task RemoveAsync(string key);

        /// <summary>Token included in cache keys; bump to invalidate all keys under that scope without wildcard delete.</summary>
        Task<string> GetScopeVersionAsync(string scopeKey);

        Task BumpScopeVersionAsync(string scopeKey);
    }
}
