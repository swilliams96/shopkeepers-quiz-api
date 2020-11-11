using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopkeepersQuiz.Api.Mappers;
using ShopkeepersQuiz.Api.Services.Questions;
using System.Linq;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNextQuestions()
        {
            var result = await _questionService.GetQuestionQueue();
            return Ok(result.Select(x => x.MapToDto()));
        }
    }
}