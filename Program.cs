using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PawVerse.Data;
using PawVerse.Models;
using PawVerse.Filters;
using OfficeOpenXml; // Added for EPPlus

var builder = WebApplication.CreateBuilder(args);

// Set EPPlus license for v8.0.6 (NonCommercial Organization)
OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("PawVerse Project");

// Ensure detailed errors are shown in Development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.CaptureStartupErrors(true);
    builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
}

// Cấu hình DbContext và kết nối đến SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm MemoryCache
builder.Services.AddMemoryCache();

// Thêm HttpClient
builder.Services.AddHttpClient();

// Đăng ký các service tùy chỉnh
builder.Services.AddScoped<PawVerse.Services.Interfaces.ILocationService, PawVerse.Services.LocationService>();
builder.Services.AddHttpClient<PawVerse.Services.Interfaces.IChatbotService, PawVerse.Services.ChatbotService>();

// Cấu hình CORS nếu cần
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Tắt xác nhận tài khoản để đơn giản
    options.User.RequireUniqueEmail = true; // Yêu cầu email phải duy nhất
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Add Google and GitHub Authentication with custom event handling
var configuration = builder.Configuration;
var googleClientId = configuration["Authentication:Google:ClientId"];
var googleClientSecret = configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";

            options.Scope.Add("profile");
            options.Scope.Add("email");

            options.ClaimActions.MapJsonKey("picture", "picture", "url");

            options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        string email = context.Principal.FindFirstValue(ClaimTypes.Email);
                        string name = context.Principal.FindFirstValue(ClaimTypes.Name);
                        string userId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        
                        var pictureClaimFromPrincipal = context.Principal.FindFirstValue("picture");
                        logger.LogInformation("Google OAuth: 'picture' claim from Principal: {PictureClaimFromPrincipal}", pictureClaimFromPrincipal);

                        string avatarUrl = context.User.TryGetProperty("picture", out var pictureElement) ? pictureElement.GetString() : null;
                        logger.LogInformation("Google OAuth: Avatar URL extracted from User JSON: {AvatarUrl}", avatarUrl);
                        
                        if (string.IsNullOrEmpty(avatarUrl)){
                            avatarUrl = pictureClaimFromPrincipal; 
                            logger.LogInformation("Google OAuth: Falling back to 'picture' claim from Principal for Avatar URL: {AvatarUrl}", avatarUrl);
                        }

                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
                        {
                            context.Fail("Could not get required user information from Google.");
                            return;
                        }

                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                UserName = email,
                                Email = email,
                                FullName = name,
                                Avatar = avatarUrl, // Use 'Avatar' property
                                EmailConfirmed = true,
                                NgayTao = DateTime.Now,
                                NgayCapNhat = DateTime.Now
                            };
                            var createResult = await userManager.CreateAsync(user);
                            if (!createResult.Succeeded)
                            {
                                throw new Exception($"Could not create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            // Kiểm tra xem tài khoản có bị khóa không
                            if (await userManager.IsLockedOutAsync(user))
                            {
                                logger.LogWarning("Google OAuth: User {Email} attempted to sign in but account is locked out", email);
                                context.Fail("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                                context.HttpContext.Response.Redirect("/Identity/Account/Lockout");
                                return;
                            }
                            
                            user.Avatar = avatarUrl; // Use 'Avatar' property
                            user.NgayCapNhat = DateTime.Now;
                            await userManager.UpdateAsync(user);
                        }

                        var loginInfo = new UserLoginInfo("Google", userId, "Google");
                        var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
                        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                        {
                            throw new Exception($"Could not add login: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                        }

                        await signInManager.SignInAsync(user, isPersistent: false);
                    }
                    catch (Exception ex)
                    {
                        context.Fail(ex);
                        logger.LogError(ex, "Error during Google authentication");
                    }
                }
            };
        })
        .AddGitHub(options =>
        {
            var gitHubAuthSection = configuration.GetSection("Authentication:GitHub");
            var gitHubClientId = gitHubAuthSection["ClientId"];
            var gitHubClientSecret = gitHubAuthSection["ClientSecret"];

            if (string.IsNullOrEmpty(gitHubClientId) || string.IsNullOrEmpty(gitHubClientSecret))
            {
                return; // Skip GitHub auth if not configured
            }

            options.ClientId = gitHubClientId;
            options.ClientSecret = gitHubClientSecret;
            options.CallbackPath = "/signin-github";

            options.Scope.Add("read:user");
            options.Scope.Add("user:email");

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
            options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

            options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        string email = context.Principal.FindFirstValue(ClaimTypes.Email);
                        string avatarUrl = context.Principal.FindFirstValue("urn:github:avatar");

                        if (string.IsNullOrEmpty(email))
                        {
                            var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                            emailRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                            emailRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                            var emailResponse = await context.Backchannel.SendAsync(emailRequest, context.HttpContext.RequestAborted);
                            if (emailResponse.IsSuccessStatusCode)
                            {
                                var emailsPayload = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync()).RootElement;
                                foreach (var emailEntry in emailsPayload.EnumerateArray())
                                {
                                    if (emailEntry.TryGetProperty("primary", out var primary) && primary.GetBoolean() &&
                                        emailEntry.TryGetProperty("verified", out var verified) && verified.GetBoolean() &&
                                        emailEntry.TryGetProperty("email", out var emailValue) && emailValue.ValueKind == JsonValueKind.String)
                                    {
                                        email = emailValue.GetString();
                                        break;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(email))
                        {
                            context.Fail("Email not available from GitHub or could not be verified.");
                            return;
                        }

                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new ApplicationUser
                            {
                                UserName = email,
                                Email = email,
                                FullName = context.Principal.FindFirstValue("urn:github:name") ?? context.Principal.FindFirstValue(ClaimTypes.Name),
                                Avatar = avatarUrl, // Use 'Avatar' property
                                EmailConfirmed = true,
                                NgayTao = DateTime.Now,
                                NgayCapNhat = DateTime.Now
                            };
                            var createResult = await userManager.CreateAsync(user);
                            if (!createResult.Succeeded)
                            {
                                throw new Exception($"Could not create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            // Kiểm tra xem tài khoản có bị khóa không
                            if (await userManager.IsLockedOutAsync(user))
                            {
                                logger.LogWarning("GitHub OAuth: User {Email} attempted to sign in but account is locked out", email);
                                context.Fail("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                                context.HttpContext.Response.Redirect("/Identity/Account/Lockout");
                                return;
                            }
                            

                            user.Avatar = avatarUrl; // Use 'Avatar' property
                            user.NgayCapNhat = DateTime.Now;
                            await userManager.UpdateAsync(user);
                        }

                        var loginInfo = new UserLoginInfo("GitHub", context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), "GitHub");
                        var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
                        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                        {
                            throw new Exception($"Could not add login: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                        }

                        await signInManager.SignInAsync(user, isPersistent: false);
                    }
                    catch (Exception ex)
                    {
                        context.Fail(ex);
                        logger.LogError(ex, "Error during GitHub authentication");
                    }
                }
            };
        });
}

// Cấu hình cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Admin/Home/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});
// Cấu hình phân quyền
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Cấu hình dịch vụ MVC
builder.Services.AddControllersWithViews();

// Đăng ký IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký CartActionFilter
builder.Services.AddScoped<CartActionFilter>();

// Thêm CartActionFilter vào tất cả các controller
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CartActionFilter>();
});

