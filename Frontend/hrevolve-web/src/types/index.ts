// ==================== 通用类型 ====================

/** API响应结构 */
export interface ApiResponse<T = unknown> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

/** 分页请求参数 */
export interface PageRequest {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDesc?: boolean;
}

/** 分页响应 */
export interface PageResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ==================== 用户与认证 ====================

/** 用户信息 */
export interface User {
  id: string;
  username: string;
  email: string;
  displayName: string;
  avatar?: string;
  roles: string[];
  permissions: string[];
  tenantId: string;
  employeeId?: string;
}

/** 登录请求 */
export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

/** 登录响应 */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: User;
}

// ==================== 租户/公司管理 ====================

/** 租户/公司信息 */
export interface Tenant {
  id: string;
  code: string;
  name: string;
  logo?: string;
  industry?: string;
  scale?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  status: 'Active' | 'Inactive' | 'Suspended';
  createdAt: string;
}

/** 成本中心 */
export interface CostCenter {
  id: string;
  code: string;
  name: string;
  parentId?: string;
  managerId?: string;
  managerName?: string;
  budget?: number;
  description?: string;
  isActive: boolean;
}

/** 标签 */
export interface Tag {
  id: string;
  name: string;
  color: string;
  category?: 'Employee' | 'Department' | 'Position' | 'Other' | string;
  description?: string;
  isActive?: boolean;
}

/** 打卡设备 */
export interface ClockDevice {
  id: string;
  code: string;
  name: string;
  type: 'Fingerprint' | 'Face' | 'Card' | 'GPS' | 'WiFi';
  location: string;
  ipAddress?: string;
  macAddress?: string;
  latitude?: number;
  longitude?: number;
  radius?: number;
  isActive: boolean;
  lastSyncAt?: string;
}

// ==================== 组织架构 ====================

/** 组织单元 */
export interface OrganizationUnit {
  id: string;
  code: string;
  name: string;
  parentId?: string;
  path: string;
  level: number;
  sortOrder: number;
  managerId?: string;
  managerName?: string;
  costCenterId?: string;
  costCenterName?: string;
  employeeCount?: number;
  children?: OrganizationUnit[];
}

/** 职位 */
export interface Position {
  id: string;
  code: string;
  name: string;
  level: number;
  category?: string;
  description?: string;
  responsibilities?: string;
  requirements?: string;
  salaryMin?: number;
  salaryMax?: number;
  isActive: boolean;
}

// ==================== 员工管理 ====================

/** 员工信息 */
export interface Employee {
  id: string;
  employeeNo: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string;
  avatar?: string;
  gender: 'Male' | 'Female' | 'Other';
  birthDate?: string;
  idNumber?: string;
  hireDate: string;
  terminationDate?: string;
  status: EmployeeStatus;
  employmentType: 'FullTime' | 'PartTime' | 'Contract' | 'Intern';
  departmentId: string;
  departmentName?: string;
  positionId: string;
  positionName?: string;
  managerId?: string;
  managerName?: string;
  workLocation?: string;
  tags?: Tag[];
}

/** 员工状态 */
export type EmployeeStatus = 'Active' | 'OnLeave' | 'Terminated' | 'Probation';

/** 员工职位历史 */
export interface JobHistory {
  id: string;
  employeeId: string;
  positionId: string;
  positionName: string;
  departmentId: string;
  departmentName: string;
  salary: number;
  effectiveStartDate: string;
  effectiveEndDate: string;
  changeReason?: string;
}

// ==================== 排班管理 ====================

/** 班次模板 */
export interface ShiftTemplate {
  id: string;
  code: string;
  name: string;
  color: string;
  startTime: string;
  endTime: string;
  breakStartTime?: string;
  breakEndTime?: string;
  breakMinutes: number;
  workHours: number;
  isFlexible: boolean;
  flexibleMinutes?: number;
  isOvernight: boolean;
  isActive: boolean;
}

/** 班次 */
export interface Shift {
  id: string;
  name: string;
  code: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  isFlexible: boolean;
  flexibleMinutes?: number;
}

