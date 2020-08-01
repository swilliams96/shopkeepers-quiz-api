using ShopkeepersQuiz.Api.Models.Questions;
using System;

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

		public Guid Id { get; set; }

		public Question Question { get; set; }

		public DateTime StartTimeUtc { get; set; }

		public DateTime EndTimeUtc { get; set; }
	}
}
