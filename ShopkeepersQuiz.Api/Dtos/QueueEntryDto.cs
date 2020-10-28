using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.Collections.Generic;

namespace ShopkeepersQuiz.Api.Dtos.Queues
{
	public class QueueEntryDto
	{
		public Guid Id { get; set; }

		public string Key { get; set; }

		public QuestionType Type { get; set; }

		public string Question { get; set; }

		public IEnumerable<AnswerDto> Answers { get; set; }

		public string ImageUrl { get; set; }
	}
}
