using ShopkeepersQuiz.Api.Models.Questions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Questions
{
	public interface IQuestionRepository
	{
		/// <summary>
		/// Gets a list of random questions from the database.
		/// </summary>
		/// <param name="count">The number of questions to retrieve.</param>
		public Task<IEnumerable<Question>> GetRandomQuestionsWithAnswers(int count);

		/// <summary>
		/// Gets all the questions that relate to the ability with the given ID.
		/// </summary>
		public Task<IEnumerable<Question>> GetQuestionsForAbility(int abilityId);
	}
}
