using Microsoft.AspNetCore.Mvc;
using ShopkeepersQuiz.Api.Services;
using System.Threading.Tasks;

namespace ShopkeepersQuiz.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNextQuestions()
        {
            return Ok(await _questionService.GetNextQuestions());
        }
    }
}