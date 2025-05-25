using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // return View("~/Views/Admin/Dashboard/Index.cshtml");
            return View();
        }
    }
}
