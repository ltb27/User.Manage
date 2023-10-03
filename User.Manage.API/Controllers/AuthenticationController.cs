using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IEmailService emailService;
        private readonly IOptions<JWTConfiguration> jwtOptions;

        public AuthenticationController(
            UserManager<ApplicationUser> userManager,
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
            var user = new ApplicationUser()
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
            ApplicationUser? user = await userManager.FindByNameAsync(logIn.UserName);

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
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(
                jwtOptions.Value.RefreshTokenValidityInDays
            );

            await userManager.UpdateAsync(user);

            return Ok(
                new
                {
                    AccessToken = accessToken,
                    Expiration = token.ValidTo,
                    RefreshToken = refreshToken,
                }
            );
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            if (tokenModel == null || !ModelState.IsValid)
                return BadRequest();

            string? accessToken = tokenModel.AccessToken;
            string? refreshToken = tokenModel.RefreshToken;

            // get claims from expired access token
            var claimsPrincipal = GetPrincipalFromExpiredToken(accessToken);
            if (claimsPrincipal == null)
                return BadRequest("Invalid access token or refresh token");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string username = claimsPrincipal.Identity.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            ApplicationUser user = await userManager.FindByNameAsync(username);

            if (
                user == null
                || user.RefreshToken != refreshToken
                || user.RefreshTokenExpiryTime <= DateTime.Now
            )
                return BadRequest("Invalid access token or refresh token");

            // generate new access token and refresh token
            var newAccessToken = GenerateAccessToken(claimsPrincipal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;

            await userManager.UpdateAsync(user);

            return Ok(
                new
                {
                    AccessToken = newAccessToken,
                    Expiration = newAccessToken.ValidTo,
                    RefreshToken = newRefreshToken,
                }
            );
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
                expires: DateTime.Now.AddMinutes(jwtOptions.Value.TokenValidityInMinutes),
                signingCredentials: new SigningCredentials(
                    secretSignature,
                    SecurityAlgorithms.HmacSha256
                )
            );

            return accessToken;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var secretKey = jwtOptions.Value.SecretKey;
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Value.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(
                token,
                tokenValidationParameters,
                out SecurityToken securityToken
            );

            if (
                securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
