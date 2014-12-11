using System.ComponentModel.DataAnnotations;

namespace CsSandbox.Models
{
	public class LoginModel
	{
		[Required(ErrorMessage = "{0} — это обязательное поле")]
		public string UserName { get; set; }

		[Required(ErrorMessage = "{0} — это обязательное поле")]
		[DataType(DataType.Password)]
		public string Password { get; set; }
	}
}