/** 排班记录 */
export interface Schedule {
  id: string;
  employeeId: string;
  employeeName?: string;
  departmentId?: string;
  departmentName?: string;
  date: string;
  shiftTemplateId: string;
  shiftTemplateName?: string;
  shiftColor?: string;
  startTime: string;
  endTime: string;
  status: 'Scheduled' | 'Confirmed' | 'Completed' | 'Cancelled';
  remark?: string;
}

/** 排班项目 */
export interface ScheduleProject {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  departmentIds: string[];
  status: 'Draft' | 'Published' | 'Archived';
  createdBy: string;
  createdAt: string;
}

// ==================== 考勤管理 ====================

/** 考勤记录 */
export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeNo?: string;
  departmentName?: string;
  date: string;
  shiftId?: string;
  shiftName?: string;
  scheduledStart?: string;
  scheduledEnd?: string;
  checkInTime?: string;
  checkOutTime?: string;
  checkInLocation?: string;
  checkOutLocation?: string;
  checkInDeviceId?: string;
  checkOutDeviceId?: string;
  status: AttendanceStatus;
  workHours?: number;
  overtimeHours?: number;
  lateMinutes?: number;
  earlyLeaveMinutes?: number;
  remark?: string;
}

/** 考勤状态 */
export type AttendanceStatus = 'Normal' | 'Late' | 'EarlyLeave' | 'Absent' | 'Leave' | 'Holiday' | 'Rest';

/** 考勤汇总 */
export interface AttendanceSummary {
  employeeId: string;
  employeeName: string;
  departmentName: string;
  period: string;
  workDays: number;
  actualDays: number;
  lateTimes: number;
  earlyLeaveTimes: number;
  absentDays: number;
  leaveDays: number;
  overtimeHours: number;
  totalWorkHours: number;
}

/** 考勤设置 */
export interface AttendanceSettings {
  id: string;
  lateGraceMinutes: number;
  earlyLeaveGraceMinutes: number;
  overtimeMinMinutes: number;
  requirePhotoCheckIn: boolean;
  requireLocationCheckIn: boolean;
  allowRemoteCheckIn: boolean;
  workdayStart: string;
  workdayEnd: string;
}

/** 工时单 */
export interface Timesheet {
  id: string;
  employeeId: string;
  employeeName?: string;
  periodStart: string;
  periodEnd: string;
  regularHours: number;
  overtimeHours: number;
  leaveHours: number;
  totalHours: number;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  submittedAt?: string;
  approvedBy?: string;
  approvedAt?: string;
}

// ==================== 假期管理 ====================

/** 假期类型 */
export interface LeaveType {
  id: string;
  code: string;
  name: string;
  description?: string;
  isPaid: boolean;
  defaultDays: number;
  maxCarryOver: number;
  carryOverExpiry?: number;
  requiresApproval: boolean;
  minAdvanceDays?: number;
  maxConsecutiveDays?: number;
  allowHalfDay: boolean;
  color: string;
  isActive: boolean;
}

/** 假期规则 */
export interface LeaveRule {
  id: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  name: string;
  description?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  conditions: LeaveRuleCondition[];
  allocation: number;
  accrualType: 'Annual' | 'Monthly' | 'ByTenure';
  isActive: boolean;
}

/** 假期规则条件 */
export interface LeaveRuleCondition {
  field: 'tenure' | 'position' | 'department' | 'employmentType';
  operator: 'eq' | 'gt' | 'gte' | 'lt' | 'lte' | 'in';
  value: string | number | string[];
}

/** 假期余额 */
export interface LeaveBalance {
  id: string;
  employeeId: string;
  employeeName?: string;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeColor?: string;
  year: number;
  totalDays: number;
  usedDays: number;
  pendingDays: number;
  remainingDays: number;
  carryOverDays: number;
  expiryDate?: string;
}

/** 请假申请 */
export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeNo?: string;
  departmentName?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  leaveTypeColor?: string;
  startDate: string;
  endDate: string;
  startHalf?: 'AM' | 'PM';
  endHalf?: 'AM' | 'PM';
  days: number;
  reason: string;
  attachments?: string[];
  status: ApprovalStatus;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
  approverComment?: string;
  createdAt: string;
}

