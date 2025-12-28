namespace Hrevolve.Application.Identity.Commands;

/// <summary>
/// 登录响应DTO
/// </summary>
public record LoginResponse
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = null!;
    public bool RequiresMfa { get; init; }
}

/// <summary>
/// 用户名密码登录命令
/// </summary>
public record LoginCommand : IRequest<Result<LoginResponse>>
{
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string? TenantCode { get; init; }
    public string? DeviceId { get; init; }
    public string? IpAddress { get; init; }
}

/// <summary>
/// 登录命令验证器
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空");
    }
}

/// <summary>
/// 登录命令处理器
/// </summary>
public class LoginCommandHandler(
    IUserRepository userRepository,
    IConfiguration configuration) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 查找用户（支持用户名或邮箱登录）
        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken)
                   ?? await userRepository.GetByEmailAsync(request.Username, cancellationToken);
        
        if (user == null)
        {
            return Result.Failure<LoginResponse>("用户名或密码错误", "INVALID_CREDENTIALS");
        }
        
        // 检查账户状态
        if (user.IsLockedOut)
        {
            return Result.Failure<LoginResponse>("账户已被锁定，请稍后重试", "ACCOUNT_LOCKED");
        }
        
        if (user.Status != Domain.Identity.UserStatus.Active)
        {
            return Result.Failure<LoginResponse>("账户已被禁用", "ACCOUNT_DISABLED");
        }
        
        // 验证密码（实际项目中应使用BCrypt等安全哈希）
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            // 这里应该保存更改
            return Result.Failure<LoginResponse>("用户名或密码错误", "INVALID_CREDENTIALS");
        }
        
        // 检查是否需要MFA
        if (user.MfaEnabled)
        {
            // 检查是否为受信任设备
            var isTrustedDevice = user.TrustedDevices.Any(d => d.DeviceId == request.DeviceId);
            if (!isTrustedDevice)
            {
                return Result.Success(new LoginResponse
                {
                    RequiresMfa = true,
                    UserId = user.Id,
                    UserName = user.Username,
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ExpiresAt = DateTime.UtcNow
                });
            }
        }
        
        // 记录登录
        user.RecordLogin(request.IpAddress ?? "unknown");
        
        // 获取用户权限
        var permissions = await userRepository.GetPermissionsAsync(user.Id, cancellationToken);
        
        // 生成JWT Token
        var token = GenerateJwtToken(user, permissions);
        var refreshToken = GenerateRefreshToken();
        
        return Result.Success(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            UserId = user.Id,
            UserName = user.Username,
            RequiresMfa = false
        });
    }
    
    private bool VerifyPassword(string password, string? passwordHash)
    {
        // 实际项目中应使用BCrypt.Net-Next等库
        // return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        return !string.IsNullOrEmpty(passwordHash) && passwordHash == password; // 仅用于演示
    }
    
    private string GenerateJwtToken(Domain.Identity.User user, IReadOnlyList<string> permissions)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("username", user.Username)
        };
        
        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
        }
        
        // 添加权限声明
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
