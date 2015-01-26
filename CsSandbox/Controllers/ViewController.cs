using System.Linq;
using System.Web.Mvc;
using CsSandbox.Models;

namespace CsSandbox.Controllers
{
	public class ViewController : Controller
	{
		private readonly DataManager _dataManager = new DataManager();

		public ActionResult All(int max = 200, int skip = 0)
		{
			return View(new ViewModel
			{
				Max = max, 
				Skip = skip
			});
		}

		public ActionResult SubmissionsList(int max = 200, int skip = 0)
		{
			var submissions = _dataManager
				.GetAllSubmission(Request.Cookies["token"], max, skip)
				.Select(details => details.ToPublic());
			return PartialView(new SubmissionsListModel
			{
				Submissions = submissions
			});
		}

		public ActionResult GetDetails(string id)
		{
			return View(_dataManager.GetDetails(id, Request.Cookies["token"]).ToPublic());
		}

	}
}