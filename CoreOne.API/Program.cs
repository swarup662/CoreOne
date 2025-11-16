using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.API.Middleware;
using CoreOne.API.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 🧩 CONFIGURATION
// ======================================================
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ======================================================
// 🧩 SERVICES
// ======================================================
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache(); // For IDistributedCache

// --- Custom Dependencies ---
builder.Services.AddSingleton<DBContext>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<PasswordHelper>();

// --- Repository and Helper Injections ---
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmailHelper, EmailHelper>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoleCreationRepository, RoleCreationRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IUserCreationRepository, UserCreationRepository>();
builder.Services.AddScoped<IModuleSetupRepository, ModuleSetupRepository>();
builder.Services.AddScoped<IActionCreationRepository, ActionCreationRepository>();
builder.Services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
builder.Services.AddScoped<IApplicationsRepository, ApplicationsRepository>();

// ======================================================
// 🧩 API VERSIONING
// ======================================================
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ======================================================
// 🧩 SWAGGER CONFIGURATION
// ======================================================
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token (Example: 'Bearer eyJhbGciOi...')",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ======================================================
// 🧩 CORS CONFIGURATION
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
// ✅ Add this before other middlewares that use IP or authentication
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ======================================================
// 🧩 MIDDLEWARE PIPELINE
// ======================================================
if (app.Environment.IsDevelopment())
{
    // ✅ Serve Swagger UI only in development (optional)
    var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Add Swagger endpoints for each API version
        foreach (var description in apiVersionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"CoreOne API {description.GroupName.ToUpperInvariant()}"
            );
        }

        options.RoutePrefix = string.Empty; // Swagger UI served at root "/"
        options.DocumentTitle = "CoreOne API Documentation";
        options.DisplayRequestDuration();
    });
}

// Enable middleware
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Custom middlewares (make sure ErrorLoggingMiddleware runs first)
app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseMiddleware<ActivityLoggingMiddleware>();

// ======================================================
// 🧩 ROUTING
// ======================================================
app.MapControllers();

// Redirect root to Swagger if UI not already at root
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();
