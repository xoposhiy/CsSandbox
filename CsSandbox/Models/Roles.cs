using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsSandbox.Models
{
	public class Roles
	{
		public virtual User User { get; set; }

		[Key]
		[Column(Order = 1)]
		public string UserId { get; set; }
		[Key]
		[Column(Order = 2)]
		public Role Role { get; set; }
	}
}