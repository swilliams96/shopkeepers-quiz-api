using Microsoft.EntityFrameworkCore;
using ShopkeepersQuiz.Api.Models.Questions;
using ShopkeepersQuiz.Api.Repositories.Context;
using System;
using System.Collections.Generic;
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

		public Task<IEnumerable<Question>> GetRandomQuestions(int count)
		{
			throw new NotImplementedException();
		}
	}
}
