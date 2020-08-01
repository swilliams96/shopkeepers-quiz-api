using Microsoft.Extensions.Options;
using ShopkeepersQuiz.Api.Models.Configuration;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Models.Queue;
using ShopkeepersQuiz.Api.Repositories.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services
{
	public class QuestionService : IQuestionService
	{
		private readonly IQuestionRepository _questionRepository;
		private readonly QuestionSettings _questionSettings;

		public QuestionService(IQuestionRepository questionRepository, IOptions<QuestionSettings> questionSettings)
		{
			_questionRepository = questionRepository;
			_questionSettings = questionSettings.Value;
		}

		public async Task<IEnumerable<QueueEntry>> GetNextQuestions()
		{
			int questionCount = _questionSettings.PreloadedQuestionsCount;

			// TODO: Check a local cache first before recalculating the entire question queue.

			IEnumerable<Question> questions = await _questionRepository.GetRandomQuestionsWithAnswers(questionCount);

			TimeSpan questionTime = TimeSpan.FromSeconds((double)_questionSettings.QuestionTimeSeconds);
			TimeSpan answerTime = TimeSpan.FromSeconds((double)_questionSettings.AnswerTimeSeconds);
			TimeSpan totalQuestionTime = questionTime + answerTime;

			DateTime nextQuestionStartTime = GetNextQuestionStartTimeUtc(totalQuestionTime);

			return questions.Select((question, index) =>
			{
				DateTime questionStartTime = nextQuestionStartTime.AddSeconds(totalQuestionTime.TotalSeconds * index);
				DateTime questionEndTime = questionStartTime.AddSeconds(questionTime.TotalSeconds);

				return new QueueEntry(question, questionStartTime, questionEndTime);
			});
		}

		/// <summary>
		/// Calculates the UTC datetime that the next question should begin, rounding up to the next even time interval.
		/// </summary>
		private DateTime GetNextQuestionStartTimeUtc(TimeSpan totalQuestionTime)
		{
			DateTime now = DateTime.UtcNow;

			var overflow = now.Ticks % totalQuestionTime.Ticks;

			return overflow == 0
				? now
				: now.AddTicks(totalQuestionTime.Ticks - overflow);
		}
	}
}
