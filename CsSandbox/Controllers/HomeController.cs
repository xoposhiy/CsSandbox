using System.Web;
using System.Web.Mvc;

namespace CsSandbox.Controllers
{
	public class HomeController : Controller
	{
		private readonly DataManager _dataManager = new DataManager();

		public ActionResult Index()
		{
			ViewBag.Title = "Home Page";

			return View();
		}

		public ActionResult Login(string token)
		{
			if (_dataManager.FindUserId(token) == null)
				return RedirectToAction("Index", "Home", false);

			var cookie = new HttpCookie("token", token);
			Response.SetCookie(cookie);
			return RedirectToAction("All", "View");
		}

		public ActionResult LoginPage(bool isSuccess = true)
		{
			return View(isSuccess);
		}

		public ActionResult Menu()
		{
			return PartialView(_dataManager.FindUserId(Request.Cookies["token"]) != null);
		}

		public ActionResult Logout()
		{
			if (Request.Cookies["token"] != null)
			{
				Response.SetCookie(new HttpCookie("token"));
			}
			return RedirectToAction("Index", "Home");
		}
	}
}
