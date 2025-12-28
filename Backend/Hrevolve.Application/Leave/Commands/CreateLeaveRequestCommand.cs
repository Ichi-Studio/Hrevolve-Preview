namespace Hrevolve.Application.Leave.Commands;

/// <summary>
/// 创建请假申请命令
/// </summary>
public record CreateLeaveRequestCommand : IRequest<Result<Guid>>
{
    public Guid LeaveTypeId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public DayPart StartDayPart { get; init; } = DayPart.FullDay;
    public DayPart EndDayPart { get; init; } = DayPart.FullDay;
    public string Reason { get; init; } = null!;
    public List<string>? Attachments { get; init; }
}

/// <summary>
/// 创建请假申请命令验证器
/// </summary>
public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId)
            .NotEmpty().WithMessage("请假类型不能为空");
        
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("开始日期不能为空");
        
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("结束日期不能为空")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("结束日期不能早于开始日期");
        
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("请假原因不能为空")
            .MaximumLength(1000).WithMessage("请假原因不能超过1000个字符");
    }
}

/// <summary>
/// 创建请假申请命令处理器
/// </summary>
public class CreateLeaveRequestCommandHandler(
    Infrastructure.Persistence.HrevolveDbContext context,
    ICurrentUserAccessor currentUserAccessor,
    ITenantContextAccessor tenantContextAccessor,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateLeaveRequestCommand, Result<Guid>>
{
    
    public async Task<Result<Guid>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var currentUser = currentUserAccessor.CurrentUser;
        var tenantId = tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
        
        if (currentUser?.EmployeeId == null)
        {
            return Result.Failure<Guid>("当前用户未关联员工信息", "NO_EMPLOYEE_LINKED");
        }
        
        var employeeId = currentUser.EmployeeId.Value;
        
        // 验证假期类型
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == request.LeaveTypeId && lt.IsActive, cancellationToken);
        
        if (leaveType == null)
        {
            return Result.Failure<Guid>("假期类型不存在或已禁用", "INVALID_LEAVE_TYPE");
        }
        
        // 检查假期余额
        var year = request.StartDate.Year;
        var balance = await context.LeaveBalances
            .FirstOrDefaultAsync(lb => 
                lb.EmployeeId == employeeId && 
                lb.LeaveTypeId == request.LeaveTypeId && 
                lb.Year == year, 
                cancellationToken);
        
        // 计算请假天数
        var totalDays = CalculateLeaveDays(request.StartDate, request.EndDate, request.StartDayPart, request.EndDayPart);
        
        if (balance != null && balance.Available < totalDays)
        {
            return Result.Failure<Guid>($"假期余额不足，可用: {balance.Available}天，申请: {totalDays}天", "INSUFFICIENT_BALANCE");
        }
        
        // 检查日期冲突
        var hasConflict = await context.LeaveRequests
            .AnyAsync(lr => 
                lr.EmployeeId == employeeId &&
                lr.Status != LeaveRequestStatus.Cancelled &&
                lr.Status != LeaveRequestStatus.Rejected &&
                lr.StartDate <= request.EndDate &&
                lr.EndDate >= request.StartDate,
                cancellationToken);
        
        if (hasConflict)
        {
            return Result.Failure<Guid>("所选日期与已有请假申请冲突", "DATE_CONFLICT");
        }
        
        // 创建请假申请
        var leaveRequest = LeaveRequest.Create(
            tenantId,
            employeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.StartDayPart,
            request.EndDayPart,
            request.Reason);
        
        await context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
        
        // 更新待审批余额
        if (balance != null)
        {
            balance.AddPending(totalDays);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(leaveRequest.Id);
    }
    
    private static decimal CalculateLeaveDays(DateOnly startDate, DateOnly endDate, DayPart startDayPart, DayPart endDayPart)
    {
        var days = (endDate.DayNumber - startDate.DayNumber) + 1m;
        
        if (startDayPart == DayPart.Afternoon) days -= 0.5m;
        if (endDayPart == DayPart.Morning) days -= 0.5m;
        
        return days;
    }
}
