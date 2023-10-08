using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Manage.API.Authorization;

namespace User.Manage.API.Controller
{
    public class AdminController : ControllerBase
    {
        public string[] UserNames { get; set; }

        public AdminController() => UserNames = new string[] { "Le Tuan Bao", "Le Van Ha" };

        [BTAuthorize(Permission = "User.GetAll")]
        [HttpPost("get-users")]
        public async Task<IActionResult> GetUsers()
        {
            await Task.Delay(1000);
            return Ok(new { UserNames });
        }
    }
}
