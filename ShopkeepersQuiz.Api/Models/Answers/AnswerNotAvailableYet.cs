namespace ShopkeepersQuiz.Api.Models.Answers
{
	/// <summary>
	/// Struct to mark that the given <see cref="Answer"/> for a previous <see cref="Models.Queues.QueueEntry"/>
	/// is not yet available for users to view because the timer hasn't ended yet.
	/// </summary>
	public struct AnswerNotAvailableYet
	{
	}
}
