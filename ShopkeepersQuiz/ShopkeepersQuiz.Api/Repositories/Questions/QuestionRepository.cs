using Microsoft.EntityFrameworkCore;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Questions
{
	public class QuestionRepository : IQuestionRepository
	{
		private readonly ApplicationDbContext _context;

		public QuestionRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Question>> GetRandomQuestionsWithAnswers(int count)
		{
			return await _context.Questions
				.Include(x => x.Answers)
				.OrderBy(x => Guid.NewGuid())
				.Take(count)
				.ToListAsync();
		}
	}
}
