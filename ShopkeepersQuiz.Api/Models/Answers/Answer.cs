using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopkeepersQuiz.Api.Models.Answers
{
	public class Answer
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public int QuestionId { get; set; }

		[Required]
		public string Text { get; set; }

		public bool Correct { get; set; }
	}
}
