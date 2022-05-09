using System.Reflection;
using System.IO;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using TanJi;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureLogging(log =>
{
    log.ClearProviders();
    log.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
}).UseNLog();

// Add services to the container.

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    // 停用自動400回應
    options.SuppressModelStateInvalidFilter = true;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Jwt Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
            new string[] { }
        }
    });
    // Set the comments path for the Swagger JSON and UI.
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseFileServer(); //啟用 wwwroot 靜態檔案功能

app.UseRouting();

app.UseCors();

// app.UseAuthentication();

app.UseAuthorization();

app.UseSwagger();

// API測試網址:https://localhost:5011/swagger/index.html
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "TanJi 中台API");
});

//app.UseMiddleware<LoggerMiddleware>();

app.MapControllers();

var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
logger.Info("init main");
app.Run();
