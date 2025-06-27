using DT_HR.Api.Helpers;
using DT_HR.Application;
using DT_HR.Persistence;
using DT_HR.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDtHrLocalization();


builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices(builder.Configuration);


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    options.AddPolicy("TelegramWebhook", policy =>
    {
        policy.WithOrigins("https://api.telegram.org")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();


app.UseCors();
app.UseRequestLocalization();


app.Use(async (context, next) =>
{

    if (context.Request.Headers.ContainsKey("ngrok-skip-browser-warning"))
    {
        context.Response.Headers.Add("ngrok-skip-browser-warning", "true");
    }
    

    if (context.Request.Path.StartsWithSegments("/api/TelegramWebhook"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Telegram webhook request: {Method} {Path} from {RemoteIp}", 
            context.Request.Method, 
            context.Request.Path, 
            context.Connection.RemoteIpAddress);
    }
    
    await next();
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();


app.MapControllers();

app.Map("/error", (HttpContext context) =>
{
    return Results.Problem(
        title: "An error occurred",
        statusCode: StatusCodes.Status500InternalServerError);
});



app.Run();