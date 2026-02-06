using System.Security.Claims;
using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Models.Text2Sql;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// 查询权限验证器实现
/// </summary>
public class QueryPermissionValidator : IQueryPermissionValidator
{
    private readonly Text2SqlOptions _options;
    private readonly ISchemaProvider _schemaProvider;

    // 权限常量
    private const string SystemAdmin = "system:admin";
    private const string HrAdmin = "hr:admin";
    private const string HrRead = "hr:read";
    private const string HrWrite = "hr:write";
    private const string PayrollRead = "payroll:read";
    private const string DepartmentManager = "department:manager";

    public QueryPermissionValidator(IOptions<Text2SqlOptions> options, ISchemaProvider schemaProvider)
    {
        _options = options.Value;
        _schemaProvider = schemaProvider;
    }

    public async Task<PermissionValidationResult> ValidateAsync(QueryRequest request, ClaimsPrincipal user)
    {
        await Task.CompletedTask; // 异步接口，当前实现是同步的

        var result = new PermissionValidationResult { IsValid = true };

        // 1. 检查用户是否已认证
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return PermissionValidationResult.Fail(
                ValidationErrorCodes.InsufficientPermission,
                "用户未认证，请先登录");
        }

        // 2. 检查实体访问权限
        if (!CanAccessEntity(user, request.TargetEntity))
        {
            return PermissionValidationResult.Fail(
                ValidationErrorCodes.InsufficientPermission,
                $"您没有权限访问 '{GetEntityDisplayName(request.TargetEntity)}' 数据");
        }

        // 3. 检查 JOIN 实体的访问权限
        foreach (var join in request.Joins)
        {
            if (!CanAccessEntity(user, join.Entity))
            {
                return PermissionValidationResult.Fail(
                    ValidationErrorCodes.InsufficientPermission,
                    $"您没有权限访问 '{GetEntityDisplayName(join.Entity)}' 数据");
            }
        }

        // 4. 检查操作权限
        if (!CanPerformOperation(user, request.Operation))
        {
            var operationName = request.Operation switch
            {
                QueryOperation.Insert => "新增",
                QueryOperation.Update => "修改",
                QueryOperation.Delete => "删除",
                _ => "执行此"
            };
            return PermissionValidationResult.Fail(
                ValidationErrorCodes.InsufficientPermission,
                $"您没有权限{operationName}数据");
        }

        // 5. 过滤敏感字段
        var accessibleFields = GetAccessibleFields(user, request.TargetEntity);
        var removedFields = new List<string>();

        if (request.SelectFields.Count > 0)
        {
            var filteredFields = new List<string>();
            foreach (var field in request.SelectFields)
            {
                // 处理导航属性
                var parts = field.Split('.');
                var entityName = parts.Length > 1 ? parts[0] : request.TargetEntity;
                var fieldName = parts.Length > 1 ? parts[1] : field;

                var entityAccessibleFields = GetAccessibleFields(user, entityName);
                if (entityAccessibleFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                {
                    filteredFields.Add(field);
                }
                else
                {
                    removedFields.Add(field);
                }
            }
            request.SelectFields = filteredFields;
        }

        if (removedFields.Count > 0)
        {
            result.RemovedFields = removedFields;
            result.Warnings.Add($"以下敏感字段已被移除: {string.Join(", ", removedFields)}");
        }

        // 6. 添加数据范围过滤条件
        var scopeFilters = GetDataScopeFilters(user, request.TargetEntity);
        if (scopeFilters.Count > 0)
        {
            result.RequiredFilters = scopeFilters;
            
            // 将范围过滤条件添加到请求中
            foreach (var filter in scopeFilters)
            {
                if (!request.Filters.Any(f => f.Field == filter.Field && f.Operator == filter.Operator))
                {
                    request.Filters.Add(filter);
                }
            }
        }

        return result;
    }

    public bool CanAccessEntity(ClaimsPrincipal user, string entityName)
    {
        // 系统管理员和 HR 管理员可以访问所有实体
        if (HasPermission(user, SystemAdmin) || HasPermission(user, HrAdmin))
        {
            return true;
        }

        // 普通用户不能访问薪资记录
        if (entityName.Equals("PayrollRecord", StringComparison.OrdinalIgnoreCase))
        {
            return HasPermission(user, PayrollRead);
        }

        // 其他实体需要 HR 读取权限
        return HasPermission(user, HrRead);
    }

    public bool CanAccessField(ClaimsPrincipal user, string entityName, string fieldName)
    {
        // 系统管理员和 HR 管理员可以访问所有字段
        if (HasPermission(user, SystemAdmin) || HasPermission(user, HrAdmin))
        {
            return true;
        }

        // 检查敏感字段配置
        if (_options.SensitiveFields.TryGetValue(entityName, out var sensitiveFields))
        {
            // "*" 表示所有字段都是敏感的
            if (sensitiveFields.Contains("*"))
            {
                // 薪资数据需要 payroll:read 权限
                if (entityName.Equals("PayrollRecord", StringComparison.OrdinalIgnoreCase))
                {
                    return HasPermission(user, PayrollRead);
                }
                return false;
            }

            // 检查具体的敏感字段
            if (sensitiveFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                var fieldSchema = _schemaProvider.GetFieldSchema(entityName, fieldName);
                if (fieldSchema?.RequiredPermission != null)
                {
                    return HasPermission(user, fieldSchema.RequiredPermission);
                }
                return false;
            }
        }

        return true;
    }

