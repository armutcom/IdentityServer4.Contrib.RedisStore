namespace IdentityServer4.Armut.RedisStore.Stores
{
    public interface IAidRedisPersistence
    {
        void SaveAsync(string key, object data);
    }
}