/** 审批状态 */
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

// ==================== 报销管理 ====================

/** 报销类型 */
export interface ExpenseType {
  id: string;
  code: string;
  name: string;
  category: 'Travel' | 'Meal' | 'Transportation' | 'Office' | 'Training' | 'Other';
  maxAmount?: number;
  requiresReceipt: boolean;
  requiresApproval: boolean;
  description?: string;
  isActive: boolean;
}

/** 报销申请 */
export interface ExpenseRequest {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeNo?: string;
  departmentName?: string;
  expenseTypeId: string;
  expenseTypeName?: string;
  category: string;
  amount: number;
  currency: string;
  expenseDate: string;
  description: string;
  receiptUrls: string[];
  status: ApprovalStatus;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
  approverComment?: string;
  paidAt?: string;
  paymentMethod?: string;
  createdAt: string;
}

/** 报销明细 */
export interface ExpenseItem {
  id: string;
  expenseRequestId: string;
  expenseTypeId: string;
  expenseTypeName?: string;
  amount: number;
  description: string;
  receiptUrl?: string;
  date: string;
}

// ==================== 薪酬管理 ====================

/** 薪酬项 */
export interface SalaryItem {
  id: string;
  code: string;
  name: string;
  type: 'Fixed' | 'Variable' | 'Deduction' | 'Benefit';
  category: 'Base' | 'Allowance' | 'Bonus' | 'Commission' | 'Insurance' | 'Tax' | 'Other';
  calculationType: 'Fixed' | 'Percentage' | 'Formula';
  formula?: string;
  isTaxable: boolean;
  isActive: boolean;
  sortOrder: number;
}

/** 员工薪酬档案 */
export interface SalaryProfile {
  id: string;
  employeeId: string;
  employeeName?: string;
  baseSalary: number;
  currency: string;
  payFrequency: 'Monthly' | 'BiWeekly' | 'Weekly';
  bankName?: string;
  bankAccount?: string;
  effectiveDate: string;
  items: SalaryProfileItem[];
}

/** 薪酬档案明细 */
export interface SalaryProfileItem {
  salaryItemId: string;
  salaryItemName: string;
  amount: number;
}

/** 薪资记录 */
export interface PayrollRecord {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeNo?: string;
  departmentName?: string;
  periodId: string;
  periodName?: string;
  baseSalary: number;
  bonus: number;
  allowances: number;
  overtime: number;
  deductions: number;
  socialInsurance: number;
  housingFund: number;
  tax: number;
  grossSalary: number;
  netSalary: number;
  status: PayrollStatus;
  paidAt?: string;
  items: PayrollRecordItem[];
}

/** 薪资记录明细 */
export interface PayrollRecordItem {
  salaryItemId: string;
  salaryItemName: string;
  type: string;
  amount: number;
}

/** 薪资状态 */
export type PayrollStatus = 'Draft' | 'Calculated' | 'Approved' | 'Paid';

/** 薪资周期 */
export interface PayrollPeriod {
  id: string;
  name: string;
  year: number;
  month: number;
  startDate: string;
  endDate: string;
  payDate: string;
  status: 'Open' | 'Calculating' | 'Calculated' | 'Approved' | 'Paid' | 'Closed';
  employeeCount?: number;
  totalAmount?: number;
}

// ==================== 保险福利 ====================

/** 保险方案 */
export interface InsurancePlan {
  id: string;
  code?: string;
  name: string;
  type: 'health' | 'life' | 'accident' | 'pension' | 'Social' | 'Commercial' | 'Supplementary';
  provider?: string;
  coverage?: number;
  premium?: number;
  description?: string;
  employeeRate?: number;
  companyRate?: number;
  baseMin?: number;
  baseMax?: number;
  effectiveDate?: string;
  expiryDate?: string;
  isActive: boolean;
}

