namespace Hrevolve.Application.Behaviors;

/// <summary>
/// 日志管道行为 - 记录请求处理日志
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserAccessor currentUserAccessor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUserAccessor.CurrentUser?.Id;
        var tenantId = currentUserAccessor.CurrentUser?.TenantId;
        
        logger.LogInformation(
            "处理请求 {RequestName} - 用户: {UserId}, 租户: {TenantId}",
            requestName, userId, tenantId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            logger.LogInformation(
                "请求 {RequestName} 处理完成 - 耗时: {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogError(ex,
                "请求 {RequestName} 处理失败 - 耗时: {ElapsedMilliseconds}ms, 错误: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            
            throw;
        }
    }
}
