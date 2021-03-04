using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Ability
	{
		[BsonId]
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string ImageUrl { get; set; }

		public Guid HeroId { get; set; }

		public string ManaCost { get; set; }

		public string Cooldown { get; set; }

		// Navigation Properties

		[BsonIgnore]
		public Hero Hero { get; private set; }
	}
}
