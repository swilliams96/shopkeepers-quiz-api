using ShopkeepersQuiz.Api.Models.Answers;
using ShopkeepersQuiz.Api.Models.GameEntities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopkeepersQuiz.Api.Models.Questions
{
	public class Question
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		public string Key { get; set; }

		[Required]
		public string Text { get; set; }

		public QuestionType Type { get; set; }

		public int? AbilityId { get; set; }

		// Navigation Properties

		public ICollection<Answer> Answers { get; set; }

		public Ability Ability { get; private set; }
	}
}
