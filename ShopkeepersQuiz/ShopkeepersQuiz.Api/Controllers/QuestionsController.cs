using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ShopkeepersQuiz.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuestionsController : Controller
    {
        [HttpGet]
        public IActionResult GetLiveQuestion()
        {
            return Ok();
        }
    }
}