using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Hero
	{
		public Hero(string name, string wikiPageUrl)
		{
			Id = Guid.NewGuid();
			Name = name;
			WikiPageUrl = wikiPageUrl;
		}

		[BsonId]
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string WikiPageUrl { get; set; }

		public ICollection<Ability> Abilities { get; set; } = new List<Ability>();
	}
}
