using Microsoft.EntityFrameworkCore;
using StockSystemBack.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
    options.AddPolicy("AllowNextJS",
        policy => policy.WithOrigins("http://localhost:3000") // URL Next.js
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
var app = builder.Build();

app.UseCors("AllowNextJS"); // CORS Policy
app.UseAuthorization();
app.MapControllers();
app.Run();