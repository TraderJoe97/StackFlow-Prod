using Microsoft.AspNetCore.Mvc;

namespace StackFlow.ApiControllers
{
    public class TicketsAPIController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
