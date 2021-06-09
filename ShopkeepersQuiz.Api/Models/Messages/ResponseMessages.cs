namespace ShopkeepersQuiz.Api.Models.Messages
{
	public static class ResponseMessages
	{
		public static class Errors
		{
			public static string GenericError = "An error has occurred.";
			public static string GenericNotFound = "The given resource could not be found.";
			public static string GenericInvalidOperation = "The given operation could not be completed.";

			public static string AnswerNotFound = "The requested answer could not be found.";
			public static string AnswerNotAvailableYet = "The requested answer is not available yet, please try again later.";
		}
	}
}
