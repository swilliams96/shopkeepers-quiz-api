using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Services.Questions.Generation
{
	public interface IQuestionGenerationService
	{
		/// <summary>
		/// Generate the questions that will be asked.
		/// </summary>
		public Task GenerateQuestions();
	}
}
