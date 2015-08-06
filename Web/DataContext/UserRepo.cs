using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using CsSandbox.Models;

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

		public List<Role> FindRoles(string token)
		{
			return
				db.Users.Where(user => user.Token == token)
					.Join(db.Roles, user => user.Id, roles => roles.UserId, (user, roles) => roles.Role)
					.ToList();
		}
	}
}