import { request } from '../request';
import type { LeaveRequest, LeaveBalance, LeaveType, PageRequest, PageResponse } from '@/types';

/** 假期管理API */
export const leaveApi = {
  /** 获取假期类型列表 */
  getTypes() {
    return request.get<LeaveType[]>('/leave/types');
  },
  
  /** 获取我的假期余额 */
  getMyBalances() {
    return request.get<LeaveBalance[]>('/leave/balances/my');
  },
  
  /** 获取员工假期余额 */
  getBalances(employeeId: string) {
    return request.get<LeaveBalance[]>(`/leave/balances/${employeeId}`);
  },
  
  /** 获取请假申请列表 */
  getRequests(params: PageRequest & { status?: string; employeeId?: string }) {
    return request.get<PageResponse<LeaveRequest>>('/leave/requests', { params });
  },
  
  /** 获取我的请假申请 */
  getMyRequests(params: PageRequest & { status?: string }) {
    return request.get<PageResponse<LeaveRequest>>('/leave/requests/my', { params });
  },
  
  /** 获取待审批的请假申请 */
  getPendingApprovals(params: PageRequest) {
    return request.get<PageResponse<LeaveRequest>>('/leave/requests/pending', { params });
  },
  
  /** 提交请假申请 */
  submitRequest(data: { leaveTypeId: string; startDate: string; endDate: string; reason: string }) {
    return request.post<LeaveRequest>('/leave/requests', data);
  },
  
  /** 取消请假申请 */
  cancelRequest(id: string) {
    return request.post(`/leave/requests/${id}/cancel`);
  },
  
  /** 审批请假申请 */
  approveRequest(id: string, data: { approved: boolean; comment?: string }) {
    return request.post(`/leave/requests/${id}/approve`, data);
  },
};
