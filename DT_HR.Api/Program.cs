using DT_HR.Application;
using DT_HR.Persistence;
using DT_HR.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices(builder.Configuration);


// Add CORS before building the app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Add CORS middleware early in the pipeline
app.UseCors();

// Add middleware to handle ngrok and Telegram webhook headers
app.Use(async (context, next) =>
{
    // Skip ngrok browser warning
    if (context.Request.Headers.ContainsKey("ngrok-skip-browser-warning"))
    {
        context.Response.Headers.Add("ngrok-skip-browser-warning", "true");
    }
    
    // Log incoming webhook requests for debugging
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



app.UseAuthorization();

app.MapControllers();

app.Map("/error", (HttpContext context) =>
{
    return Results.Problem(
        title: "An error occurred",
        statusCode: StatusCodes.Status500InternalServerError);
});



app.Run();