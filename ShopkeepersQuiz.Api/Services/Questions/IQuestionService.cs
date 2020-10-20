using ShopkeepersQuiz.Api.Models.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions
{
	public interface IQuestionService
	{
		/// <summary>
		/// Gets the next few questions in the queue.
		/// </summary>
		public Task<IEnumerable<QueueEntry>> GetNextQuestions();
	}
}
