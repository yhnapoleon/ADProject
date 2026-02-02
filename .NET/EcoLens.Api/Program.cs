using System.Text;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;

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
			.SetIsOriginAllowed(_ => true) // 允许所有来源
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials(); // 允许凭证
	});
});

// EF Core - SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// JWT Options binding
builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
// AI Options binding
builder.Services.Configure<AiSettings>(configuration.GetSection("AiSettings"));

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
		Description = @"
## EcoLens - 可持续生活方式应用 API

EcoLens 是一个帮助用户追踪和管理个人碳排放的应用系统。

### 主要功能模块：

1. **出行记录 (Travel)**
   - 记录出行路线，自动计算距离和碳排放
   - 支持多种出行方式（步行、自行车、公交、地铁、汽车等）
   - 集成 Google Maps API 进行路线规划和地理编码

2. **水电账单 (UtilityBill)**
   - 上传账单文件，自动 OCR 识别账单信息
   - 手动输入账单数据
   - 自动计算水电使用的碳排放
   - 支持电费、水费、燃气费和综合账单

3. **活动记录 (Activity)**
   - 记录日常活动产生的碳排放
   - 支持图片识别和手动输入

4. **用户认证 (Auth)**
   - JWT Token 认证
   - 用户注册和登录

### API 文档说明：

- 所有接口都需要 JWT Token 认证（除了注册和登录接口）
- 点击右上角的 **Authorize** 按钮，输入 `Bearer {your_token}` 进行认证
- 详细的接口说明请参考各模块的前端对接文档

### 相关文档：

- 出行记录前端对接文档：`FRONTEND_API_DOC.md`
- 水电账单前端对接文档：`UTILITY_BILL_FRONTEND_API_DOC.md`
		",
		Contact = new OpenApiContact
		{
			Name = "EcoLens Development Team",
			Email = "support@ecolens.app"
		},
		License = new OpenApiLicense
		{
			Name = "MIT License",
			Url = new Uri("https://opensource.org/licenses/MIT")
		}
	});

	// 读取 XML 注释文件
	var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	if (File.Exists(xmlPath))
	{
		c.IncludeXmlComments(xmlPath);
	}

	// JWT Bearer Security Definition
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "Enter JWT below. Example: Bearer {token}",
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

	// 显式映射 IFormFile 到 Swagger 的文件上传控件
	c.MapType<IFormFile>(() => new OpenApiSchema
	{
		Type = "string",
		Format = "binary"
	});
	c.MapType<IFormFileCollection>(() => new OpenApiSchema
	{
		Type = "array",
		Items = new OpenApiSchema { Type = "string", Format = "binary" }
	});
	c.MapType<IEnumerable<IFormFile>>(() => new OpenApiSchema
	{
		Type = "array",
		Items = new OpenApiSchema { Type = "string", Format = "binary" }
	});
});

// Memory Cache
builder.Services.AddMemoryCache();

// File upload configuration
builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 10485760; // 10MB
	options.ValueLengthLimit = 10485760;
});

// DI registrations
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ISensitiveWordService, SensitiveWordService>();
builder.Services.AddScoped<ITravelService, TravelService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IDocumentTypeClassifier, DocumentTypeClassifier>();
builder.Services.AddScoped<IUtilityBillParser, UtilityBillParser>();
builder.Services.AddScoped<IUtilityBillCalculationService, UtilityBillCalculationService>();
builder.Services.AddScoped<IUtilityBillService, UtilityBillService>();

// HttpClient for Google Maps API
builder.Services.AddHttpClient();

// Google Maps Service
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();

// Caching Services
builder.Services.AddScoped<EcoLens.Api.Services.Caching.IGeocodingCacheService, EcoLens.Api.Services.Caching.GeocodingCacheService>();

// External API services
builder.Services.AddHttpClient<IClimatiqService, ClimatiqService>();
builder.Services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>();
builder.Services.AddHttpClient<IAiService, GeminiService>((sp, client) =>
{
	var options = sp.GetRequiredService<IOptions<AiSettings>>().Value;
	var baseUrl = (options.BaseUrl ?? string.Empty).TrimEnd('/');
	if (!string.IsNullOrWhiteSpace(baseUrl))
	{
		client.BaseAddress = new Uri(baseUrl + "/");
	}

	// 可按需调整
	client.Timeout = TimeSpan.FromSeconds(60);
});

// Vision settings binding & HttpClient
builder.Services.Configure<VisionSettings>(configuration.GetSection("Vision"));
builder.Services.AddHttpClient<IVisionService, PythonVisionService>((sp, client) =>
{
	var options = sp.GetRequiredService<IOptions<VisionSettings>>().Value;
	var config = sp.GetRequiredService<IConfiguration>();

	var baseUrl = (options.BaseUrl ?? "http://localhost:8000/").TrimEnd('/') + "/";
	client.BaseAddress = new Uri(baseUrl);

	var timeoutSeconds = config.GetValue<int?>("Vision:TimeoutSeconds") ?? 30;
	client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// Diet Template services
builder.Services.AddScoped<IDietTemplateService, DietTemplateService>();
builder.Services.AddScoped<IPointService, PointService>();
var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// CORS 必须在 Authentication 之前
app.UseCors(AllowAllCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// 静态文件（用于访问 wwwroot/uploads）
app.UseStaticFiles();

app.MapControllers();

app.Run();

