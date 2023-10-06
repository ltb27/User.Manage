using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using User.Manage.API.Models;
using User.Manage.API.Models.Configuration;
using User.Manage.Services.Emails;
using User.Manage.Services.Models.Emails;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// configurations
var configuration = builder.Configuration;
var env = builder.Environment;

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
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settingscc
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        // User settings
        options.User.RequireUniqueEmail = true;
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 10;
    })
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

// For Authorization policy configurations
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CheckPermission",
        policy =>
        {
            policy.Requirements.Add(new CheckPermissionRequirement());
        }
    );
});

// For Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(s =>
{
    s.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Version = "v1",
            Title = "EcomSea API",
            Description = "Api for EcomSea App",
        }
    );

    s.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        }
    );
    s.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});

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