/** 员工保险档案 */
export interface EmployeeInsurance {
  id: string;
  employeeId: string;
  employeeName?: string;
  insurancePlanId?: string;
  insurancePlanName?: string;
  planId?: string;
  planName?: string;
  startDate?: string;
  endDate?: string;
  premium?: number;
  base?: number;
  employeeAmount?: number;
  companyAmount?: number;
  effectiveDate?: string;
  expiryDate?: string;
  status: 'active' | 'terminated' | 'Active' | 'Suspended' | 'Terminated';
}

/** 福利项目 */
export interface BenefitItem {
  id: string;
  code: string;
  name: string;
  type: 'Allowance' | 'Subsidy' | 'Reimbursement' | 'Other';
  amount?: number;
  frequency: 'Monthly' | 'Quarterly' | 'Annual' | 'OneTime';
  eligibility?: string;
  description?: string;
  isActive: boolean;
}

// ==================== 报税管理 ====================

/** 税务档案 */
export interface TaxProfile {
  id: string;
  employeeId: string;
  employeeName?: string;
  taxId: string;
  taxType: 'Resident' | 'NonResident';
  specialDeductions: TaxDeduction[];
  effectiveYear: number;
}

/** 税务扣除项 */
export interface TaxDeduction {
  type: 'ChildEducation' | 'ContinuingEducation' | 'HousingLoan' | 'HousingRent' | 'ElderlySupport' | 'ChildCare';
  amount: number;
  startDate: string;
  endDate?: string;
}

/** 税务计算记录 */
export interface TaxRecord {
  id: string;
  employeeId: string;
  employeeName?: string;
  periodId: string;
  periodName?: string;
  grossIncome: number;
  taxableIncome: number;
  deductions: number;
  taxAmount: number;
  cumulativeIncome: number;
  cumulativeTax: number;
  status: 'Calculated' | 'Declared' | 'Paid';
}

// ==================== 系统设置 ====================

/** 系统配置 */
export interface SystemConfig {
  key: string;
  value: string;
  category: string;
  description?: string;
}

/** 审批流程 */
export interface ApprovalFlow {
  id: string;
  name: string;
  type: 'Leave' | 'Expense' | 'Overtime' | 'Transfer';
  steps: ApprovalStep[];
  isActive: boolean;
}

/** 审批步骤 */
export interface ApprovalStep {
  order: number;
  approverType: 'DirectManager' | 'DepartmentHead' | 'HR' | 'SpecificUser' | 'Role';
  approverId?: string;
  approverRole?: string;
  isRequired: boolean;
}

// ==================== AI助手 ====================

/** 聊天消息 */
export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  isLoading?: boolean;
}

/** 聊天请求 */
export interface ChatRequest {
  message: string;
}

/** 聊天响应 */
export interface ChatResponse {
  reply: string;
}

// ==================== 统计报表 ====================

/** 仪表盘统计 */
export interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  newHiresThisMonth: number;
  terminationsThisMonth: number;
  attendanceRate: number;
  leaveRate: number;
  pendingApprovals: number;
  upcomingBirthdays: number;
}

/** 部门统计 */
export interface DepartmentStats {
  departmentId: string;
  departmentName: string;
  employeeCount: number;
  attendanceRate: number;
  avgWorkHours: number;
  overtimeHours: number;
}

/** 考勤统计 */
export interface AttendanceStats {
  date: string;
  totalEmployees: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  leaveCount: number;
  attendanceRate: number;
}

// ==================== 补充类型 ====================

/** 角色 */
export interface Role {
  id: string;
  code: string;
  name: string;
  description?: string;
  permissions: string[];
  isSystem: boolean;
  isActive: boolean;
}

/** 审计日志 */
export interface AuditLog {
  id: string;
  userId: string;
  userName?: string;
  action: 'create' | 'update' | 'delete' | 'login' | 'logout' | 'export';
  entityType: string;
  entityId?: string;
  description: string;
  changes?: Record<string, { old: unknown; new: unknown }>;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
}

/** 福利项目（简化版） */
export interface Benefit {
  id: string;
  name: string;
  type: string;
  amount: number;
  description?: string;
  isActive: boolean;
}

/** 用户扩展（用于用户管理） */
export interface UserExtended extends User {
  phone?: string;
  isActive: boolean;
  lastLoginTime?: string;
  createdAt?: string;
}
