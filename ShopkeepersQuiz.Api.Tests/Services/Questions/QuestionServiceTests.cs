using AutoFixture;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using ShopkeepersQuiz.Api.Models.Cache;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Queues;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.QueueEntries;
using ShopkeepersQuiz.Api.Services.Questions;
using ShopkeepersQuiz.Api.Tests.Common;
using ShopkeepersQuiz.Api.Tests.Common.Cache;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ShopkeepersQuiz.Api.Tests.Services.Questions
{
	public class QuestionServiceTests : TestFixture
	{
		public static readonly DateTime UtcNowFixed = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		private readonly Mock<IQuestionRepository> _mockQuestionRepository = new Mock<IQuestionRepository>();
		private readonly Mock<IQueueEntryRepository> _mockQueueEntryRepository = new Mock<IQueueEntryRepository>();
		private readonly IMemoryCache _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
		private readonly Mock<DateTimeProvider> _mockDateTimeProvider = new Mock<DateTimeProvider>();

		private readonly QuestionSettings _questionSettings = new QuestionSettings();

		private readonly QuestionService Sut;

		public QuestionServiceTests()
		{
			Sut = new QuestionService(
				_mockQuestionRepository.Object,
				_mockQueueEntryRepository.Object,
				Options.Create(_questionSettings),
				_cache,
				_mockDateTimeProvider.Object);

			_mockDateTimeProvider.Setup(x => x.GetUtcNow()).Returns(UtcNowFixed);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheReturnsEnoughQuestions_CachedQuestionsReturnedSuccessfully()
		{
			var cacheResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, UtcNowFixed.AddSeconds(30))
				.With(x => x.EndTimeUtc, UtcNowFixed.AddSeconds(45))
				.CreateMany();
			_cache.Set(CacheKeys.QuestionQueue, cacheResults);

			_mockQueueEntryRepository.Setup(x => x.GetUpcomingQueueEntries())
				.Throws(new AssertionFailedException("QueueRepository should not be called in this scenario."));

			_questionSettings.PreloadedQuestionsCount = cacheResults.Count();

			var result = await Sut.GetQuestionQueue();

			result.Should().HaveCount(cacheResults.Count());
			result.Should().BeEquivalentTo(cacheResults);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheIsEmpty_QuestionQueueEntriesFromDatabaseAreReturnedSuccessfullyInstead()
		{
			var repositoryResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, UtcNowFixed.AddSeconds(30))
				.With(x => x.EndTimeUtc, UtcNowFixed.AddSeconds(45))
				.CreateMany();
			_mockQueueEntryRepository.Setup(x => x.GetUpcomingQueueEntries()).ReturnsAsync(repositoryResults);

			_questionSettings.PreloadedQuestionsCount = repositoryResults.Count();

			var result = await Sut.GetQuestionQueue();

			result.Should().HaveCount(repositoryResults.Count());
			result.Should().BeEquivalentTo(repositoryResults);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheDoesNotContainEnoughQuestionsButDatabaseDoes_QuestionQueueEntriesFromDatabaseAreReturnedSuccessfullyInstead()
		{
			var repositoryResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, UtcNowFixed.AddSeconds(30))
				.With(x => x.EndTimeUtc, UtcNowFixed.AddSeconds(45))
				.CreateMany(3);
			_mockQueueEntryRepository.Setup(x => x.GetUpcomingQueueEntries()).ReturnsAsync(repositoryResults);

			var cacheResults = new List<QueueEntry>() { repositoryResults.First() };

			_cache.Set(CacheKeys.QuestionQueue, cacheResults);
			
			_questionSettings.PreloadedQuestionsCount = repositoryResults.Count();

			var result = await Sut.GetQuestionQueue();

			result.Should().HaveCount(repositoryResults.Count());
			result.Should().NotHaveCount(cacheResults.Count());
			result.Should().BeEquivalentTo(repositoryResults);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheDoesNotContainEnoughQuestionsButDatabaseDoes_CacheIsUpdatedWithNewQueueSuccessfully()
		{
			var repositoryResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, UtcNowFixed.AddSeconds(30))
				.With(x => x.EndTimeUtc, UtcNowFixed.AddSeconds(45))
				.CreateMany(3);
			_mockQueueEntryRepository.Setup(x => x.GetUpcomingQueueEntries()).ReturnsAsync(repositoryResults);

			var cacheResults = new List<QueueEntry>() { repositoryResults.First() };

			_cache.Set(CacheKeys.QuestionQueue, cacheResults);

			_questionSettings.PreloadedQuestionsCount = repositoryResults.Count();

			await Sut.GetQuestionQueue();

			_cache.Should().ContainValueForKey(CacheKeys.QuestionQueue, repositoryResults);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheIsEmptyButDatabaseContainsEnoughEntries_CorrectAnswersAreAddedToCache()
		{
			DateTime testTimeUtcNow = DateTime.UtcNow.AddDays(1);
			_mockDateTimeProvider.Setup(x => x.GetUtcNow()).Returns(testTimeUtcNow);

			var repositoryResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, testTimeUtcNow.AddSeconds(30))
				.With(x => x.EndTimeUtc, testTimeUtcNow.AddSeconds(45))
				.CreateMany(3);
			_mockQueueEntryRepository.Setup(x => x.GetUpcomingQueueEntries()).ReturnsAsync(repositoryResults);

			_questionSettings.PreloadedQuestionsCount = repositoryResults.Count();

			await Sut.GetQuestionQueue();

			foreach (var item in repositoryResults)
			{
				_cache.Should().ContainValueForKey(CacheKeys.PreviousQueueEntry(item.Id), item);
			}
		}
	}
}
