using Cyriller.Model;
using Microsoft.AspNetCore.Mvc;

namespace Cyriller.Web.Controllers
{
    public class HomeController : BaseController
    {
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment;

        public HomeController(Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            ViewBag.Page = "Home.Index";

            return View();
        }

        [HttpGet("/Decline")]
        public IActionResult Decline()
        {
            return RedirectPermanent("/Decline/Phrase");
        }

        [HttpGet("/Download")]
        public IActionResult Download()
        {
            ViewBag.Page = "Home.Download";

            return View();
        }

        [HttpGet("/DownloadFile/{ID}")]
        public IActionResult DownloadFile(string ID)
        {
            ViewBag.Page = "Home.Download";

            string path = System.IO.Path.Combine(this.environment.ContentRootPath, "Files", ID);

            if (!System.IO.File.Exists(path))
            {
                return this.NotFoundView();
            }

            ViewBag.ID = ID;
            ViewBag.Url = this.Url.Content("~/Files/" + ID);

            return View();
        }

        [HttpGet("/Contacts")]
        public IActionResult Contacts()
        {
            ViewBag.Page = "Home.Contacts";

            return View();
        }

        [HttpGet("/Thanks")]
        public IActionResult Thanks()
        {
            ViewBag.Page = "Home.Thanks";

            return View();
        }

        [HttpGet("/News")]
        public IActionResult News()
        {
            ViewBag.Page = "Home.News";

            return View();
        }
    }
}
