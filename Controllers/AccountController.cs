using Microsoft.AspNetCore.Mvc;

namespace StackFlow.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
