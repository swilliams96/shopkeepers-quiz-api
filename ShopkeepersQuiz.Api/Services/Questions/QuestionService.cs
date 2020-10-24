using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShopkeepersQuiz.Api.Models.Cache;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Models.Queue;
using ShopkeepersQuiz.Api.Repositories.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions
{
	public class QuestionService : IQuestionService
	{
		private readonly IQuestionRepository _questionRepository;
		private readonly QuestionSettings _questionSettings;
		private readonly IDistributedCache _cache;

		public QuestionService(
			IQuestionRepository questionRepository,
			IOptions<QuestionSettings> questionSettings,
			IDistributedCache cache)
		{
			_questionRepository = questionRepository;
			_questionSettings = questionSettings.Value;
			_cache = cache;
		}

		public async Task<IEnumerable<QueueEntry>> GetQuestionQueue()
		{
			int questionCount = _questionSettings.PreloadedQuestionsCount;

			List<QueueEntry> questionQueue = (await GetQuestionQueueFromCache(questionCount)).ToList();
			if (questionQueue.Count >= questionCount)
			{
				return questionQueue;
			}

			int newQuestions = await AddQuestionsToQuestionQueue(questionQueue, questionCount);
			if (newQuestions > 0)
			{
				await _cache.SetStringAsync(CacheKeys.QuestionQueue, JsonConvert.SerializeObject(questionQueue));

				// TODO: Add to the database so that correct answers can be revealed after the question has
				// finished (and provide peristence after application restarts):

				// _queueService.UpdateQueue(questionQueue);
			}

			return questionQueue;
		}

		/// <summary>
		/// Retrieves the existing question queue from the cache.
		/// </summary>
		/// <returns>A non-null <see cref="IEnumerable{QueueEntry}"/>.</returns>
		private async Task<IEnumerable<QueueEntry>> GetQuestionQueueFromCache(int questionCount)
		{
			string questionQueueJson = await _cache.GetStringAsync(CacheKeys.QuestionQueue);
			if (string.IsNullOrWhiteSpace(questionQueueJson))
			{
				return Enumerable.Empty<QueueEntry>();
			}

			IEnumerable<QueueEntry> questionQueue = JsonConvert.DeserializeObject<IEnumerable<QueueEntry>>(questionQueueJson);

			DateTime fromTime = DateTime.UtcNow.AddSeconds((double)_questionSettings.AnswerTimeSeconds * -1);

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

				extraQuestions = extraQuestions.Where(x => !existingQueue.Any(n => n.Question.Id == x.Id));

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
			afterDate ??= DateTime.UtcNow;

			var overflow = afterDate.Value.Ticks % totalQuestionTime.Ticks;

			return overflow == 0
				? afterDate.Value
				: afterDate.Value.AddTicks(totalQuestionTime.Ticks - overflow);
		}
	}
}
