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

		public string GetUser(string token)
		{
			return db.Users.FirstOrDefaultAsync(user => user.Token == token).Result.Id;
		}
	}
}