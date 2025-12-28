namespace Hrevolve.Infrastructure.Persistence;

/// <summary>
/// 数据库初始化器 - 创建初始数据
/// </summary>
public class DbInitializer(HrevolveDbContext context, ILogger<DbInitializer> logger)
{
    
    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // 应用迁移
            await context.Database.MigrateAsync();
            logger.LogInformation("数据库迁移完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库迁移失败");
            throw;
        }
    }
    
    /// <summary>
    /// 创建种子数据
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await SeedTenantsAsync();
            await context.SaveChangesAsync(); // 先保存租户
            
            await SeedRolesAsync();
            await context.SaveChangesAsync(); // 保存角色
            
            await SeedUsersAsync();
            await context.SaveChangesAsync(); // 保存用户
            
            logger.LogInformation("种子数据创建完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "种子数据创建失败");
            throw;
        }
    }
    
    private async Task SeedTenantsAsync()
    {
        if (await context.Tenants.AnyAsync()) return;
        
        // 创建默认租户
        var defaultTenant = Tenant.Create("默认租户", "default", TenantPlan.Professional);
        await context.Tenants.AddAsync(defaultTenant);
        
        logger.LogInformation("默认租户已创建: {TenantCode}", defaultTenant.Code);
    }
    
    private async Task SeedRolesAsync()
    {
        if (await context.Roles.AnyAsync()) return;
        
        var defaultTenant = await context.Tenants.FirstAsync();
        
        // 创建系统角色
        var roles = new[]
        {
            CreateRole(defaultTenant.Id, "系统管理员", "system_admin", true, 
                Permissions.SystemAdmin),
            
            CreateRole(defaultTenant.Id, "HR管理员", "hr_admin", true,
                Permissions.EmployeeRead, Permissions.EmployeeWrite, Permissions.EmployeeDelete,
                Permissions.OrganizationRead, Permissions.OrganizationWrite,
                Permissions.AttendanceRead, Permissions.AttendanceWrite, Permissions.AttendanceApprove,
                Permissions.LeaveRead, Permissions.LeaveWrite, Permissions.LeaveApprove,
                Permissions.PayrollRead, Permissions.PayrollWrite, Permissions.PayrollApprove,
                Permissions.ExpenseRead, Permissions.ExpenseWrite, Permissions.ExpenseApprove),
            
            CreateRole(defaultTenant.Id, "部门经理", "manager", true,
                Permissions.EmployeeRead,
                Permissions.OrganizationRead,
                Permissions.AttendanceRead, Permissions.AttendanceApprove,
                Permissions.LeaveRead, Permissions.LeaveApprove,
                Permissions.ExpenseRead, Permissions.ExpenseApprove),
            
            CreateRole(defaultTenant.Id, "普通员工", "employee", true,
                Permissions.EmployeeRead,
                Permissions.OrganizationRead,
                Permissions.AttendanceRead, Permissions.AttendanceWrite,
                Permissions.LeaveRead, Permissions.LeaveWrite,
                Permissions.ExpenseRead, Permissions.ExpenseWrite)
        };
        
        await context.Roles.AddRangeAsync(roles);
        logger.LogInformation("系统角色已创建: {Count} 个", roles.Length);
    }
    
    private static Role CreateRole(Guid tenantId, string name, string code, bool isSystem, params string[] permissions)
    {
        var role = Role.Create(tenantId, name, code, isSystem);
        foreach (var permission in permissions)
        {
            role.AddPermission(permission);
        }
        return role;
    }
    
    private async Task SeedUsersAsync()
    {
        if (await context.Users.AnyAsync()) return;
        
        var defaultTenant = await context.Tenants.FirstAsync();
        var adminRole = await context.Roles.FirstAsync(r => r.Code == "system_admin");
        
        // 创建管理员用户
        var admin = User.Create(defaultTenant.Id, "admin", "admin@hrevolve.com");
        admin.SetPassword("admin123"); // 演示用明文密码，生产环境应使用BCrypt哈希
        admin.AddRole(adminRole.Id);
        
        await context.Users.AddAsync(admin);
        logger.LogInformation("管理员用户已创建: admin / admin123");
    }
}
