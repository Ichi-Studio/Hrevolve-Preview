using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// HR Schema 提供者实现 - 提供 HR 系统实体的结构描述
/// </summary>
public class HrSchemaProvider : ISchemaProvider
{
    private readonly SchemaDescriptor _schema;
    private readonly Dictionary<string, EntitySchema> _entityMap;
    private readonly Dictionary<string, string> _entityAliasMap;
    private readonly List<QueryExample> _examples;

    public HrSchemaProvider()
    {
        _schema = BuildSchema();
        _entityMap = _schema.Entities.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        _entityAliasMap = BuildEntityAliasMap();
        _examples = BuildQueryExamples();
    }

    public SchemaDescriptor GetSchema() => _schema;

    public EntitySchema? GetEntitySchema(string entityName)
    {
        return _entityMap.GetValueOrDefault(entityName);
    }

    public IReadOnlyList<string> GetAllEntityNames() => _schema.Entities.Select(e => e.Name).ToList();

    public bool EntityExists(string entityName) => _entityMap.ContainsKey(entityName);

    public bool FieldExists(string entityName, string fieldName)
    {
        if (!_entityMap.TryGetValue(entityName, out var entity)) return false;
        return entity.Fields.Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    public FieldSchema? GetFieldSchema(string entityName, string fieldName)
    {
        if (!_entityMap.TryGetValue(entityName, out var entity)) return null;
        return entity.Fields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    public string? FindEntityByAlias(string alias)
    {
        // 先精确匹配实体名
        if (_entityMap.ContainsKey(alias)) return alias;
        
        // 再匹配别名
        return _entityAliasMap.GetValueOrDefault(alias.ToLowerInvariant());
    }

    public string? FindFieldByAlias(string entityName, string alias)
    {
        if (!_entityMap.TryGetValue(entityName, out var entity)) return null;
        
        // 先精确匹配字段名
        var field = entity.Fields.FirstOrDefault(f => 
            f.Name.Equals(alias, StringComparison.OrdinalIgnoreCase));
        if (field != null) return field.Name;
        
        // 再匹配别名
        field = entity.Fields.FirstOrDefault(f => 
            f.Aliases.Any(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase)) ||
            f.DisplayName.Equals(alias, StringComparison.OrdinalIgnoreCase));
        
        return field?.Name;
    }

    public string GetPromptSchemaDescription() => _schema.ToPromptFormat();

    public IReadOnlyList<QueryExample> GetQueryExamples() => _examples;

    private Dictionary<string, string> BuildEntityAliasMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in _schema.Entities)
        {
            map[entity.DisplayName.ToLowerInvariant()] = entity.Name;
            foreach (var alias in entity.Aliases)
            {
                map[alias.ToLowerInvariant()] = entity.Name;
            }
        }
        return map;
    }

    private static SchemaDescriptor BuildSchema()
    {
        return new SchemaDescriptor
        {
            Entities =
            [
                BuildEmployeeSchema(),
                BuildAttendanceRecordSchema(),
                BuildLeaveRequestSchema(),
                BuildLeaveBalanceSchema(),
                BuildLeaveTypeSchema(),
                BuildPayrollRecordSchema(),
                BuildOrganizationUnitSchema(),
                BuildPositionSchema()
            ]
        };
    }

    private static EntitySchema BuildEmployeeSchema()
    {
        return new EntitySchema
        {
            Name = "Employee",
            DisplayName = "员工",
            Description = "员工基本信息表，存储员工的个人信息、联系方式、雇佣状态等",
            Aliases = ["员工", "职员", "成员", "人员", "员工信息"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "员工ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "EmployeeNumber", DisplayName = "员工编号", DataType = "string", Aliases = ["工号", "编号"] },
                new FieldSchema { Name = "FirstName", DisplayName = "名字", DataType = "string", Aliases = ["名"] },
                new FieldSchema { Name = "LastName", DisplayName = "姓氏", DataType = "string", Aliases = ["姓"] },
                new FieldSchema { Name = "EnglishName", DisplayName = "英文名", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "Gender", DisplayName = "性别", DataType = "enum", 
                    EnumValues = [
                        new EnumValue { Name = "Male", Value = 0, DisplayName = "男" },
                        new EnumValue { Name = "Female", Value = 1, DisplayName = "女" },
                        new EnumValue { Name = "Other", Value = 2, DisplayName = "其他" }
                    ]
                },
                new FieldSchema { Name = "DateOfBirth", DisplayName = "出生日期", DataType = "DateOnly", IsNullable = true, Aliases = ["生日"] },
                new FieldSchema { Name = "Email", DisplayName = "工作邮箱", DataType = "string", Aliases = ["邮箱", "邮件"] },
                new FieldSchema { Name = "Phone", DisplayName = "工作电话", DataType = "string", IsNullable = true, Aliases = ["电话", "手机"] },
                new FieldSchema { Name = "IdCardNumber", DisplayName = "身份证号", DataType = "string", IsSensitive = true, RequiredPermission = "hr:admin", IsNullable = true },
                new FieldSchema { Name = "PersonalEmail", DisplayName = "个人邮箱", DataType = "string", IsSensitive = true, IsNullable = true },
                new FieldSchema { Name = "Address", DisplayName = "住址", DataType = "string", IsNullable = true, Aliases = ["地址"] },
                new FieldSchema { Name = "Status", DisplayName = "雇佣状态", DataType = "enum", Aliases = ["状态"],
                    EnumValues = [
                        new EnumValue { Name = "Active", Value = 0, DisplayName = "在职" },
                        new EnumValue { Name = "OnLeave", Value = 1, DisplayName = "休假中" },
                        new EnumValue { Name = "Suspended", Value = 2, DisplayName = "停职" },
                        new EnumValue { Name = "Terminated", Value = 3, DisplayName = "已离职" }
                    ]
                },
                new FieldSchema { Name = "EmploymentType", DisplayName = "雇佣类型", DataType = "enum",
                    EnumValues = [
                        new EnumValue { Name = "FullTime", Value = 0, DisplayName = "全职" },
                        new EnumValue { Name = "PartTime", Value = 1, DisplayName = "兼职" },
                        new EnumValue { Name = "Contract", Value = 2, DisplayName = "合同工" },
                        new EnumValue { Name = "Intern", Value = 3, DisplayName = "实习生" },
                        new EnumValue { Name = "Consultant", Value = 4, DisplayName = "顾问" }
                    ]
                },
                new FieldSchema { Name = "HireDate", DisplayName = "入职日期", DataType = "DateOnly", Aliases = ["入职时间"] },
                new FieldSchema { Name = "TerminationDate", DisplayName = "离职日期", DataType = "DateOnly", IsNullable = true },
                new FieldSchema { Name = "ProbationEndDate", DisplayName = "试用期结束日期", DataType = "DateOnly", IsNullable = true },
                new FieldSchema { Name = "DirectManagerId", DisplayName = "直属上级ID", DataType = "Guid", IsNullable = true, IsForeignKey = true, ForeignKeyEntity = "Employee" },
                new FieldSchema { Name = "CreatedAt", DisplayName = "创建时间", DataType = "DateTime", IsReadOnly = true },
                new FieldSchema { Name = "UpdatedAt", DisplayName = "更新时间", DataType = "DateTime", IsNullable = true, IsReadOnly = true }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "DirectManager", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "DirectManagerId", Description = "直属上级" },
                new RelationshipSchema { Name = "AttendanceRecords", RelatedEntity = "AttendanceRecord", RelationType = "OneToMany", ForeignKey = "EmployeeId", Description = "考勤记录" },
                new RelationshipSchema { Name = "LeaveRequests", RelatedEntity = "LeaveRequest", RelationType = "OneToMany", ForeignKey = "EmployeeId", Description = "请假申请" }
            ]
        };
    }

    private static EntitySchema BuildAttendanceRecordSchema()
    {
        return new EntitySchema
        {
            Name = "AttendanceRecord",
            DisplayName = "考勤记录",
            Description = "员工每日考勤打卡记录，包含签到签退时间、迟到早退等信息",
            Aliases = ["考勤", "打卡记录", "出勤记录", "签到记录"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "记录ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "EmployeeId", DisplayName = "员工ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "Employee" },
                new FieldSchema { Name = "AttendanceDate", DisplayName = "考勤日期", DataType = "DateOnly", Aliases = ["日期", "打卡日期"] },
                new FieldSchema { Name = "CheckInTime", DisplayName = "签到时间", DataType = "DateTime", IsNullable = true, Aliases = ["上班时间", "打卡时间"] },
                new FieldSchema { Name = "CheckOutTime", DisplayName = "签退时间", DataType = "DateTime", IsNullable = true, Aliases = ["下班时间"] },
                new FieldSchema { Name = "CheckInMethod", DisplayName = "签到方式", DataType = "enum", IsNullable = true,
                    EnumValues = [
                        new EnumValue { Name = "App", Value = 0, DisplayName = "APP打卡" },
                        new EnumValue { Name = "WiFi", Value = 1, DisplayName = "WiFi打卡" },
                        new EnumValue { Name = "Device", Value = 2, DisplayName = "设备打卡" },
                        new EnumValue { Name = "Manual", Value = 3, DisplayName = "手动补卡" },
                        new EnumValue { Name = "Web", Value = 4, DisplayName = "网页打卡" }
                    ]
                },
                new FieldSchema { Name = "CheckOutMethod", DisplayName = "签退方式", DataType = "enum", IsNullable = true },
                new FieldSchema { Name = "CheckInLocation", DisplayName = "签到位置", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "CheckOutLocation", DisplayName = "签退位置", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "Status", DisplayName = "考勤状态", DataType = "enum", Aliases = ["状态"],
                    EnumValues = [
                        new EnumValue { Name = "Pending", Value = 0, DisplayName = "待确认" },
                        new EnumValue { Name = "Normal", Value = 1, DisplayName = "正常" },
                        new EnumValue { Name = "Late", Value = 2, DisplayName = "迟到" },
                        new EnumValue { Name = "EarlyLeave", Value = 3, DisplayName = "早退" },
                        new EnumValue { Name = "Absent", Value = 4, DisplayName = "缺勤" },
                        new EnumValue { Name = "Incomplete", Value = 5, DisplayName = "打卡不完整" },
                        new EnumValue { Name = "Leave", Value = 6, DisplayName = "请假" },
                        new EnumValue { Name = "BusinessTrip", Value = 7, DisplayName = "出差" }
                    ]
                },
                new FieldSchema { Name = "LateMinutes", DisplayName = "迟到分钟数", DataType = "int", Aliases = ["迟到时长"] },
                new FieldSchema { Name = "EarlyLeaveMinutes", DisplayName = "早退分钟数", DataType = "int", Aliases = ["早退时长"] },
                new FieldSchema { Name = "ActualHours", DisplayName = "实际工时", DataType = "decimal", Aliases = ["工时", "工作时长"] },
                new FieldSchema { Name = "OvertimeHours", DisplayName = "加班时长", DataType = "decimal", Aliases = ["加班"] },
                new FieldSchema { Name = "Remarks", DisplayName = "备注", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "IsApproved", DisplayName = "是否已审核", DataType = "bool" },
                new FieldSchema { Name = "ApprovedBy", DisplayName = "审核人ID", DataType = "Guid", IsNullable = true },
                new FieldSchema { Name = "ApprovedAt", DisplayName = "审核时间", DataType = "DateTime", IsNullable = true }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "Employee", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "EmployeeId", Description = "所属员工" }
            ]
        };
    }

    private static EntitySchema BuildLeaveRequestSchema()
    {
        return new EntitySchema
        {
            Name = "LeaveRequest",
            DisplayName = "请假申请",
            Description = "员工请假申请记录，包含请假类型、起止日期、审批状态等",
            Aliases = ["请假", "假期申请", "休假申请", "请假记录"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "申请ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "EmployeeId", DisplayName = "员工ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "Employee" },
                new FieldSchema { Name = "LeaveTypeId", DisplayName = "假期类型ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "LeaveType" },
                new FieldSchema { Name = "StartDate", DisplayName = "开始日期", DataType = "DateOnly", Aliases = ["起始日期"] },
                new FieldSchema { Name = "EndDate", DisplayName = "结束日期", DataType = "DateOnly", Aliases = ["截止日期"] },
                new FieldSchema { Name = "StartDayPart", DisplayName = "开始时段", DataType = "enum",
                    EnumValues = [
                        new EnumValue { Name = "FullDay", Value = 0, DisplayName = "全天" },
                        new EnumValue { Name = "Morning", Value = 1, DisplayName = "上午" },
                        new EnumValue { Name = "Afternoon", Value = 2, DisplayName = "下午" }
                    ]
                },
                new FieldSchema { Name = "EndDayPart", DisplayName = "结束时段", DataType = "enum" },
                new FieldSchema { Name = "TotalDays", DisplayName = "请假天数", DataType = "decimal", Aliases = ["天数", "请假时长"] },
                new FieldSchema { Name = "Reason", DisplayName = "请假原因", DataType = "string", Aliases = ["原因", "事由"] },
                new FieldSchema { Name = "Attachments", DisplayName = "附件", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "Status", DisplayName = "申请状态", DataType = "enum", Aliases = ["状态"],
                    EnumValues = [
                        new EnumValue { Name = "Pending", Value = 0, DisplayName = "待审批" },
                        new EnumValue { Name = "Approved", Value = 1, DisplayName = "已批准" },
                        new EnumValue { Name = "Rejected", Value = 2, DisplayName = "已拒绝" },
                        new EnumValue { Name = "Cancelled", Value = 3, DisplayName = "已取消" }
                    ]
                },
                new FieldSchema { Name = "CancelReason", DisplayName = "取消原因", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "CreatedAt", DisplayName = "申请时间", DataType = "DateTime", IsReadOnly = true }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "Employee", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "EmployeeId", Description = "申请人" },
                new RelationshipSchema { Name = "LeaveType", RelatedEntity = "LeaveType", RelationType = "ManyToOne", ForeignKey = "LeaveTypeId", Description = "假期类型" }
            ]
        };
    }

    private static EntitySchema BuildLeaveBalanceSchema()
    {
        return new EntitySchema
        {
            Name = "LeaveBalance",
            DisplayName = "假期余额",
            Description = "员工各类假期的年度余额信息",
            Aliases = ["假期余额", "年假余额", "休假余额"],
            SupportsCrud = false, // 余额由系统计算，不允许直接修改
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "记录ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "EmployeeId", DisplayName = "员工ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "Employee" },
                new FieldSchema { Name = "LeaveTypeId", DisplayName = "假期类型ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "LeaveType" },
                new FieldSchema { Name = "Year", DisplayName = "年份", DataType = "int" },
                new FieldSchema { Name = "Entitlement", DisplayName = "年度额度", DataType = "decimal", Aliases = ["额度", "总额度"] },
                new FieldSchema { Name = "CarriedOver", DisplayName = "结转额度", DataType = "decimal", Aliases = ["结转"] },
                new FieldSchema { Name = "Used", DisplayName = "已使用", DataType = "decimal", Aliases = ["已用"] },
                new FieldSchema { Name = "Pending", DisplayName = "待审批", DataType = "decimal" }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "Employee", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "EmployeeId", Description = "所属员工" },
                new RelationshipSchema { Name = "LeaveType", RelatedEntity = "LeaveType", RelationType = "ManyToOne", ForeignKey = "LeaveTypeId", Description = "假期类型" }
            ]
        };
    }

    private static EntitySchema BuildLeaveTypeSchema()
    {
        return new EntitySchema
        {
            Name = "LeaveType",
            DisplayName = "假期类型",
            Description = "系统支持的假期类型定义",
            Aliases = ["假期类型", "假期种类", "假别"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "类型ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "Name", DisplayName = "类型名称", DataType = "string", Aliases = ["名称"] },
                new FieldSchema { Name = "Code", DisplayName = "类型代码", DataType = "string", Aliases = ["代码"] },
                new FieldSchema { Name = "Description", DisplayName = "描述", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "IsPaid", DisplayName = "是否带薪", DataType = "bool", Aliases = ["带薪"] },
                new FieldSchema { Name = "RequiresApproval", DisplayName = "是否需要审批", DataType = "bool" },
                new FieldSchema { Name = "AllowHalfDay", DisplayName = "允许半天", DataType = "bool" },
                new FieldSchema { Name = "MinUnit", DisplayName = "最小单位", DataType = "decimal" },
                new FieldSchema { Name = "MaxDaysPerRequest", DisplayName = "单次最大天数", DataType = "int", IsNullable = true },
                new FieldSchema { Name = "RequiresAttachment", DisplayName = "需要附件", DataType = "bool" },
                new FieldSchema { Name = "Color", DisplayName = "颜色标识", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "IsActive", DisplayName = "是否激活", DataType = "bool" }
            ]
        };
    }

    private static EntitySchema BuildPayrollRecordSchema()
    {
        return new EntitySchema
        {
            Name = "PayrollRecord",
            DisplayName = "薪资记录",
            Description = "员工月度薪资发放记录，包含基本工资、扣款、实发等",
            Aliases = ["薪资", "工资", "工资单", "薪酬记录"],
            SupportsCrud = false, // 薪资记录不允许通过 Text2SQL 修改
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "记录ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true, IsSensitive = true, RequiredPermission = "payroll:read" },
                new FieldSchema { Name = "EmployeeId", DisplayName = "员工ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "Employee", IsSensitive = true, RequiredPermission = "payroll:read" },
                new FieldSchema { Name = "PayrollPeriodId", DisplayName = "薪资周期ID", DataType = "Guid", IsSensitive = true, RequiredPermission = "payroll:read" },
                new FieldSchema { Name = "BaseSalary", DisplayName = "基本工资", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["底薪"] },
                new FieldSchema { Name = "GrossSalary", DisplayName = "应发工资", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["税前工资"] },
                new FieldSchema { Name = "TotalDeductions", DisplayName = "扣除总额", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["扣款"] },
                new FieldSchema { Name = "NetSalary", DisplayName = "实发工资", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["税后工资", "到手工资"] },
                new FieldSchema { Name = "IncomeTax", DisplayName = "个人所得税", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["个税"] },
                new FieldSchema { Name = "SocialInsuranceEmployee", DisplayName = "社保个人部分", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["社保"] },
                new FieldSchema { Name = "HousingFundEmployee", DisplayName = "公积金个人部分", DataType = "decimal", IsSensitive = true, RequiredPermission = "payroll:read", Aliases = ["公积金"] },
                new FieldSchema { Name = "Status", DisplayName = "状态", DataType = "enum", IsSensitive = true, RequiredPermission = "payroll:read",
                    EnumValues = [
                        new EnumValue { Name = "Draft", Value = 0, DisplayName = "草稿" },
                        new EnumValue { Name = "Calculated", Value = 1, DisplayName = "已计算" },
                        new EnumValue { Name = "Approved", Value = 2, DisplayName = "已审批" },
                        new EnumValue { Name = "Paid", Value = 3, DisplayName = "已发放" }
                    ]
                }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "Employee", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "EmployeeId", Description = "所属员工" }
            ]
        };
    }

    private static EntitySchema BuildOrganizationUnitSchema()
    {
        return new EntitySchema
        {
            Name = "OrganizationUnit",
            DisplayName = "组织单元",
            Description = "公司组织架构，包含公司、部门、团队等层级",
            Aliases = ["组织", "部门", "团队", "组织架构"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "组织ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "Name", DisplayName = "组织名称", DataType = "string", Aliases = ["名称", "部门名称"] },
                new FieldSchema { Name = "Code", DisplayName = "组织代码", DataType = "string", Aliases = ["代码", "部门代码"] },
                new FieldSchema { Name = "Description", DisplayName = "描述", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "ParentId", DisplayName = "父级组织ID", DataType = "Guid", IsNullable = true, IsForeignKey = true, ForeignKeyEntity = "OrganizationUnit" },
                new FieldSchema { Name = "Path", DisplayName = "路径", DataType = "string", Aliases = ["组织路径"] },
                new FieldSchema { Name = "Level", DisplayName = "层级", DataType = "int" },
                new FieldSchema { Name = "SortOrder", DisplayName = "排序", DataType = "int" },
                new FieldSchema { Name = "Type", DisplayName = "组织类型", DataType = "enum",
                    EnumValues = [
                        new EnumValue { Name = "Company", Value = 0, DisplayName = "公司" },
                        new EnumValue { Name = "Division", Value = 1, DisplayName = "事业部" },
                        new EnumValue { Name = "Department", Value = 2, DisplayName = "部门" },
                        new EnumValue { Name = "Team", Value = 3, DisplayName = "团队" },
                        new EnumValue { Name = "Group", Value = 4, DisplayName = "小组" }
                    ]
                },
                new FieldSchema { Name = "IsActive", DisplayName = "是否激活", DataType = "bool" },
                new FieldSchema { Name = "ManagerId", DisplayName = "负责人ID", DataType = "Guid", IsNullable = true, IsForeignKey = true, ForeignKeyEntity = "Employee" }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "Parent", RelatedEntity = "OrganizationUnit", RelationType = "ManyToOne", ForeignKey = "ParentId", Description = "父级组织" },
                new RelationshipSchema { Name = "Children", RelatedEntity = "OrganizationUnit", RelationType = "OneToMany", ForeignKey = "ParentId", Description = "子组织" },
                new RelationshipSchema { Name = "Manager", RelatedEntity = "Employee", RelationType = "ManyToOne", ForeignKey = "ManagerId", Description = "负责人" }
            ]
        };
    }

    private static EntitySchema BuildPositionSchema()
    {
        return new EntitySchema
        {
            Name = "Position",
            DisplayName = "职位",
            Description = "职位定义，包含职位名称、职级、薪资范围等",
            Aliases = ["职位", "岗位", "职务"],
            SupportsCrud = true,
            Fields =
            [
                new FieldSchema { Name = "Id", DisplayName = "职位ID", DataType = "Guid", IsPrimaryKey = true, IsReadOnly = true },
                new FieldSchema { Name = "Name", DisplayName = "职位名称", DataType = "string", Aliases = ["名称"] },
                new FieldSchema { Name = "Code", DisplayName = "职位代码", DataType = "string", Aliases = ["代码"] },
                new FieldSchema { Name = "Description", DisplayName = "描述", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "OrganizationUnitId", DisplayName = "所属组织ID", DataType = "Guid", IsForeignKey = true, ForeignKeyEntity = "OrganizationUnit" },
                new FieldSchema { Name = "Sequence", DisplayName = "序号", DataType = "string", IsNullable = true },
                new FieldSchema { Name = "Level", DisplayName = "职级", DataType = "enum" },
                new FieldSchema { Name = "SalaryRangeMin", DisplayName = "薪资下限", DataType = "decimal", IsSensitive = true, RequiredPermission = "hr:admin" },
                new FieldSchema { Name = "SalaryRangeMax", DisplayName = "薪资上限", DataType = "decimal", IsSensitive = true, RequiredPermission = "hr:admin" },
                new FieldSchema { Name = "IsActive", DisplayName = "是否激活", DataType = "bool" }
            ],
            Relationships =
            [
                new RelationshipSchema { Name = "OrganizationUnit", RelatedEntity = "OrganizationUnit", RelationType = "ManyToOne", ForeignKey = "OrganizationUnitId", Description = "所属组织" }
            ]
        };
    }

    private static List<QueryExample> BuildQueryExamples()
    {
        return
        [
            new QueryExample
            {
                Input = "查询张三的考勤记录",
                Description = "按姓名查询员工的考勤记录",
                ExpectedOutput = new QueryRequest
                {
                    Operation = QueryOperation.Select,
                    TargetEntity = "AttendanceRecord",
                    Joins = [new JoinClause { Entity = "Employee", On = "EmployeeId = Employee.Id" }],
                    Filters = [new FilterCondition { Field = "Employee.LastName", Operator = FilterOperator.Contains, Value = "张" }],
                    SelectFields = ["AttendanceDate", "CheckInTime", "CheckOutTime", "Status", "LateMinutes"],
                    OrderBy = [new OrderByClause { Field = "AttendanceDate", Descending = true }],
                    Limit = 100
                }
            },
            new QueryExample
            {
                Input = "显示销售部门的所有员工",
                Description = "按部门名称查询员工列表",
                ExpectedOutput = new QueryRequest
                {
                    Operation = QueryOperation.Select,
                    TargetEntity = "Employee",
                    Joins = [new JoinClause { Entity = "OrganizationUnit", On = "DepartmentId = OrganizationUnit.Id" }],
                    Filters = [new FilterCondition { Field = "OrganizationUnit.Name", Operator = FilterOperator.Contains, Value = "销售" }],
                    SelectFields = ["EmployeeNumber", "FirstName", "LastName", "Email", "Phone", "Status"],
                    OrderBy = [new OrderByClause { Field = "EmployeeNumber" }],
                    Limit = 100
                }
            },
            new QueryExample
            {
                Input = "统计本月请假人数",
                Description = "聚合查询统计请假人数",
                ExpectedOutput = new QueryRequest
                {
                    Operation = QueryOperation.Select,
                    TargetEntity = "LeaveRequest",
                    Aggregation = AggregationType.CountDistinct,
                    AggregationField = "EmployeeId",
                    Filters = [
                        new FilterCondition { Field = "StartDate", Operator = FilterOperator.GreaterThanOrEqual, Value = "@CurrentMonthStart" },
                        new FilterCondition { Field = "Status", Operator = FilterOperator.Equal, Value = "Approved" }
                    ]
                }
            },
            new QueryExample
            {
                Input = "查询本周迟到的员工",
                Description = "查询本周有迟到记录的员工",
                ExpectedOutput = new QueryRequest
                {
                    Operation = QueryOperation.Select,
                    TargetEntity = "AttendanceRecord",
                    Joins = [new JoinClause { Entity = "Employee", On = "EmployeeId = Employee.Id" }],
                    Filters = [
                        new FilterCondition { Field = "AttendanceDate", Operator = FilterOperator.GreaterThanOrEqual, Value = "@CurrentWeekStart" },
                        new FilterCondition { Field = "Status", Operator = FilterOperator.Equal, Value = "Late" }
                    ],
                    SelectFields = ["Employee.EmployeeNumber", "Employee.FirstName", "Employee.LastName", "AttendanceDate", "LateMinutes"],
                    OrderBy = [new OrderByClause { Field = "AttendanceDate", Descending = true }],
                    Limit = 100
                }
            },
            new QueryExample
            {
                Input = "查询所有在职员工的年假余额",
                Description = "关联查询员工和假期余额",
                ExpectedOutput = new QueryRequest
                {
                    Operation = QueryOperation.Select,
                    TargetEntity = "LeaveBalance",
                    Joins = [
                        new JoinClause { Entity = "Employee", On = "EmployeeId = Employee.Id" },
                        new JoinClause { Entity = "LeaveType", On = "LeaveTypeId = LeaveType.Id" }
                    ],
                    Filters = [
                        new FilterCondition { Field = "Employee.Status", Operator = FilterOperator.Equal, Value = "Active" },
                        new FilterCondition { Field = "LeaveType.Code", Operator = FilterOperator.Equal, Value = "ANNUAL" },
                        new FilterCondition { Field = "Year", Operator = FilterOperator.Equal, Value = "@CurrentYear" }
                    ],
                    SelectFields = ["Employee.EmployeeNumber", "Employee.FirstName", "Employee.LastName", "Entitlement", "Used", "Pending"],
                    Limit = 500
                }
            }
        ];
    }
}
