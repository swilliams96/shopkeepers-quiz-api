using ShopkeepersQuiz.Api.Models.Questions;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.Queue
{
	public class QueueEntry
	{
		public QueueEntry(Question question, DateTime startTimeUtc, DateTime endTimeUtc)
		{
			Id = Guid.NewGuid();
			Question = question;
			StartTimeUtc = startTimeUtc;
			EndTimeUtc = endTimeUtc;
		}

		private QueueEntry()
		{
			// Necessary for EF Core
		}

		[Key]
		public Guid Id { get; set; }

		public int QuestionId { get; set; }

		public DateTime StartTimeUtc { get; set; }

		public DateTime EndTimeUtc { get; set; }

		// Navigation Properties

		public Question Question { get; set; }
	}
}