// Configure SendGrid settings and register the real email sender
builder.Services.Configure<PawVerse.Services.AuthMessageSenderOptions>(builder.Configuration.GetSection("SendGrid"));
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, PawVerse.Services.EmailSender>();

// Configure QuestPDF license (required from v2024.1)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
// Sử dụng CORS
app.UseCors("AllowAll");

// Add the AJAX exception handler middleware after CORS but before other exception handlers
app.UseMiddleware<PawVerse.Middleware.AjaxExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Thêm middleware session
app.UseAuthentication();
app.UseAuthorization();

// Route cho Area Admin
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

// Route cho Identity
app.MapRazorPages();

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Route xử lý AccessDenied
app.MapControllerRoute(
    name: "access-denied",
    pattern: "Admin/AccessDenied",
    defaults: new { area = "Admin", controller = "Home", action = "AccessDenied" });

// Route xử lý AccessDenied cho Identity
app.MapControllerRoute(
    name: "identity-access-denied",
    pattern: "Identity/Account/AccessDenied",
    defaults: new { area = "Identity", page = "/Account/AccessDenied" });

// Fallback route for areas
app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Fallback route for controllers without area
app.MapControllerRoute(
    name: "defaultRoute",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Catch-all route for 404 errors
app.MapFallbackToController("Error", "Home");
// Chạy ứng dụng
app.Run();
