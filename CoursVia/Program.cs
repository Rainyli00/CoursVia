using CoursVia.Data;
using CoursVia.Data.Seed;
using CoursVia.Services;
using CoursVia.Services.Ai;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF lisans ayarı
QuestPDF.Settings.License = LicenseType.Community;
    
// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<KullaniciHesapService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IpAdresService>();
builder.Services.AddScoped<AdminLogService>();
builder.Services.AddScoped<BildirimService>();

// Demo Seed Services
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<DemoKategoriSeeder>();
builder.Services.AddScoped<DemoKullaniciSeeder>();
builder.Services.AddScoped<DemoKursSeeder>();
builder.Services.AddScoped<DemoOgrenciHareketSeeder>();
builder.Services.AddScoped<DemoSinavSeeder>();
builder.Services.AddScoped<DemoSistemSeeder>();
builder.Services.AddScoped<DemoOturumSeeder>();

// AI Settings
builder.Services.Configure<AiSettings>(
    builder.Configuration.GetSection("AiSettings"));

// AI Services
builder.Services.AddScoped<AiPromptBuilder>();
builder.Services.AddScoped<AiCiktiGuvenlikFiltresi>();

// Gemini -> Google.GenAI SDK
builder.Services.AddScoped<GeminiAiService>();

// Local Gemma -> OpenAI SDK + LM Studio
builder.Services.AddScoped<LocalGemmaAiService>();

// MiniCoursViaLLM -> Python process
builder.Services.AddScoped<MiniCoursViaAiService>();

// Ana AI analiz servisi
builder.Services.AddScoped<AiAnalizService>();
builder.Services.AddScoped<AiOneriService>();

// Web için Cookie Authentication, Mobil API için JWT Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/OgrenciLogin";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                string path = context.Request.Path.Value ?? string.Empty;
                string returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                string loginPath = "/Account/OgrenciLogin";

                if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                {
                    loginPath = "/Account/AdminLogin";
                }
                else if (path.StartsWith("/Egitmen", StringComparison.OrdinalIgnoreCase))
                {
                    loginPath = "/Account/EgitmenLogin";
                }

                context.Response.Redirect($"{loginPath}?ReturnUrl={Uri.EscapeDataString(returnUrl)}");

                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        string jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key appsettings.json içinde bulunamadı.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// MVC + Razor Runtime Compilation
var mvcBuilder = builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

var app = builder.Build();

// Development ortamında demo verileri ekle
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();

    await demoDataSeeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Mobil APK local HTTP kullandığı için Development ortamında HTTPS zorlamıyoruz.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Authentication / Authorization sırası önemli
app.UseAuthentication();
app.UseAuthorization();

// Attribute route kullanan API controllerları için gerekli
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();