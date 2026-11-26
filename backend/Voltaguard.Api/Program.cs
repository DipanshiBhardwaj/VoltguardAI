using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voltaguard.Api.Data;
using Voltaguard.Api.Extensions;
using Voltaguard.Api.Middleware;
using Voltaguard.BLL;
using Voltaguard.DAL;
using Voltaguard.DAL.Entities;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<VoltaguardDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<UserAccount, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<VoltaguardDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFrontendCors();
builder.Services.AddBusinessServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoltaguardDbContext>();
    await db.Database.MigrateAsync();
}
await DatabaseSeeder.SeedAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
