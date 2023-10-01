using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManage.API.Models.Authentication.SignUp;

namespace UserManage.API.Controller
{
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AuthenticationController(
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            this.configuration = configuration;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUser registerUser,
            [FromQuery] string role
        )
        {
            // Validate model
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await roleManager.RoleExistsAsync(role))
                return BadRequest($"the role : {role} does not exist");

            // Check User exists
            if (await userManager.Users.AnyAsync(u => u.UserName == registerUser.UserName))
                return BadRequest("User already exists");

            // Create new User
            var user = new IdentityUser()
            {
                Email = registerUser.Email,
                UserName = registerUser.UserName,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(user, registerUser.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign a role to User
            await userManager.AddToRoleAsync(user, role);

            return Ok("User created successfully");
        }
    }
}
