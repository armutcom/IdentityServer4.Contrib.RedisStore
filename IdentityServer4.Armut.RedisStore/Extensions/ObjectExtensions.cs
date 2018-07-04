using System.Collections.Generic;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace IdentityServer4.Armut.RedisStore.Extensions
{
    public static class ObjectExtensions
    {
        public static TTarget Deserialize<TTarget>(this string raw)
        {
            return JsonConvert.DeserializeObject<TTarget>(raw);
        }

        public static TTarget Deserialize<TTarget>(this RedisValue raw)
        {
            return JsonConvert.DeserializeObject<TTarget>(raw);
        }

        public static IEnumerable<TTarget> Deserialize<TTarget>(this IEnumerable<RedisValue> raws)
        {
            foreach (var raw in raws)
            {
                yield return raw.Deserialize<TTarget>();
            }
        }

        public static string Serialize(this object data)
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
