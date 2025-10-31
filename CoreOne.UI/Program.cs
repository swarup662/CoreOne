﻿using CoreOne.DOMAIN.Models;
using CoreOne.UI.Controllers;
using CoreOne.UI.Helper;
using CoreOne.UI.Middleware;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddScoped<MenuLayoutController>();
builder.Services.Configure<ApiSettingsHelper>(builder.Configuration.GetSection("ApiSettings"));
// Add this line to make ApiSettings directly injectable
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<ApiSettingsHelper>>().Value);

builder.Services.Configure<FileUploadSettings>(options =>
{
    var configPath = Path.Combine(builder.Environment.WebRootPath, "config", "FileUploadSettings.json");
    var json = File.ReadAllText(configPath);
    var settings = System.Text.Json.JsonSerializer.Deserialize<FileUploadSettings>(json);
    options.UploadModules = settings.UploadModules;
});

builder.Services.AddHttpContextAccessor(); // ✅ Needed for token access
builder.Services.AddHttpClient();

builder.Services.AddScoped<PermissionHelper>();
builder.Services.AddScoped<NotificationHelper>();

builder.Services.AddScoped<ActionPermissionHtmlProcessorUiHelper>();
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

// Named client with Bearer token
builder.Services.AddHttpClient("ApiWithToken")
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
        var token = httpContext?.Request.Cookies["jwtToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    });

var app = builder.Build();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 404)
    {
        response.Redirect($"/Home/Error?statusCode=404&errorMessage=Page not found");
    }
    else if (response.StatusCode == 403)
    {
        response.Redirect($"/Home/Error?statusCode=403&errorMessage=Access denied");
    }
    else if (response.StatusCode == 401)
    {
        response.Redirect($"/Home/Error?statusCode=401&errorMessage=Unauthorized access");
    }
});

// Configure the HTTP request pipeline.
// Middleware order matters
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseStaticFiles(); 

app.UseRouting();
app.UseMiddleware<UiErrorLoggingMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>(); // Your token expiry middleware
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}");



app.Run();
