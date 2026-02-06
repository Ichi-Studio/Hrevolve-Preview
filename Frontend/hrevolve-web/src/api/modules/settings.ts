import { request } from '../request';
import type { 
  SystemConfig, ApprovalFlow, UserExtended,
  PageRequest, PageResponse 
} from '@/types';

/** 系统设置 API */
export const settingsApi = {
  // ==================== 系统配置 ====================
  
  /** 获取系统配置 */
  getConfigs(category?: string) {
    return request.get<SystemConfig[]>('/settings/configs', { params: { category } });
  },
  
  /** 获取单个配置 */
  getConfig(key: string) {
    return request.get<SystemConfig>(`/settings/configs/${key}`);
  },
  
  /** 更新配置 */
  updateConfig(key: string, value: string) {
    return request.put(`/settings/configs/${key}`, { value });
  },
  
  /** 批量更新配置 */
  batchUpdateConfigs(configs: { key: string; value: string }[]) {
    return request.put('/settings/configs/batch', { configs });
  },
  
  // ==================== 用户管理 ====================
  
  /** 获取用户列表 */
  getUsers(params: PageRequest & { keyword?: string; role?: string; status?: string }) {
    return request.get<PageResponse<UserExtended>>('/settings/users', { params });
  },
  
  /** 获取用户详情 */
  getUser(id: string) {
    return request.get<UserExtended>(`/settings/users/${id}`);
  },
  
  /** 创建用户 */
  createUser(data: {
    username: string;
    email?: string;
    password?: string;
    displayName: string;
    roles: string[];
    employeeId?: string;
    phone?: string;
    isActive?: boolean;
  }) {
    return request.post<UserExtended>('/settings/users', data);
  },
  
  /** 更新用户 */
  updateUser(id: string, data: Partial<UserExtended>) {
    return request.put<UserExtended>(`/settings/users/${id}`, data);
  },
  
  /** 重置用户密码 */
  resetUserPassword(id: string, newPassword?: string) {
    return request.post(`/settings/users/${id}/reset-password`, { newPassword });
  },
  
  /** 禁用用户 */
  disableUser(id: string) {
    return request.post(`/settings/users/${id}/disable`);
  },
  
  /** 启用用户 */
  enableUser(id: string) {
    return request.post(`/settings/users/${id}/enable`);
  },
  
  /** 删除用户 */
  deleteUser(id: string) {
    return request.delete(`/settings/users/${id}`);
  },
  
  // ==================== 角色管理 ====================
  
  /** 获取角色列表 */
  getRoles() {
    return request.get<{ id: string; code: string; name: string; description: string; permissions: string[]; isSystem: boolean; isActive: boolean }[]>(
      '/settings/roles'
    );
  },
  
  /** 创建角色 */
  createRole(data: { code: string; name: string; description?: string; permissions?: string[]; isActive?: boolean }) {
    return request.post('/settings/roles', data);
  },
  
  /** 更新角色 */
  updateRole(id: string, data: { name?: string; description?: string; isActive?: boolean }) {
    return request.put(`/settings/roles/${id}`, data);
  },
  
  /** 更新角色权限 */
  updateRolePermissions(id: string, permissions: string[]) {
    return request.put(`/settings/roles/${id}/permissions`, { permissions });
  },
  
  /** 删除角色 */
  deleteRole(id: string) {
    return request.delete(`/settings/roles/${id}`);
  },
  
  /** 获取所有权限 */
  getPermissions() {
    return request.get<{ code: string; name: string; category: string }[]>('/settings/permissions');
  },
  
  // ==================== 审批流程 ====================
  
  /** 获取审批流程列表 */
  getApprovalFlows(type?: string) {
    return request.get<ApprovalFlow[]>('/settings/approval-flows', { params: { type } });
  },
  
  /** 获取审批流程详情 */
  getApprovalFlow(id: string) {
    return request.get<ApprovalFlow>(`/settings/approval-flows/${id}`);
  },
  
  /** 创建审批流程 */
  createApprovalFlow(data: Partial<ApprovalFlow>) {
    return request.post<ApprovalFlow>('/settings/approval-flows', data);
  },
  
  /** 更新审批流程 */
  updateApprovalFlow(id: string, data: Partial<ApprovalFlow>) {
    return request.put<ApprovalFlow>(`/settings/approval-flows/${id}`, data);
  },
  
  /** 删除审批流程 */
  deleteApprovalFlow(id: string) {
    return request.delete(`/settings/approval-flows/${id}`);
  },
  
  // ==================== 操作日志 ====================
  
  /** 获取操作日志 */
  getAuditLogs(params: PageRequest & {
    userId?: string;
    action?: string;
    startDate?: string;
    endDate?: string;
  }) {
    return request.get<PageResponse<{
      id: string;
      userId: string;
      userName: string;
      action: string;
      resource: string;
      resourceId: string;
      details: string;
      ipAddress: string;
      userAgent: string;
      createdAt: string;
    }>>('/settings/audit-logs', { params });
  },
  
  // ==================== 数据备份 ====================
  
  /** 获取备份列表 */
  getBackups() {
    return request.get<{
      id: string;
      fileName: string;
      size: number;
      createdAt: string;
      status: string;
    }[]>('/settings/backups');
  },
  
  /** 创建备份 */
  createBackup() {
    return request.post('/settings/backups');
  },
  
  /** 下载备份 */
  downloadBackup(id: string) {
    return request.get(`/settings/backups/${id}/download`, { responseType: 'blob' });
  },
  
  /** 删除备份 */
  deleteBackup(id: string) {
    return request.delete(`/settings/backups/${id}`);
  },
  
  // ==================== 系统配置（扩展） ====================
  
  /** 获取系统配置（全部） */
  getSystemConfigs() {
    return request.get<{
      general: Record<string, unknown>;
      security: Record<string, unknown>;
      notification: Record<string, unknown>;
    }>('/settings/system-configs');
  },
  
  /** 更新系统配置 */
  updateSystemConfigs(data: {
    general?: Record<string, unknown>;
    security?: Record<string, unknown>;
    notification?: Record<string, unknown>;
  }) {
    return request.put('/settings/system-configs', data);
  },
  
  /** 导出审计日志 */
  exportAuditLogs(params: { action?: string; startDate?: string; endDate?: string }) {
    return request.get('/settings/audit-logs/export', { params, responseType: 'blob' });
  },
};
