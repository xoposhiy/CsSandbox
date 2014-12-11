using System.ComponentModel.DataAnnotations;

namespace CsSandbox.Models
{
	public class User
	{
		[Key]
		public string Id { get; set; }

		[Required]
		[StringLength(128)]
		public string Token { get; set; }
	}
}