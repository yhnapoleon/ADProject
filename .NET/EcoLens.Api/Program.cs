using System.Text;
using EcoLens.Api.Data;
using EcoLens.Api.Services;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Controllers
builder.Services.AddControllers();

// CORS (Allow all for development)
const string AllowAllCorsPolicy = "AllowAll";
builder.Services.AddCors(options =>
{
	options.AddPolicy(AllowAllCorsPolicy, policy =>
	{
		policy
			.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

// EF Core - SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// JWT Options binding
builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

// JWT Authentication
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		var issuer = configuration["Jwt:Issuer"];
		var audience = configuration["Jwt:Audience"];
		var key = configuration["Jwt:Key"];

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = issuer,
			ValidAudience = audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? string.Empty))
		};
	});

// Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "EcoLens API",
		Version = "v1",
		Description = "Sustainable lifestyle application API"
	});

	// JWT Bearer Security Definition
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "在下方输入 JWT。示例：Bearer {token}",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
	});
});

// DI registrations
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors(AllowAllCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// 静态文件（用于访问 wwwroot/uploads）
app.UseStaticFiles();

app.MapControllers();

app.Run();

