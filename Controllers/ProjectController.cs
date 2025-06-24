using Microsoft.AspNetCore.Mvc;

namespace StackFlow.Controllers
{
    public class ProjectController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
