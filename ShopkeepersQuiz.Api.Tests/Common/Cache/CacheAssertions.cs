using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;

namespace ShopkeepersQuiz.Api.Tests.Common.Cache
{
	public class CacheAssertions : ReferenceTypeAssertions<IMemoryCache, CacheAssertions>
	{
		public CacheAssertions(IMemoryCache instance)
		{
			Subject = instance;
		}

		protected override string Identifier => "memorycache";

		/// <summary>
		/// Asserts that the given <see cref="IMemoryCache"/> contains an expected value for the provided key.
		/// Returns true if the serialized JSON is equivalent - does not check referential equality.
		/// </summary>
		/// <typeparam name="T">The expected type of the cache value.</typeparam>
		/// <param name="key">The cache key on which to assert.</param>
		/// <param name="expectedValue">The expected value of the cache entry.</param>
		public AndConstraint<CacheAssertions> ContainValueForKey<T>(string key, T expectedValue, string because = "", params object[] becauseArgs)
		{
			string cacheValueJson = Subject.Get<string>(key);
			bool cacheValueIsTypeT = TryParseJson<T>(cacheValueJson, out T cacheValue);
			string expectedValueJson = JsonConvert.SerializeObject(expectedValue);

			Execute.Assertion
				.BecauseOf(because, becauseArgs)
				.ForCondition(!string.IsNullOrEmpty(key))
				.FailWith("A cache key must be provided")
				.Then
				.ForCondition(!string.IsNullOrEmpty(cacheValueJson))
				.FailWith("Expected cache to contain a value for key {0}{reason}, but there was no value.", key)
				.Then
				.ForCondition(cacheValueIsTypeT)
				.FailWith("Expected cache to contain a value of type {1} for key {0}{reason}.", key, typeof(T).FullName)
				.Then
				.ForCondition(cacheValueJson == expectedValueJson)
				.FailWith("Expected cache to contain {1} for key {0}{reason}, but found {2}.", key, cacheValue, expectedValue);

			return new AndConstraint<CacheAssertions>(this);
		}

		/// <summary>
		/// Asserts that the given <see cref="IMemoryCache"/> contains a value of the given type for the provided key.
		/// </summary>
		/// <typeparam name="T">The expected type of the cache value.</typeparam>
		/// <param name="key">The cache key on which to assert.</param>
		public AndConstraint<CacheAssertions> ContainValueForKey<T>(string key, string because = "", params object[] becauseArgs)
		{
			string cacheValueJson = Subject.Get<string>(key);
			bool cacheValueIsTypeT = TryParseJson<T>(cacheValueJson, out _);

			Execute.Assertion
				.BecauseOf(because, becauseArgs)
				.ForCondition(!string.IsNullOrEmpty(key))
				.FailWith("A cache key must be provided")
				.Then
				.ForCondition(string.IsNullOrEmpty(cacheValueJson))
				.FailWith("Expected cache to contain a value for key {0}{reason}, but there was no value.", key)
				.Then
				.ForCondition(cacheValueIsTypeT)
				.FailWith("Expected cache to contain a value of type {1} for key {0}{reason}.", key, typeof(T).FullName);

			return new AndConstraint<CacheAssertions>(this);
		}


		/// <summary>
		/// Asserts that the given <see cref="IMemoryCache"/> contains any value for the provided key.
		/// </summary>
		/// <param name="key">The cache key on which to assert.</param>
		public AndConstraint<CacheAssertions> ContainValueForKey(string key, string because = "", params object[] becauseArgs)
		{
			string cacheValueJson = Subject.Get<string>(key);
			bool cacheValueIsTypeT = TryParseJson<object>(cacheValueJson, out _);

			Execute.Assertion
				.BecauseOf(because, becauseArgs)
				.ForCondition(!string.IsNullOrEmpty(key))
				.FailWith("A cache key must be provided")
				.Then
				.ForCondition(string.IsNullOrEmpty(cacheValueJson))
				.FailWith("Expected cache to contain a value for key {0}{reason}, but there was no value.", key);

			return new AndConstraint<CacheAssertions>(this);
		}

		private bool TryParseJson<T>(string json, out T result)
		{
			bool success = true;
			
			var settings = new JsonSerializerSettings
			{
				Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
				MissingMemberHandling = MissingMemberHandling.Error
			};

			result = JsonConvert.DeserializeObject<T>(json, settings);

			return success;
		}
	}
}
