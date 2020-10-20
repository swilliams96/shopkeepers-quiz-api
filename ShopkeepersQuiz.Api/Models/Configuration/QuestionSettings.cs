namespace ShopkeepersQuiz.Api.Models.Configuration
{
	/// <summary>
	/// Configuration for the question settings.
	/// </summary>
	public class QuestionSettings
	{
		/// <summary>
		/// The number of questions to preload when the queue is empty.
		/// </summary>
		public int PreloadedQuestionsCount { get; set; }

		/// <summary>
		/// The amount of time that users should have to answer a question.
		/// </summary>
		public decimal QuestionTimeSeconds { get; set; }

		/// <summary>
		/// The amount of time between question rounds where users are shown the answer from the last round.
		/// </summary>
		public decimal AnswerTimeSeconds { get; set; }

		/// <summary>
		/// The number of incorrect questions that are generated for each question.
		/// </summary>
		public int IncorrectAnswersGenerated { get; set; } = 5;
	}
}
