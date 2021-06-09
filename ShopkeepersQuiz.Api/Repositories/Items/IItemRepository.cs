using ShopkeepersQuiz.Api.Models.GameEntities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Repositories.Items
{
	public interface IItemRepository
	{
		public Task<IEnumerable<Item>> GetAllItems();

		public Task<IEnumerable<Item>> CreateItems(IEnumerable<Item> items);

		public Task DeleteItems(IEnumerable<string> itemIds);

		public Task<Item> UpdateItem(Item item);
	}
}
