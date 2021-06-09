using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using ShopkeepersQuiz.Api.Dtos;
using ShopkeepersQuiz.Api.Mappers;
using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.Cache;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Models.Queues;
using ShopkeepersQuiz.Api.Repositories.Questions;
using ShopkeepersQuiz.Api.Repositories.QueueEntries;
using ShopkeepersQuiz.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions
{
	public class QuestionService : IQuestionService
	{
		const int TimeDriftBufferSeconds = 1;

		private readonly IQuestionRepository _questionRepository;
		private readonly IQueueEntryRepository _queueEntryRepository;
		private readonly QuestionSettings _questionSettings;
		private readonly IMemoryCache _cache;
		private readonly DateTimeProvider _dateTimeProvider;

		public QuestionService(
			IQuestionRepository questionRepository,
			IQueueEntryRepository queueRepository,
			IOptions<QuestionSettings> questionSettings,
			IMemoryCache cache,
			DateTimeProvider dateTimeProvider)
		{
			_questionRepository = questionRepository;
			_queueEntryRepository = queueRepository;
			_questionSettings = questionSettings.Value;
			_cache = cache;
			_dateTimeProvider = dateTimeProvider;
		}

		public async Task<IEnumerable<QueueEntry>> GetQuestionQueue()
		{
			int questionCount = _questionSettings.PreloadedQuestionsCount;

			List<QueueEntry> questionQueue = GetQuestionQueueFromCache(questionCount).ToList();
			if (questionQueue.Count >= questionCount)
			{
				return questionQueue;
			}

			questionQueue = (await _queueEntryRepository.GetUpcomingQueueEntries()).ToList();
			if (questionQueue.Count >= questionCount)
			{
				_cache.Set(CacheKeys.QuestionQueue, questionQueue);
			}
			else
			{
				int newQuestions = await AddQuestionsToQuestionQueue(questionQueue, questionCount);
				if (newQuestions > 0)
				{
					_cache.Set(CacheKeys.QuestionQueue, questionQueue);

					try
					{
						await UpdateQueue(questionQueue);
					}
					catch
					{
						_cache.Remove(CacheKeys.QuestionQueue);
					}
				}
			}

			// Store the correct answers in the cache separately to easily retrieve the answer for when the question ends
			foreach (var queueEntry in questionQueue)
			{
				string cacheKey = CacheKeys.PreviousQueueEntry(queueEntry.Id);
				DateTimeOffset cacheExpiry = queueEntry.EndTimeUtc.AddSeconds((double)_questionSettings.QuestionTimeSeconds * 5);

				_cache.Set(cacheKey, queueEntry, cacheExpiry);
			}

			return questionQueue;
		}

		public async ValueTask<OneOf<AnswerDto, NotFound, AnswerNotAvailableYet>> GetPreviousQueueEntryAnswer(Guid queueEntryId)
		{
			string answerCacheKey = CacheKeys.PreviousQueueEntry(queueEntryId);

			if (_cache.TryGetValue(answerCacheKey, out QueueEntry entry))
			{
				DateTime now = _dateTimeProvider.GetUtcNow();
				TimeSpan timeUntilAnswerAvailable = entry.EndTimeUtc - now - TimeSpan.FromSeconds(TimeDriftBufferSeconds);
				TimeSpan timeUntilQuestionStarts = entry.StartTimeUtc - now - TimeSpan.FromSeconds(TimeDriftBufferSeconds);

				if (timeUntilAnswerAvailable < TimeSpan.Zero)
				{
					return entry.CorrectAnswer.MapToDto();
				}
				else if (timeUntilQuestionStarts < TimeSpan.Zero)
				{
					// Hold the response until the answer is available if the question has started
					await Task.Delay(timeUntilAnswerAvailable);
					return entry.CorrectAnswer.MapToDto();
				}

				return new AnswerNotAvailableYet();
			}

			return new NotFound();
		}

		/// <summary>
		/// Retrieves the existing question queue from the cache.
		/// </summary>
		/// <returns>A non-null <see cref="IEnumerable{QueueEntry}"/>.</returns>
		private IEnumerable<QueueEntry> GetQuestionQueueFromCache(int questionCount)
		{
			if (!_cache.TryGetValue(CacheKeys.QuestionQueue, out IEnumerable<QueueEntry> questionQueue))
			{
				return Enumerable.Empty<QueueEntry>();
			}

			DateTime fromTime = _dateTimeProvider.GetUtcNow().AddSeconds((double)_questionSettings.AnswerTimeSeconds * -1);

			return questionQueue.Where(x => x.StartTimeUtc >= fromTime).Take(questionCount);
		}

		/// <summary>
		/// Attempts to add some new questions to the provided question queue, which haven't been already asked in the queue.
		/// The process is random so any number of questions could be added from 0 to the provided question count.
		/// </summary>
		/// <param name="existingQueue">The existing queue to add new questions to.</param>
		/// <param name="questionCount">The maximum number of questions to add to the queue.</param>
		/// <returns>The number of questions added to the queue.</returns>
		private async Task<int> AddQuestionsToQuestionQueue(List<QueueEntry> existingQueue, int questionCount)
		{
			if (existingQueue == null)
			{
				throw new ArgumentNullException(nameof(existingQueue));
			}

			int addedQuestionsCount = 0;

			TimeSpan questionTime = TimeSpan.FromSeconds((double)_questionSettings.QuestionTimeSeconds);
			TimeSpan answerTime = TimeSpan.FromSeconds((double)_questionSettings.AnswerTimeSeconds);
			TimeSpan totalQuestionTime = questionTime + answerTime;

			DateTime? latestQuestionStartTime = existingQueue.OrderBy(x => x.StartTimeUtc).LastOrDefault()?.EndTimeUtc;
			DateTime nextQuestionStartTime = GetNextQuestionStartTimeUtc(totalQuestionTime, latestQuestionStartTime);

			int attempts = 0;
			while (existingQueue.Count < questionCount)
			{
				if (attempts++ > 10)
				{
					break;
				}

				IEnumerable<Question> extraQuestions = await _questionRepository.GetRandomQuestionsWithAnswers(questionCount);

				extraQuestions = extraQuestions.Where(x => !existingQueue.Any(n => n.QuestionId == x.Id));

				foreach (var question in extraQuestions)
				{
					DateTime questionStartTime = nextQuestionStartTime;
					DateTime questionEndTime = questionStartTime.AddSeconds(questionTime.TotalSeconds);

					existingQueue.Add(new QueueEntry(question, questionStartTime, questionEndTime));

					nextQuestionStartTime = questionStartTime.Add(totalQuestionTime);
					addedQuestionsCount++;
				}
			}

			return addedQuestionsCount;
		}

		/// <summary>
		/// Calculates the UTC DateTime that the next question should begin, rounding up to the next even time interval.
		/// </summary>
		private DateTime GetNextQuestionStartTimeUtc(TimeSpan totalQuestionTime, DateTime? afterDate = null)
		{
			afterDate ??= _dateTimeProvider.GetUtcNow();

			var overflow = afterDate.Value.Ticks % totalQuestionTime.Ticks;

			return overflow == 0
				? afterDate.Value
				: afterDate.Value.AddTicks(totalQuestionTime.Ticks - overflow);
		}

		/// <summary>
		/// Stores any untracked queue entries from the provided queue in the database.
		/// </summary>
		private async Task UpdateQueue(IEnumerable<QueueEntry> questionQueue)
		{
			if (questionQueue == null || !questionQueue.Any())
			{
				throw new ArgumentNullException(nameof(questionQueue));
			}

			IEnumerable<QueueEntry> existingQueueEntries = await _queueEntryRepository.GetUpcomingQueueEntries();

			IEnumerable<QueueEntry> queueEntriesToAdd = questionQueue
				.Where(x => x.StartTimeUtc >= _dateTimeProvider.GetUtcNow())
				.Where(x => !existingQueueEntries.Any(existing => x.Id == existing.Id));

			foreach (var entry in queueEntriesToAdd)
			{
				await _queueEntryRepository.CreateQueueEntry(entry);
			}
		}
	}
}
