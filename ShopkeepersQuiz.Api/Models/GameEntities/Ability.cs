using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Ability
	{
		public Ability(string name, Guid heroId, string imageUrl, string manaCost, string cooldown)
		{
			Id = Guid.NewGuid();
			Name = name;
			HeroId = heroId;
			ImageUrl = imageUrl;
			ManaCost = manaCost;
			Cooldown = cooldown;
		}

		[BsonId]
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string ImageUrl { get; set; }

		public Guid HeroId { get; set; }

		public string ManaCost { get; set; }

		public string Cooldown { get; set; }
	}
}
