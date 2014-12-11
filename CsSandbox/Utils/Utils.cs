using System;
using System.Linq;
using System.Security.Cryptography;
using CsSandbox.DataContext;
using CsSandbox.Models;
using NUnit.Framework;

namespace CsSandbox.Utils
{
	public static class Utils
	{
		[Test]
		[Explicit]
		public static async void AddUser()
		{
			var db = new CsSandboxDb();
			var userId = Guid.NewGuid().ToString();
			var tokenBytes = new byte[64];
			new RNGCryptoServiceProvider().GetBytes(tokenBytes);
			var token = BitConverter.ToString(tokenBytes).Replace("-", "").ToLower();
			db.Users.Add(new User
			{
				Id = userId,
				Token = token
			});
			await db.SaveChangesAsync();
			Console.WriteLine(token);
		}

		[Test]
		[Explicit]
		public static void AllUsersToken()
		{
			var db = new CsSandboxDb();
			var tokens = db.Users.Select(user => user.Token).ToList();
			foreach (var token in tokens)
			{
				Console.Out.WriteLine(token);
			}
		}
	}
}