namespace Hrevolve.Application.Employees.Commands;

/// <summary>
/// 创建员工命令
/// </summary>
public record CreateEmployeeCommand : IRequest<Result<Guid>>
{
    public string EmployeeNumber { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? EnglishName { get; init; }
    public Gender Gender { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public DateOnly HireDate { get; init; }
    public EmploymentType EmploymentType { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? DirectManagerId { get; init; }
    
    // 初始职位信息
    public Guid PositionId { get; init; }
    public Guid DepartmentId { get; init; }
    public decimal BaseSalary { get; init; }
}

/// <summary>
/// 创建员工命令验证器
/// </summary>
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("员工编号不能为空")
            .MaximumLength(50).WithMessage("员工编号不能超过50个字符");
        
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("名字不能为空")
            .MaximumLength(50).WithMessage("名字不能超过50个字符");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("姓氏不能为空")
            .MaximumLength(50).WithMessage("姓氏不能超过50个字符");
        
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("出生日期必须早于今天");
        
        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("入职日期不能为空");
        
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("邮箱格式不正确");
        
        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("职位不能为空");
        
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("部门不能为空");
        
        RuleFor(x => x.BaseSalary)
            .GreaterThanOrEqualTo(0).WithMessage("基本工资不能为负数");
    }
}

/// <summary>
/// 创建员工命令处理器
/// </summary>
public class CreateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ITenantContextAccessor tenantContextAccessor) : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
        
        // 检查员工编号是否已存在
        var existing = await employeeRepository.GetByEmployeeNumberAsync(request.EmployeeNumber, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<Guid>("员工编号已存在", "DUPLICATE_EMPLOYEE_NUMBER");
        }
        
        // 创建员工
        var employee = Employee.Create(
            tenantId,
            request.EmployeeNumber,
            request.FirstName,
            request.LastName,
            request.Gender,
            request.DateOfBirth,
            request.HireDate,
            request.EmploymentType);
        
        employee.SetContactInfo(request.Email, request.Phone, null, null);
        
        if (request.DirectManagerId.HasValue)
        {
            employee.SetDirectManager(request.DirectManagerId.Value);
        }
        
        await employeeRepository.AddAsync(employee, cancellationToken);
        
        // 创建初始职位历史记录
        var jobHistory = JobHistory.Create(
            tenantId,
            employee.Id,
            request.PositionId,
            request.DepartmentId,
            request.BaseSalary,
            request.HireDate,
            JobChangeType.NewHire,
            "新员工入职");
        
        // 这里需要通过DbContext添加JobHistory
        // 实际项目中应该有专门的JobHistoryRepository
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(employee.Id);
    }
}
