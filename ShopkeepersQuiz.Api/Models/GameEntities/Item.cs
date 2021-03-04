using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopkeepersQuiz.Api.Models.GameEntities
{
	public class Item
	{
		[BsonId]
		public Guid Id { get; set; }

		[Required]
		public string Name { get; set; }

		public string ImageUrl { get; set; }

		public int GoldCost { get; set; }
	}
}
