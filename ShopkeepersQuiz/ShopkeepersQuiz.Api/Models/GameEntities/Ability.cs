using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Ability
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string ImageUrl { get; set; }

		[Required]
		public string Hero { get; set; }

		[Required]
		public string ManaCost { get; set; }

		[Required]
		public string Cooldown { get; set; }
	}
}
