using ShopkeepersQuiz.Api.Models.Questions;
using System.Collections.Generic;

namespace ShopkeepersQuiz.Api.Services
{
	public interface IQuestionService
	{
		/// <summary>
		/// Gets the next few questions in the queue.
		/// </summary>
		public IEnumerable<Question> GetNextQuestions();
	}
}
