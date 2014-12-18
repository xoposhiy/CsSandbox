using System;
using System.Linq;
using System.Web.Mvc;
using CsSandbox.Models;

namespace CsSandbox.Controllers
{
	public class ViewController : Controller
	{
		private readonly DataManager _dataManager = new DataManager();

		public ActionResult All(string token)
		{
			return View((object)token);
		}

		public ActionResult SubmissionsList(string token, int max = Int32.MaxValue, int skip = 0)
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