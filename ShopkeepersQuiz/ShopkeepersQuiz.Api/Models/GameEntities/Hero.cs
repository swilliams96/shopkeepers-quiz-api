using Flurl;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Hero
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string WikiPageUrl { get; set; }

		// Navigiation Properties

		public ICollection<Ability> Abilities { get; set; }
	}
}
