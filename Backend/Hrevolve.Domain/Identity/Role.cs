using Hrevolve.Domain.Common;

namespace Hrevolve.Domain.Identity;

/// <summary>
/// 角色实体 - RBAC核心
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    
    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();
    
    private Role() { }
    
    public static Role Create(Guid tenantId, string name, string code, bool isSystemRole = false)
    {
        return new Role
        {
            TenantId = tenantId,
            Name = name,
            Code = code,
            IsSystemRole = isSystemRole
        };
    }
    
    public void AddPermission(string permissionCode)
    {
        if (!_permissions.Any(p => p.PermissionCode == permissionCode))
        {
            _permissions.Add(new RolePermission(Id, permissionCode));
        }
    }
    
    public void RemovePermission(string permissionCode)
    {
        var permission = _permissions.FirstOrDefault(p => p.PermissionCode == permissionCode);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name不能为空", nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void ReplacePermissions(IEnumerable<string> permissionCodes)
    {
        var target = permissionCodes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _permissions.RemoveAll(p => !target.Contains(p.PermissionCode));
        foreach (var code in target)
        {
            AddPermission(code);
        }
    }
}

/// <summary>
/// 角色权限
/// </summary>
public class RolePermission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; } = null!; // 如: employee:read, payroll:write
    
    private RolePermission() { }
    
    public RolePermission(Guid roleId, string permissionCode)
    {
        RoleId = roleId;
        PermissionCode = permissionCode;
    }
}

/// <summary>
/// 用户角色关联
/// </summary>
public class UserRole
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;
    
    private UserRole() { }
    
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}

/// <summary>
/// 预定义权限常量
/// </summary>
public static class Permissions
{
    // 员工管理
    public const string EmployeeRead = "employee:read";
    public const string EmployeeWrite = "employee:write";
    public const string EmployeeDelete = "employee:delete";
    
    // 组织管理
    public const string OrganizationRead = "organization:read";
    public const string OrganizationWrite = "organization:write";
    
    // 考勤管理
    public const string AttendanceRead = "attendance:read";
    public const string AttendanceWrite = "attendance:write";
    public const string AttendanceApprove = "attendance:approve";
    
    // 假期管理
    public const string LeaveRead = "leave:read";
    public const string LeaveWrite = "leave:write";
    public const string LeaveApprove = "leave:approve";
    
    // 薪酬管理
    public const string PayrollRead = "payroll:read";
    public const string PayrollWrite = "payroll:write";
    public const string PayrollApprove = "payroll:approve";
    
    // 报销管理
    public const string ExpenseRead = "expense:read";
    public const string ExpenseWrite = "expense:write";
    public const string ExpenseApprove = "expense:approve";
    
    // 系统管理
    public const string SystemAdmin = "system:admin";
    public const string TenantAdmin = "tenant:admin";
}
