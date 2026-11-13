using Microsoft.AspNetCore.Mvc;

namespace Cyriller.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        public IActionResult NotFoundView()
        {
            Response.StatusCode = 404;

            return View("~/Views/Error/p404.cshtml");
        }
    }
}
