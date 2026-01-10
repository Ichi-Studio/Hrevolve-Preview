var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel 启用 HTTP/3
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP 端口（开发环境）
    options.ListenLocalhost(5224, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    // HTTPS 端口，支持 HTTP/1.1、HTTP/2 和 HTTP/3
    options.ListenLocalhost(5225, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });

});


// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Hrevolve")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 添加 OpenAPI
builder.Services.AddOpenApi();


// JWT认证
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 添加应用层和基础设施层服务
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 添加AI Agent服务（基于Microsoft Agent Framework）
builder.Services.AddAgentServices(builder.Configuration);

// 添加HttpContextAccessor
builder.Services.AddHttpContextAccessor();







var app = builder.Build();

// 数据库初始化和种子数据
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<Hrevolve.Infrastructure.Persistence.DbInitializer>();
    await dbInitializer.InitializeAsync();
    await dbInitializer.SeedAsync();
}

// 配置中间件管道
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// CORS必须在其他中间件之前
app.UseCors("AllowAll");

// 添加 Alt-Svc 头，通知客户端支持 HTTP/3
app.Use(async (context, next) =>
{
    context.Response.Headers.AltSvc = "h3=\":5225\"; ma=86400";
    await next();
});

// 全局异常处理
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// 多租户中间件（必须在认证之后，这样才能从JWT获取租户ID）
app.UseMiddleware<TenantMiddleware>();

// 当前用户中间件
app.UseMiddleware<CurrentUserMiddleware>();

app.MapControllers();

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

try
{

    Log.Information("启动 Hrevolve HRM 应用...");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
}
finally
{
    await Log.CloseAndFlushAsync();
}
