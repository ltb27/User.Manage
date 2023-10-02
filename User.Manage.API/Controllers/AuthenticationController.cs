using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using User.Manage.API.Models;
using User.Manage.API.Models.Authentication.SignUp;
using User.Manage.API.Models.Configuration;
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
        private readonly IOptions<JWTConfiguration> jwtOptions;

        public AuthenticationController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IOptions<JWTConfiguration> jwtOptions
        )
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.emailService = emailService;
            this.jwtOptions = jwtOptions;
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

            // Send confirm email
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Authentication",
                new { token, email = user.Email },
                Request.Scheme
            );

            var content =
                @$"
                  <h1>please, Confirm Email to finish sign up process </h1>
                  <br/>
                  <div>
                     {confirmationLink}
                  </div>
            ";

            await emailService.SendEmailAsync(
                new Message(
                    to: new string[] { user.Email ?? string.Empty },
                    "Confirm your email",
                    content
                )
            );

            return Ok(
                "User created successfully and email sent to your email.Confirm Email to finish sign up process"
            );
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
                return NotFound($"User with email : {email} does not exist in system");

            var result = await userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                return Ok("Email confirmed successfully");

            return BadRequest(result.Errors);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LogIn logIn)
        {
            // Check Model State
            if (!ModelState.IsValid)
                return BadRequest();

            // Check User exists and  Check Password is correct
            var user = await userManager.FindByNameAsync(logIn.UserName);

            if (user == null || !await userManager.CheckPasswordAsync(user, logIn.Password))
                return BadRequest("Invalid username or password");

            // Generate Claims
            var claims = new List<Claim>()
            {
                new(ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add user roles to claims list
            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            // Get user token
            var token = GenerateAccessToken(claims);
            // return Token
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { accessToken, expiration = token.ValidTo });
        }

        private JwtSecurityToken GenerateAccessToken(IList<Claim> claims)
        {
            // create security signature
            var secretSignature = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey)
            );

            var accessToken = new JwtSecurityToken(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: new SigningCredentials(
                    secretSignature,
                    SecurityAlgorithms.HmacSha256
                )
            );

            return accessToken;
        }
    }
}
