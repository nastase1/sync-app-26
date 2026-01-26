using Microsoft.EntityFrameworkCore;
using SyncApp26.Application.IServices;
using SyncApp26.Application.Services;
using SyncApp26.Domain.IRepositories;
using SyncApp26.Infrastructure.Context;
using SyncApp26.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Configure EF Core context and resolve relative SQLite path against ContentRoot.
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var sqliteBuilder = new SqliteConnectionStringBuilder(configuredConnection);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    var basePath = builder.Environment.ContentRootPath;
    sqliteBuilder.DataSource = Path.GetFullPath(Path.Combine(basePath, sqliteBuilder.DataSource));
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteBuilder.ToString()));

// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowAngularApp");

app.MapControllers();

app.Run();
