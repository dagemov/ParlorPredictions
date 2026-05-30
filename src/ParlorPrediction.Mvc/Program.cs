using System.Net;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.DependencyInjection;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Infrastructure.DependencyInjection;
using ParlorPrediction.Persistence.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Validation error." : error.ErrorMessage)
            .ToArray();

        return new BadRequestObjectResult(
            ApiResponse<object>.Failure(
                "Validation failed.",
                HttpStatusCode.BadRequest,
                errors));
    };
});

builder.Services.AddApplicationLayer();
builder.Services.AddUserLayer();
builder.Services.AddFileLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddMailLayer();
builder.Services.AddPersistenceLayer(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitializeAuthBootstrapAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
