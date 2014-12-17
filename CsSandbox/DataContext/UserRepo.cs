using System.Data.Entity;

namespace CsSandbox.DataContext
{
	public class UserRepo
	{
		private readonly CsSandboxDb db;

		public UserRepo() : this(new CsSandboxDb())
		{
			
		}

		private UserRepo(CsSandboxDb db)
		{
			this.db = db;
		}

		public string FindUser(string token)
		{
			var userInfo = db.Users.FirstOrDefaultAsync(user => user.Token == token).Result;
			return userInfo == null ? null : userInfo.Id;
		}
	}
}