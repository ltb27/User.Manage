using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using User.Manage.API.Models.Authentication.SignUp;
using User.Manage.Services.Emails;
using User.Manage.Services.Models.Emails;

namespace User.Manage.API.Controller
{
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IEmailService emailService;

        public AuthenticationController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService
        )
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.emailService = emailService;
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

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail()
        {
            var message = new Message(
                new string[] { "letuanbao@mbg8.onmicrosoft.com" },
                "Hello World",
                "Hello"
            );

            await emailService.SendEmailAsync(message);

            return Ok();
        }
    }
}
