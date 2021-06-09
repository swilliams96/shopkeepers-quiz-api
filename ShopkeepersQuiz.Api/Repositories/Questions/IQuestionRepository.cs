using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Questions
{
	public interface IQuestionRepository
	{
		/// <summary>
		/// Gets a list of random <see cref="Question"/>s from the database.
		/// </summary>
		/// <param name="count">The number of <see cref="Question"/>s to retrieve.</param>
		public Task<IEnumerable<Question>> GetRandomQuestionsWithAnswers(int count);

		/// <summary>
		/// Gets all the <see cref="Question"/>s that relate to the <see cref="Models.GameEntities.Ability"/> with the given ID.
		/// </summary>
		public Task<IEnumerable<Question>> GetQuestionsForAbility(Guid abilityId);

		/// <summary>
		/// Adds the given <see cref="Question"/> to the database.
		/// </summary>
		/// <param name="question">The <see cref="Question"/> to create.</param>
		public Task<Question> CreateQuestion(Question question);

		/// <summary>
		/// Deletes the single <see cref="Question"/> with the given ID.
		/// </summary>
		/// <param name="questionId">The ID of the <see cref="Question"/> to delete.</param>
		public Task DeleteQuestion(Guid questionId);

		/// <summary>
		/// Updates a single <see cref="Question"/> with a matching ID.
		/// </summary>
		/// <param name="question">The <see cref="Question"/> to update.</param>
		public Task<Question> UpdateQuestion(Question question);
	}
}
