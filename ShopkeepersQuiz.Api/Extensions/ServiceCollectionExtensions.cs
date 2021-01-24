using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using ShopkeepersQuiz.Api.Models.Configuration;
using System;

namespace ShopkeepersQuiz.Api.Extensions
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Connects to MongoDB and adds the <see cref="IMongoDatabase"/> instance to the service collection.
		/// </summary>
		public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
		{
			MongoDefaults.AssignIdOnInsert = true;

			string mongoConnectionString = configuration.GetConnectionString(nameof(ConnectionStrings.MongoDb));

			var mongoDatabaseName = MongoUrl.Create(mongoConnectionString).DatabaseName
				?? throw new ArgumentException("MongoDB connection string must include a database.");

			var mongoClient = new MongoClient(mongoConnectionString);
			var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

			bool connectionSuccessful = mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(5000);
			if (!connectionSuccessful)
			{
				throw new InvalidOperationException("Connection to MongoDB could not be established.");
			}

			services.AddSingleton(mongoDatabase);

			return services;
		}
	}
}
