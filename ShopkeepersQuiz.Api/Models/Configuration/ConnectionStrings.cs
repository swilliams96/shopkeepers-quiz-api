namespace ShopkeepersQuiz.Api.Models.Configuration
{
	/// <summary>
	/// Connection strings for the application.
	/// </summary>
	public class ConnectionStrings
	{
		/// <summary>
		/// Connection string to the application's SQL Server database
		/// </summary>
		public string ApplicationDatabase { get; set; }

		/// <summary>
		/// Connection string to the application's Mongo DB data store
		/// </summary>
		public string MongoDb { get; set; }
	}
}
