using Microsoft.AspNetCore.Mvc;

namespace StackFlow.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
