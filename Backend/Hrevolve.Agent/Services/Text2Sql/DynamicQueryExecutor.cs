using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Models.Text2Sql;
using Hrevolve.Domain.Attendance;
using Hrevolve.Domain.Employees;
using Hrevolve.Domain.Leave;
using Hrevolve.Domain.Organizations;
using Hrevolve.Domain.Payroll;
using Hrevolve.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// 动态查询执行器 - 使用 Expression Tree 安全地执行动态 LINQ 查询
/// </summary>
public class DynamicQueryExecutor : IQueryExecutor
{
    private readonly HrevolveDbContext _dbContext;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IQueryPermissionValidator _permissionValidator;
    private readonly ISqlSecurityValidator _securityValidator;
    private readonly Text2SqlOptions _options;
    private readonly ILogger<DynamicQueryExecutor> _logger;

    // 实体类型映射
    private static readonly Dictionary<string, Type> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Employee"] = typeof(Employee),
        ["AttendanceRecord"] = typeof(AttendanceRecord),
        ["LeaveRequest"] = typeof(LeaveRequest),
        ["LeaveBalance"] = typeof(LeaveBalance),
        ["LeaveType"] = typeof(LeaveType),
        ["PayrollRecord"] = typeof(PayrollRecord),
        ["OrganizationUnit"] = typeof(OrganizationUnit),
        ["Position"] = typeof(Position),
        ["Shift"] = typeof(Shift),
        ["Schedule"] = typeof(Schedule)
    };

    public DynamicQueryExecutor(
        HrevolveDbContext dbContext,
        ISchemaProvider schemaProvider,
        IQueryPermissionValidator permissionValidator,
        ISqlSecurityValidator securityValidator,
        IOptions<Text2SqlOptions> options,
        ILogger<DynamicQueryExecutor> logger)
    {
        _dbContext = dbContext;
        _schemaProvider = schemaProvider;
        _permissionValidator = permissionValidator;
        _securityValidator = securityValidator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteAsync(QueryRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. 安全验证
            var securityValidation = _securityValidator.Validate(request);
            if (!securityValidation.IsValid)
            {
                return QueryResult.Fail(string.Join("; ", securityValidation.Errors.Select(e => e.Message)));
            }

            // 2. 权限验证
            var permissionValidation = await _permissionValidator.ValidateAsync(request, user);
            if (!permissionValidation.IsValid)
            {
                return QueryResult.Fail(string.Join("; ", permissionValidation.Errors.Select(e => e.Message)));
            }

            // 3. 根据操作类型执行
            QueryResult result = request.Operation switch
            {
                QueryOperation.Select => await ExecuteSelectAsync(request, cancellationToken),
                QueryOperation.Insert => await ExecuteInsertAsync(request, cancellationToken),
                QueryOperation.Update => await ExecuteUpdateAsync(request, cancellationToken),
                QueryOperation.Delete => await ExecuteDeleteAsync(request, cancellationToken),
                _ => QueryResult.Fail($"不支持的操作类型: {request.Operation}")
            };

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行查询失败: {Entity}", request.TargetEntity);
            return QueryResult.Fail("查询执行过程中发生错误，请稍后重试");
        }
    }

    private async Task<QueryResult> ExecuteSelectAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        if (!EntityTypeMap.TryGetValue(request.TargetEntity, out var entityType))
        {
            return QueryResult.Fail($"未知的实体类型: {request.TargetEntity}");
        }

        // 使用泛型方法执行查询
        var method = typeof(DynamicQueryExecutor)
            .GetMethod(nameof(ExecuteSelectAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        var task = (Task<QueryResult>)method.Invoke(this, [request, cancellationToken])!;
        return await task;
    }

    private async Task<QueryResult> ExecuteSelectAsyncGeneric<TEntity>(QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class
    {
        var dbSet = _dbContext.Set<TEntity>();
        IQueryable<TEntity> query = dbSet.AsNoTracking();

        // 应用过滤条件
        query = ApplyFilters(query, request.Filters);

        // 聚合查询
        if (request.Aggregation.HasValue)
        {
            return await ExecuteAggregationAsync(query, request, cancellationToken);
        }

        // 应用排序
        query = ApplyOrderBy(query, request.OrderBy);

        // 应用分页
        if (request.Offset > 0)
        {
            query = query.Skip(request.Offset);
        }
        query = query.Take(Math.Min(request.Limit, _options.MaxResultRows));

        string? generatedSql = null;
        try
        {
            generatedSql = query.ToQueryString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "生成 SQL 失败（Select）");
        }

        // 执行查询
        var entities = await query.ToListAsync(cancellationToken);

        // 转换为字典列表
        var data = ConvertToDataList(entities, request.SelectFields, typeof(TEntity));

        // 获取列信息
        var columns = GetColumnInfos(request.SelectFields, request.TargetEntity);

        var result = QueryResult.Ok(data, data.Count, columns);
        result.GeneratedSql = generatedSql;
        return result;
    }

    private async Task<QueryResult> ExecuteAggregationAsync<TEntity>(IQueryable<TEntity> query, QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class
    {
        try
        {
            string? generatedSql = null;
            try
            {
                generatedSql = TryGenerateAggregationSql(query, request);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "生成 SQL 失败（Aggregation）");
            }

            object? result = null;

            switch (request.Aggregation)
            {
                case AggregationType.Count:
                    result = await query.CountAsync(cancellationToken);
                    break;

                case AggregationType.CountDistinct when !string.IsNullOrEmpty(request.AggregationField):
                    var distinctSelector = BuildPropertySelector<TEntity, object>(request.AggregationField);
                    if (distinctSelector != null)
                    {
                        result = await query.Select(distinctSelector).Distinct().CountAsync(cancellationToken);
                    }
                    break;

                case AggregationType.Sum when !string.IsNullOrEmpty(request.AggregationField):
                    result = await ExecuteNumericAggregation(query, request.AggregationField, "Sum", cancellationToken);
                    break;

                case AggregationType.Avg when !string.IsNullOrEmpty(request.AggregationField):
                    result = await ExecuteNumericAggregation(query, request.AggregationField, "Average", cancellationToken);
                    break;

                case AggregationType.Min when !string.IsNullOrEmpty(request.AggregationField):
                    result = await ExecuteMinMaxAggregation(query, request.AggregationField, "Min", cancellationToken);
                    break;

                case AggregationType.Max when !string.IsNullOrEmpty(request.AggregationField):
                    result = await ExecuteMinMaxAggregation(query, request.AggregationField, "Max", cancellationToken);
                    break;
            }

            var ok = QueryResult.OkAggregation(result);
            ok.GeneratedSql = generatedSql;
            if (request.Aggregation.HasValue && request.Aggregation != AggregationType.Count && !string.IsNullOrWhiteSpace(generatedSql))
            {
                ok.Warnings.Add("generated-sql-approx");
            }
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚合查询失败");
            return QueryResult.Fail("聚合查询失败，请检查字段类型是否支持该操作");
        }
    }

    private string? TryGenerateAggregationSql<TEntity>(IQueryable<TEntity> query, QueryRequest request)
        where TEntity : class
    {
        if (!request.Aggregation.HasValue)
        {
            return null;
        }

        if (request.Aggregation == AggregationType.Count)
        {
            var aggQuery = query.GroupBy(_ => 1).Select(g => g.Count());
            return aggQuery.ToQueryString();
        }

        return query.ToQueryString();
    }

    private async Task<object?> ExecuteNumericAggregation<TEntity>(IQueryable<TEntity> query, string fieldName, string operation, CancellationToken cancellationToken)
        where TEntity : class
    {
        var property = typeof(TEntity).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null) return null;

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        // 根据属性类型选择合适的聚合方法
        if (propertyType == typeof(decimal))
        {
            var selector = BuildPropertySelector<TEntity, decimal?>(fieldName);
            if (selector == null) return null;
            return operation == "Sum"
                ? await query.SumAsync(selector, cancellationToken)
                : await query.AverageAsync(selector, cancellationToken);
        }
        if (propertyType == typeof(double))
        {
            var selector = BuildPropertySelector<TEntity, double?>(fieldName);
            if (selector == null) return null;
            return operation == "Sum"
                ? await query.SumAsync(selector, cancellationToken)
                : await query.AverageAsync(selector, cancellationToken);
        }
        if (propertyType == typeof(float))
        {
            var selector = BuildPropertySelector<TEntity, float?>(fieldName);
            if (selector == null) return null;
            return operation == "Sum"
                ? await query.SumAsync(selector, cancellationToken)
                : await query.AverageAsync(selector, cancellationToken);
        }
        if (propertyType == typeof(int))
        {
            var selector = BuildPropertySelector<TEntity, int?>(fieldName);
            if (selector == null) return null;
            return operation == "Sum"
                ? await query.SumAsync(selector, cancellationToken)
                : await query.AverageAsync(selector, cancellationToken);
        }
        if (propertyType == typeof(long))
        {
            var selector = BuildPropertySelector<TEntity, long?>(fieldName);
            if (selector == null) return null;
            return operation == "Sum"
                ? await query.SumAsync(selector, cancellationToken)
                : await query.AverageAsync(selector, cancellationToken);
        }

        return null;
    }

    private async Task<object?> ExecuteMinMaxAggregation<TEntity>(IQueryable<TEntity> query, string fieldName, string operation, CancellationToken cancellationToken)
        where TEntity : class
    {
        var selector = BuildPropertySelector<TEntity, object>(fieldName);
        if (selector == null) return null;

        return operation == "Min"
            ? await query.MinAsync(selector, cancellationToken)
            : await query.MaxAsync(selector, cancellationToken);
    }

    private async Task<QueryResult> ExecuteInsertAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        if (!EntityTypeMap.TryGetValue(request.TargetEntity, out var entityType))
        {
            return QueryResult.Fail($"未知的实体类型: {request.TargetEntity}");
        }

        // 创建新实体
        var entity = Activator.CreateInstance(entityType);
        if (entity == null)
        {
            return QueryResult.Fail("无法创建实体实例");
        }

        // 设置属性值
        foreach (var (fieldName, value) in request.UpdateValues)
        {
            var property = entityType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanWrite)
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(entity, convertedValue);
            }
        }

        // 添加到 DbContext
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 获取新创建的 ID
        var idProperty = entityType.GetProperty("Id");
        var newId = idProperty?.GetValue(entity) as Guid?;

        return QueryResult.OkModified(QueryOperation.Insert, 1, newId);
    }

    private async Task<QueryResult> ExecuteUpdateAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        if (!EntityTypeMap.TryGetValue(request.TargetEntity, out var entityType))
        {
            return QueryResult.Fail($"未知的实体类型: {request.TargetEntity}");
        }

        if (request.Filters.Count == 0)
        {
            return QueryResult.Fail("更新操作必须指定过滤条件");
        }

        // 使用泛型方法执行更新
        var method = typeof(DynamicQueryExecutor)
            .GetMethod(nameof(ExecuteUpdateAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        var task = (Task<QueryResult>)method.Invoke(this, [request, cancellationToken])!;
        return await task;
    }

    private async Task<QueryResult> ExecuteUpdateAsyncGeneric<TEntity>(QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class
    {
        var dbSet = _dbContext.Set<TEntity>();
        IQueryable<TEntity> query = dbSet;

        query = ApplyFilters(query, request.Filters);
        var entities = await query.ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return QueryResult.OkModified(QueryOperation.Update, 0);
        }

        var entityType = typeof(TEntity);

        // 更新实体
        foreach (var entity in entities)
        {
            foreach (var (fieldName, value) in request.UpdateValues)
            {
                var property = entityType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    property.SetValue(entity, convertedValue);
                }
            }
            _dbContext.Update(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return QueryResult.OkModified(QueryOperation.Update, entities.Count);
    }

    private async Task<QueryResult> ExecuteDeleteAsync(QueryRequest request, CancellationToken cancellationToken)
    {
        if (!EntityTypeMap.TryGetValue(request.TargetEntity, out var entityType))
        {
            return QueryResult.Fail($"未知的实体类型: {request.TargetEntity}");
        }

        if (request.Filters.Count == 0)
        {
            return QueryResult.Fail("删除操作必须指定过滤条件");
        }

        // 使用泛型方法执行删除
        var method = typeof(DynamicQueryExecutor)
            .GetMethod(nameof(ExecuteDeleteAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(entityType);

        var task = (Task<QueryResult>)method.Invoke(this, [request, cancellationToken])!;
        return await task;
    }

    private async Task<QueryResult> ExecuteDeleteAsyncGeneric<TEntity>(QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class
    {
        var dbSet = _dbContext.Set<TEntity>();
        IQueryable<TEntity> query = dbSet;

        query = ApplyFilters(query, request.Filters);
        var entities = await query.ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return QueryResult.OkModified(QueryOperation.Delete, 0);
        }

        // 删除实体（由 DbContext 处理软删除）
        foreach (var entity in entities)
        {
            _dbContext.Remove(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return QueryResult.OkModified(QueryOperation.Delete, entities.Count);
    }

    #region Expression Tree Builders

    private static IQueryable<TEntity> ApplyFilters<TEntity>(IQueryable<TEntity> query, List<FilterCondition> filters)
        where TEntity : class
    {
        if (filters.Count == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            var filterExpression = BuildFilterExpression<TEntity>(parameter, filter);
            if (filterExpression == null) continue;

            if (combinedExpression == null)
            {
                combinedExpression = filterExpression;
            }
            else
            {
                combinedExpression = filter.LogicalOperator?.Equals("OR", StringComparison.OrdinalIgnoreCase) == true
                    ? Expression.OrElse(combinedExpression, filterExpression)
                    : Expression.AndAlso(combinedExpression, filterExpression);
            }
        }

        if (combinedExpression == null)
        {
            return query;
        }

        var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    private static Expression? BuildFilterExpression<TEntity>(ParameterExpression parameter, FilterCondition filter)
    {
        // 解析字段路径（支持导航属性，如 "Department.Name"）
        var propertyPath = filter.Field.Split('.');
        Expression propertyAccess = parameter;

        foreach (var propertyName in propertyPath)
        {
            var property = propertyAccess.Type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null) return null;
            propertyAccess = Expression.Property(propertyAccess, property);
        }

        var propertyType = propertyAccess.Type;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return filter.Operator switch
        {
            FilterOperator.Equal => BuildEqualExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.NotEqual => BuildNotEqualExpression(propertyAccess, filter.Value, propertyType),
            FilterOperator.GreaterThan => BuildComparisonExpression(propertyAccess, filter.Value, propertyType, Expression.GreaterThan),
            FilterOperator.GreaterThanOrEqual => BuildComparisonExpression(propertyAccess, filter.Value, propertyType, Expression.GreaterThanOrEqual),
            FilterOperator.LessThan => BuildComparisonExpression(propertyAccess, filter.Value, propertyType, Expression.LessThan),
            FilterOperator.LessThanOrEqual => BuildComparisonExpression(propertyAccess, filter.Value, propertyType, Expression.LessThanOrEqual),
            FilterOperator.Contains => BuildStringMethodExpression(propertyAccess, filter.Value, "Contains"),
            FilterOperator.StartsWith => BuildStringMethodExpression(propertyAccess, filter.Value, "StartsWith"),
            FilterOperator.EndsWith => BuildStringMethodExpression(propertyAccess, filter.Value, "EndsWith"),
            FilterOperator.IsNull => Expression.Equal(propertyAccess, Expression.Constant(null, propertyType)),
            FilterOperator.IsNotNull => Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType)),
            FilterOperator.In => BuildInExpression(propertyAccess, filter.Value, propertyType),
            _ => null
        };
    }

    private static Expression BuildEqualExpression(Expression property, object? value, Type propertyType)
    {
        if (value == null)
        {
            return Expression.Equal(property, Expression.Constant(null, propertyType));
        }

        var convertedValue = ConvertFilterValue(value, propertyType);
        return Expression.Equal(property, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression BuildNotEqualExpression(Expression property, object? value, Type propertyType)
    {
        if (value == null)
        {
            return Expression.NotEqual(property, Expression.Constant(null, propertyType));
        }

        var convertedValue = ConvertFilterValue(value, propertyType);
        return Expression.NotEqual(property, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression? BuildComparisonExpression(Expression property, object? value, Type propertyType,
        Func<Expression, Expression, BinaryExpression> comparison)
    {
        if (value == null) return null;

        var convertedValue = ConvertFilterValue(value, propertyType);
        return comparison(property, Expression.Constant(convertedValue, propertyType));
    }

    private static Expression? BuildStringMethodExpression(Expression property, object? value, string methodName)
    {
        if (value == null || property.Type != typeof(string)) return null;

        var method = typeof(string).GetMethod(methodName, [typeof(string)]);
        if (method == null) return null;

        var valueExpression = Expression.Constant(value.ToString(), typeof(string));
        return Expression.Call(property, method, valueExpression);
    }

    private static Expression? BuildInExpression(Expression property, object? value, Type propertyType)
    {
        if (value is not IEnumerable enumerable || value is string) return null;

        var values = enumerable.Cast<object>().ToList();
        if (values.Count == 0)
        {
            return Expression.Constant(false); // 空列表，始终为假
        }

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var listType = typeof(List<>).MakeGenericType(underlyingType);
        var convertedList = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add")!;

        foreach (var item in values)
        {
            var convertedItem = ConvertFilterValue(item, underlyingType);
            addMethod.Invoke(convertedList, [convertedItem]);
        }

        var containsMethod = listType.GetMethod("Contains", [underlyingType])!;
        var listExpression = Expression.Constant(convertedList);

        // 处理可空类型
        var propertyForContains = property;
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            propertyForContains = Expression.Property(property, "Value");
        }

        return Expression.Call(listExpression, containsMethod, propertyForContains);
    }

    private static IQueryable<TEntity> ApplyOrderBy<TEntity>(IQueryable<TEntity> query, List<OrderByClause> orderBy)
        where TEntity : class
    {
        if (orderBy.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;

        foreach (var clause in orderBy)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var property = typeof(TEntity).GetProperty(clause.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null) continue;

            var propertyAccess = Expression.Property(parameter, property);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            var methodName = orderedQuery == null
                ? (clause.Descending ? "OrderByDescending" : "OrderBy")
                : (clause.Descending ? "ThenByDescending" : "ThenBy");

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), property.PropertyType);

            orderedQuery = (IOrderedQueryable<TEntity>)method.Invoke(null, [orderedQuery ?? query, lambda])!;
        }

        return orderedQuery ?? query;
    }

    private static Expression<Func<TEntity, TResult>>? BuildPropertySelector<TEntity, TResult>(string fieldName)
    {
        var property = typeof(TEntity).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null) return null;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var propertyAccess = Expression.Property(parameter, property);

        // 转换为目标类型
        Expression body = propertyAccess;
        if (property.PropertyType != typeof(TResult))
        {
            body = Expression.Convert(propertyAccess, typeof(TResult));
        }

        return Expression.Lambda<Func<TEntity, TResult>>(body, parameter);
    }

    #endregion

    #region Helper Methods

    private List<Dictionary<string, object?>> ConvertToDataList<TEntity>(List<TEntity> entities, List<string> selectFields, Type entityType)
    {
        var result = new List<Dictionary<string, object?>>();

        var properties = selectFields.Count > 0
            ? selectFields.Select(f => entityType.GetProperty(f.Split('.').Last(),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase))
                .Where(p => p != null)
                .Cast<PropertyInfo>()
                .ToList()
            : entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (var entity in entities)
        {
            var row = new Dictionary<string, object?>();
            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                row[property.Name] = value;
            }
            result.Add(row);
        }

        return result;
    }

    private List<ColumnInfo> GetColumnInfos(List<string> selectFields, string entityName)
    {
        var schema = _schemaProvider.GetEntitySchema(entityName);
        if (schema == null)
        {
            return [];
        }

        var fields = selectFields.Count > 0
            ? selectFields.Select(f => f.Split('.').Last())
            : schema.Fields.Where(f => !f.IsSensitive).Select(f => f.Name);

        return fields
            .Select(fieldName => schema.Fields.FirstOrDefault(f =>
                f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
            .Where(f => f != null)
            .Select(f => new ColumnInfo
            {
                Name = f!.Name,
                DisplayName = f.DisplayName,
                DataType = f.DataType,
                IsNullable = f.IsNullable
            })
            .ToList();
    }

    private static object? ConvertFilterValue(object? value, Type targetType)
    {
        if (value == null) return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return ConvertJsonElement(jsonElement, underlyingType);
        }

        if (value.GetType() == underlyingType)
        {
            return value;
        }

        if (underlyingType == typeof(Guid) && value is string guidString)
        {
            return Guid.Parse(guidString);
        }

        if (underlyingType == typeof(DateOnly) && value is string dateString)
        {
            return DateOnly.Parse(dateString);
        }

        if (underlyingType == typeof(DateTime) && value is string dateTimeString)
        {
            return DateTime.Parse(dateTimeString);
        }

        if (underlyingType.IsEnum)
        {
            return value is string enumString
                ? Enum.Parse(underlyingType, enumString, true)
                : Enum.ToObject(underlyingType, value);
        }

        return Convert.ChangeType(value, underlyingType);
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        return ConvertFilterValue(value, targetType);
    }

    private static object? ConvertJsonElement(System.Text.Json.JsonElement element, Type targetType)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => targetType == typeof(Guid)
                ? Guid.Parse(element.GetString()!)
                : targetType == typeof(DateOnly)
                    ? DateOnly.Parse(element.GetString()!)
                    : targetType == typeof(DateTime)
                        ? DateTime.Parse(element.GetString()!)
                        : element.GetString(),
            System.Text.Json.JsonValueKind.Number => Convert.ChangeType(element.GetDecimal(), targetType),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    #endregion
}
