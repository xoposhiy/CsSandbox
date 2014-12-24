using System.Linq;
using CsSandbox.DataContext;
using CsSandbox.Models;

namespace CsSandbox.Migrations
{
	using System.Data.Entity.Migrations;


	internal sealed class Configuration : DbMigrationsConfiguration<CsSandboxDb>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(CsSandboxDb context)
        {
			//  This method will be called after migrating to the latest version.

	        if (!context.Users.Any(user => user.Id == "tester"))
	        {
		        var db = new CsSandboxDb();
		        db.Users.Add(new User
		        {
					Id = "tester",
					Token = "tester"
		        });
		        db.SaveChanges();
	        }

			if (!context.Users.Any(user => user.Id == "tester2"))
			{
				var db = new CsSandboxDb();
				db.Users.Add(new User
				{
					Id = "tester2",
					Token = "tester2"
				});
				db.SaveChanges();
			}

	        if (!context.Users.Any(user => user.Id == "runner"))
	        {
				var db = new CsSandboxDb();
				db.Users.Add(new User
				{
					Id = "runner",
					Token = "runner"
				});
				db.SaveChanges();
			}

	        if (!context.Roles.Any(roles => roles.UserId == "runner" && roles.Role == Role.Runner))
	        {
		        var db = new CsSandboxDb();
		        db.Roles.Add(new Roles
		        {
			        UserId = "runner",
			        Role = Role.Runner
		        });
		        db.SaveChanges();
	        }

	        //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
