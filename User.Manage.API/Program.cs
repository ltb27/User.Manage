using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using User.Manage.API.Models;
using User.Manage.API.Models.Configuration;
using User.Manage.Services.Emails;
using User.Manage.Services.Models.Emails;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// configurations
var configuration = builder.Configuration;

// for email configuration
builder.Services.Configure<EmailConfiguration>(configuration.GetSection("SmtpConfiguration"));

// For JWT configuration
builder.Services.Configure<JWTConfiguration>(configuration.GetSection("JWT"));

// For Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// For Identity Sign In Options. Require confirm email when signing up to be able to sign in
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
});

// For EFCore
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"))
);

// For Identity
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// For Authentication
var jwtConfiguration = builder.Configuration.GetSection("JWT").Get<JWTConfiguration>();
var secretKey = jwtConfiguration.SecretKey;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// config authentication default scheme
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    //  config options for jwt bear token authentication handler
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        //   config the way to validate token
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfiguration.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        //   config events handler for jwt bear token authentication procedure
        //   default behavior of jwt token handler will get the token from header(Authorization)
        //   config this event to get the token from other place
        options.Events = new JwtBearerEvents()
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Query.TryGetValue("access_token", out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

// For Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// build the host
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
