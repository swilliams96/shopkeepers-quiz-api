using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Item
	{
		public Item(string name, string imageUrl, int goldCost)
		{
			Id = Guid.NewGuid();
			Name = name;
			ImageUrl = imageUrl;
			GoldCost = goldCost;
		}

		[BsonId]
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string ImageUrl { get; set; }

		public int GoldCost { get; set; }
	}
}
