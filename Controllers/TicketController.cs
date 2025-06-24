using Microsoft.AspNetCore.Mvc;

namespace StackFlow.Controllers
{
    public class TaskController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
