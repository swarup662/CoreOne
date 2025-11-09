using CoreOne.API.Helpers;
using CoreOne.API.Infrastructure.Data;
using CoreOne.API.Infrastructure.Services;
using CoreOne.API.Interface;
using CoreOne.API.Interfaces;
using CoreOne.API.Middleware;
using CoreOne.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// --- Add core services ---
builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache(); // ✅ Fixes IDistributedCache
builder.Services.AddSingleton<DBContext>();
builder.Services.AddSingleton<TokenService>();

// --- Dependency Injection ---
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmailHelper, EmailHelper>();

// --- Other repositories ---
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoleCreationRepository, RoleCreationRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IUserCreationRepository, UserCreationRepository>();
builder.Services.AddScoped<IModuleSetupRepository, ModuleSetupRepository>();
builder.Services.AddScoped<IActionCreationRepository, ActionCreationRepository>();
builder.Services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();

// --- API Versioning ---
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

// --- Swagger ---
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token",
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
            new string[]{}
        }
    });
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Middleware pipeline ---
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseMiddleware<ActivityLoggingMiddleware>();

// --- Swagger Setup (only once!) ---
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in apiVersionProvider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
            $"CoreOne API {description.GroupName.ToUpperInvariant()}");
    }
    options.RoutePrefix = string.Empty; // Swagger opens at root "/"
});

// --- Map controllers ---
app.MapControllers();

// --- Default redirect to Swagger ---
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();
