using FluentAssertions.Primitives;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace ShopkeepersQuiz.Api.Tests.Common.Cache
{
	public static class CacheTestExtensions
	{
		public static CacheAssertions Should(this IMemoryCache instance) => new CacheAssertions(instance);
	}
}
