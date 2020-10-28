using ShopkeepersQuiz.Api.Dtos.Queues;
using ShopkeepersQuiz.Api.Models.Answers;

namespace ShopkeepersQuiz.Api.Mappers
{
	public static class AnswerMapper
	{
		public static AnswerDto MapToDto(this Answer entity)
		{
			if (entity == null)
			{
				return null;
			}

			return new AnswerDto()
			{
				Id = entity.Id,
				Answer = entity.Text
			};
		}
	}
}
