using Microsoft.AspNetCore.Mvc;

namespace Cyriller.Web.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult p404()
        {
            Response.StatusCode = 404;

            return View();
        }

        public IActionResult p403()
        {
            Response.StatusCode = 403;

            return View();
        }

        public IActionResult p500()
        {
            Response.StatusCode = 500;

            return View();
        }
    }
}
