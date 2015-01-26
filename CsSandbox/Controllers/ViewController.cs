using System;
using System.Linq;
using System.Web.Mvc;
using CsSandbox.Models;

namespace CsSandbox.Controllers
{
	public class ViewController : Controller
	{
		private readonly DataManager _dataManager = new DataManager();

		public ActionResult All(string token, int max = 200, int skip = 0)
		{
			return View(new ViewModel
			{
				Token = token, 
				Max = max, 
				Skip = skip
			});
		}

		public ActionResult SubmissionsList(string token, int max = 200, int skip = 0)
		{
			var submissions = _dataManager
				.GetAllSubmission(token, max, skip)
				.Select(details => details.ToPublic());
			return PartialView(new SubmissionsListModel
			{
				Token = token,
				Submissions = submissions
			});
		}

		public ActionResult GetDetails(string token, string id)
		{
			return View(_dataManager.GetDetails(id, token).ToPublic());
		}

	}
}