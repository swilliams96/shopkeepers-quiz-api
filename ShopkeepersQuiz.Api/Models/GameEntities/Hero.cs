using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Hero
	{
		[BsonId]
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string WikiPageUrl { get; set; }

		// Navigiation Properties

		public ICollection<Ability> Abilities { get; set; } = new List<Ability>();
	}
}
