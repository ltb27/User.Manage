using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace User.Manage.API.Controller
{
    public class AdminController : ControllerBase
    {
        public string[] UserNames { get; set; }

        public AdminController() => UserNames = new string[] { "Le Tuan Bao", "Le Van Ha" };

        [Authorize(Roles = "Admin")]
        [HttpPost("get-users")]
        public async Task<IActionResult> GetUsers()
        {
            return Ok(new { UserNames });
        }
    }
}
