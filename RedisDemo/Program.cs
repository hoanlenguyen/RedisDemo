using Microsoft.EntityFrameworkCore;
using RedisDemo.Data;

var builder = WebApplication.CreateBuilder(args);
//https://www.c-sharpcorner.com/article/easily-use-redis-cache-in-asp-net-6-0-web-api/
var configuration = builder.Configuration;
builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = configuration["RedisCacheUrl"]; });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();