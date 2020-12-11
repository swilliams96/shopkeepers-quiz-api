using AutoFixture;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Queues;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.Queues;
using ShopkeepersQuiz.Api.Services.Questions;
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
		private readonly Mock<IQueueRepository> _mockQueueRepository = new Mock<IQueueRepository>();
		private readonly Mock<IMemoryCache> _mockCache = new Mock<IMemoryCache>();
		private readonly Mock<DateTimeProvider> _mockDateTimeProvider = new Mock<DateTimeProvider>();

		private readonly QuestionSettings _questionSettings = new QuestionSettings();

		private readonly QuestionService Sut;

		public QuestionServiceTests()
		{
			Sut = new QuestionService(
				_mockQuestionRepository.Object,
				_mockQueueRepository.Object,
				Options.Create(_questionSettings),
				_mockCache.Object,
				_mockDateTimeProvider.Object);

			_mockDateTimeProvider.Setup(x => x.GetUtcNow()).Returns(UtcNowFixed);
		}

		[Fact]
		public async Task GetQuestionQueue_CacheReturnsEnoughQuestions_CachedQuestionsReturnedSuccessfully()
		{
			IEnumerable<QueueEntry> cacheResults = _fixture.Build<QueueEntry>()
				.With(x => x.StartTimeUtc, UtcNowFixed.AddSeconds(30))
				.With(x => x.EndTimeUtc, UtcNowFixed.AddSeconds(45))
				.CreateMany();
			object cacheResultsJson = JsonConvert.SerializeObject(cacheResults);

			// We can't mock extension methods but Get<> calls TryGetValue under the hood, so we can mock this instead
			_mockCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cacheResultsJson)).Returns(true);

			_mockQueueRepository.Setup(x => x.GetUpcomingQueueEntries())
				.Throws(new AssertionFailedException("QueueRepository should not be called in this scenario."));

			_questionSettings.PreloadedQuestionsCount = cacheResults.Count();

			var result = await Sut.GetQuestionQueue();

			result.Should().HaveCount(cacheResults.Count());
			result.Should().BeEquivalentTo(cacheResults);
		}
	}
}
