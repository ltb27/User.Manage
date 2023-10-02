using System.ComponentModel.DataAnnotations;

namespace User.Manage.API.Models
{
    public class LogIn
    {
        [Required(ErrorMessage = "User Name is Required")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Password is Required")]
        public string Password { get; set; } = null!;
    }
}
