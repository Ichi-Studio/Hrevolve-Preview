using Hrevolve.Domain.Common;
using Hrevolve.Infrastructure.MultiTenancy;
using Hrevolve.Infrastructure.Persistence;
using Hrevolve.Infrastructure.Persistence.Repositories;
using Hrevolve.Shared.Identity;
using Hrevolve.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hrevolve.Infrastructure;

/// <summary>
/// 基础设施层依赖注入配置
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 数据库上下文
        services.AddDbContext<HrevolveDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(HrevolveDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
        });
        
        // Redis缓存
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Hrevolve:";
        });
        
        // 多租户
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        
        // 当前用户
        services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
        
        // 工作单元
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // 数据库初始化器
        services.AddScoped<DbInitializer>();
        
        // 仓储
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        
        return services;
    }
}
