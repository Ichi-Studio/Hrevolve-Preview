namespace Hrevolve.Web.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        
        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };
        
        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = validationEx.ErrorCode;
                errorResponse.Message = validationEx.Message;
                errorResponse.Errors = validationEx.Errors;
                break;
                
            case EntityNotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Code = notFoundEx.ErrorCode;
                errorResponse.Message = notFoundEx.Message;
                break;
                
            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Code = unauthorizedEx.ErrorCode;
                errorResponse.Message = unauthorizedEx.Message;
                break;
                
            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Code = forbiddenEx.ErrorCode;
                errorResponse.Message = forbiddenEx.Message;
                break;
                
            case TenantException tenantEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = tenantEx.ErrorCode;
                errorResponse.Message = tenantEx.Message;
                break;
                
            case HrevolveException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Code = businessEx.ErrorCode;
                errorResponse.Message = businessEx.Message;
                break;
                
            default:
                logger.LogError(exception, "未处理的异常: {Message}", exception.Message);
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Code = "INTERNAL_ERROR";
                errorResponse.Message = "服务器内部错误，请稍后重试";
                break;
        }
        
        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await response.WriteAsync(result);
    }
}

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? TraceId { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
