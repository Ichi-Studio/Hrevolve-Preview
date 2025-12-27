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
  employeeCount?: number;
  children?: OrganizationUnit[];
}

/** 职位 */
export interface Position {
  id: string;
  code: string;
  name: string;
  level: number;
  description?: string;
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
  hireDate: string;
  terminationDate?: string;
  status: EmployeeStatus;
  departmentId: string;
  departmentName?: string;
  positionId: string;
  positionName?: string;
  managerId?: string;
  managerName?: string;
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

// ==================== 考勤管理 ====================

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

/** 考勤记录 */
export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName?: string;
  date: string;
  shiftId: string;
  shiftName?: string;
  checkInTime?: string;
  checkOutTime?: string;
  checkInLocation?: string;
  checkOutLocation?: string;
  status: AttendanceStatus;
  workHours?: number;
  overtimeHours?: number;
  remark?: string;
}

/** 考勤状态 */
export type AttendanceStatus = 'Normal' | 'Late' | 'EarlyLeave' | 'Absent' | 'Leave' | 'Holiday';

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
  requiresApproval: boolean;
  color: string;
}

/** 假期余额 */
export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  totalDays: number;
  usedDays: number;
  pendingDays: number;
  remainingDays: number;
  expiryDate?: string;
}

/** 请假申请 */
export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: ApprovalStatus;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
  approverComment?: string;
  createdAt: string;
}

/** 审批状态 */
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

// ==================== 薪酬管理 ====================

/** 薪资记录 */
export interface PayrollRecord {
  id: string;
  employeeId: string;
  employeeName?: string;
  periodId: string;
  periodName?: string;
  baseSalary: number;
  bonus: number;
  allowances: number;
  deductions: number;
  socialInsurance: number;
  housingFund: number;
  tax: number;
  netSalary: number;
  status: PayrollStatus;
  paidAt?: string;
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
  status: 'Open' | 'Closed' | 'Locked';
}

// ==================== 报销管理 ====================

/** 报销申请 */
export interface ExpenseRequest {
  id: string;
  employeeId: string;
  employeeName?: string;
  category: string;
  amount: number;
  currency: string;
  description: string;
  receiptUrls: string[];
  status: ApprovalStatus;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
  paidAt?: string;
  createdAt: string;
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