    public bool CanPerformOperation(ClaimsPrincipal user, QueryOperation operation)
    {
        // SELECT 操作只需要基本的读取权限
        if (operation == QueryOperation.Select)
        {
            return HasPermission(user, HrRead) || HasPermission(user, SystemAdmin) || HasPermission(user, HrAdmin);
        }

        // CRUD 操作检查
        if (!_options.EnableCrud)
        {
            return false;
        }

        // 系统管理员可以执行所有操作
        if (HasPermission(user, SystemAdmin))
        {
            return true;
        }

        // 检查具体操作权限
        var operationKey = operation.ToString();
        if (_options.CrudPermissions.TryGetValue(operationKey, out var requiredPermission))
        {
            return HasPermission(user, requiredPermission);
        }

        // 默认需要 hr:write 权限
        return HasPermission(user, HrWrite);
    }

    public List<string> GetAccessibleFields(ClaimsPrincipal user, string entityName)
    {
        var entitySchema = _schemaProvider.GetEntitySchema(entityName);
        if (entitySchema == null)
        {
            return [];
        }

        var accessibleFields = new List<string>();

        foreach (var field in entitySchema.Fields)
        {
            if (CanAccessField(user, entityName, field.Name))
            {
                accessibleFields.Add(field.Name);
            }
        }

        return accessibleFields;
    }

    public List<FilterCondition> GetDataScopeFilters(ClaimsPrincipal user, string entityName)
    {
        var filters = new List<FilterCondition>();

        // 系统管理员和 HR 管理员可以访问全部数据
        if (HasPermission(user, SystemAdmin) || HasPermission(user, HrAdmin))
        {
            return filters;
        }

        // 获取当前用户的员工 ID
        var employeeId = GetEmployeeId(user);

        // 部门经理可以查看本部门数据
        if (HasPermission(user, DepartmentManager))
        {
            var departmentId = GetDepartmentId(user);
            if (departmentId.HasValue)
            {
                // 对于员工相关查询，添加部门过滤
                if (entityName.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    // 注意：这里假设 Employee 有 DepartmentId 字段
                    // 实际实现可能需要通过 JobHistory 或其他方式关联
                    filters.Add(new FilterCondition
                    {
                        Field = "OrganizationUnitId",
                        Operator = FilterOperator.Equal,
                        Value = departmentId.Value
                    });
                }
                else if (entityName.Equals("AttendanceRecord", StringComparison.OrdinalIgnoreCase) ||
                         entityName.Equals("LeaveRequest", StringComparison.OrdinalIgnoreCase) ||
                         entityName.Equals("LeaveBalance", StringComparison.OrdinalIgnoreCase))
                {
                    // 对于这些实体，需要通过员工关联过滤
                    // 这里简化处理，实际可能需要更复杂的子查询
                }
            }
            return filters;
        }

        // 普通员工只能查看自己的数据
        if (employeeId.HasValue)
        {
            var employeeIdFilterField = entityName switch
            {
                "Employee" => "Id",
                "AttendanceRecord" => "EmployeeId",
                "LeaveRequest" => "EmployeeId",
                "LeaveBalance" => "EmployeeId",
                "PayrollRecord" => "EmployeeId",
                _ => null
            };

            if (employeeIdFilterField != null)
            {
                filters.Add(new FilterCondition
                {
                    Field = employeeIdFilterField,
                    Operator = FilterOperator.Equal,
                    Value = employeeId.Value
                });
            }
        }

        return filters;
    }

    private bool HasPermission(ClaimsPrincipal user, string permission)
    {
        // 检查 permission claim
        var permissions = user.Claims
            .Where(c => c.Type == "permission" || c.Type == "permissions")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(p => p.Trim());

        if (permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // 检查角色（某些角色隐含权限）
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value);

        // 系统管理员角色
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
            roles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // HR 管理员角色
        if ((permission == HrAdmin || permission == HrRead || permission == HrWrite) &&
            roles.Contains("HrAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private Guid? GetEmployeeId(ClaimsPrincipal user)
    {
        var employeeIdClaim = user.Claims.FirstOrDefault(c => 
            c.Type == "employee_id" || c.Type == "employeeId" || c.Type == "EmployeeId");

        if (employeeIdClaim != null && Guid.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return employeeId;
        }

        return null;
    }

    private Guid? GetDepartmentId(ClaimsPrincipal user)
    {
        var departmentIdClaim = user.Claims.FirstOrDefault(c =>
            c.Type == "department_id" || c.Type == "departmentId" || c.Type == "DepartmentId");

        if (departmentIdClaim != null && Guid.TryParse(departmentIdClaim.Value, out var departmentId))
        {
            return departmentId;
        }

        return null;
    }

    private string GetEntityDisplayName(string entityName)
    {
        var schema = _schemaProvider.GetEntitySchema(entityName);
        return schema?.DisplayName ?? entityName;
    }
}
