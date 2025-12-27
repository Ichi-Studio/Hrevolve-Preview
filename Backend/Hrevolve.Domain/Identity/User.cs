using Hrevolve.Domain.Common;

namespace Hrevolve.Domain.Identity;

/// <summary>
/// 用户实体 - 认证与授权核心
/// </summary>
public class User : AuditableEntity
{
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    
    /// <summary>
    /// 密码哈希（BCrypt）
    /// </summary>
    public string? PasswordHash { get; private set; }
    
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool PhoneConfirmed { get; private set; }
    
    /// <summary>
    /// MFA是否启用
    /// </summary>
    public bool MfaEnabled { get; private set; }
    
    /// <summary>
    /// TOTP密钥（加密存储）
    /// </summary>
    public string? TotpSecret { get; private set; }
    
    /// <summary>
    /// 备用恢复代码（加密存储）
    /// </summary>
    public string? RecoveryCodes { get; private set; }
    
    /// <summary>
    /// 登录失败次数
    /// </summary>
    public int AccessFailedCount { get; private set; }
    
    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTime? LockoutEnd { get; private set; }
    
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    
    /// <summary>
    /// 关联的员工ID
    /// </summary>
    public Guid? EmployeeId { get; private set; }
    
    /// <summary>
    /// 外部登录提供商
    /// </summary>
    private readonly List<ExternalLogin> _externalLogins = [];
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();
    
    /// <summary>
    /// 用户角色
    /// </summary>
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    
    /// <summary>
    /// 受信任设备
    /// </summary>
    private readonly List<TrustedDevice> _trustedDevices = [];
    public IReadOnlyCollection<TrustedDevice> TrustedDevices => _trustedDevices.AsReadOnly();
    
    private User() { }
    
    public static User Create(Guid tenantId, string username, string email)
    {
        return new User
        {
            TenantId = tenantId,
            Username = username,
            Email = email.ToLowerInvariant(),
            Status = UserStatus.Active
        };
    }
    
    public void SetPassword(string passwordHash) => PasswordHash = passwordHash;
    
    public void EnableMfa(string totpSecret, string recoveryCodes)
    {
        MfaEnabled = true;
        TotpSecret = totpSecret;
        RecoveryCodes = recoveryCodes;
    }
    
    public void DisableMfa()
    {
        MfaEnabled = false;
        TotpSecret = null;
        RecoveryCodes = null;
    }
    
    public void RecordLogin(string ip)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ip;
        AccessFailedCount = 0;
        LockoutEnd = null;
    }
    
    public void RecordFailedLogin()
    {
        AccessFailedCount++;
        if (AccessFailedCount >= 5)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        }
    }
    
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    
    public void LinkEmployee(Guid employeeId) => EmployeeId = employeeId;
    
    public void AddExternalLogin(string provider, string providerKey)
    {
        _externalLogins.Add(new ExternalLogin(Id, provider, providerKey));
    }
    
    public void AddRole(Guid roleId)
    {
        if (!_userRoles.Any(ur => ur.RoleId == roleId))
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }
    }
    
    public void AddTrustedDevice(string deviceId, string deviceName, string userAgent)
    {
        _trustedDevices.Add(new TrustedDevice(Id, deviceId, deviceName, userAgent));
    }
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}

/// <summary>
/// 外部登录（SSO）
/// </summary>
public class ExternalLogin
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = null!; // Google, Microsoft, WeChat, WhatsApp
    public string ProviderKey { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    private ExternalLogin() { }
    
    public ExternalLogin(Guid userId, string provider, string providerKey)
    {
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
    }
}

/// <summary>
/// 受信任设备
/// </summary>
public class TrustedDevice
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = null!;
    public string DeviceName { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
    public DateTime TrustedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; private set; }
    
    private TrustedDevice() { }
    
    public TrustedDevice(Guid userId, string deviceId, string deviceName, string userAgent)
    {
        UserId = userId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        UserAgent = userAgent;
    }
}
