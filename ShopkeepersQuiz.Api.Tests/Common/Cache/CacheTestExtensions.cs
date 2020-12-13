using FluentAssertions.Primitives;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace ShopkeepersQuiz.Api.Tests.Common.Cache
{
	public static class CacheTestExtensions
	{
		public static CacheAssertions Should(this IMemoryCache instance)
			=> new CacheAssertions(instance);

		public static void SetupForTest(this IMemoryCache cache, string key, object value) =>
			cache.Set(key, JsonConvert.SerializeObject(value));
	}
}
