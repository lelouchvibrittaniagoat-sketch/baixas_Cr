using System;
using API_CONTAS_A_RECEBER_BAIXAS.Models_context;
using API_CONTAS_A_RECEBER_BAIXAS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:5129") // URL do seu frontend
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<ContasAReceberDbContext>(options =>
    options.UseNpgsql("Host=192.168.0.250;Database=DB-CONTAS A RECEBER;Username=postgres;Password=postgres"));
var app = builder.Build();
app.UseCors("CorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // You can configure SwaggerUI options here if needed
}


app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
