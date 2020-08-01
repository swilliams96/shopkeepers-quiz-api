using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Item
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; }

		public string ImageUrl { get; set; }

		public int GoldCost { get; set; }
	}
}
