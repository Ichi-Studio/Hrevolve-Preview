import { request } from '../request';
import type { Employee, JobHistory, PageRequest, PageResponse } from '@/types';

/** 员工管理API */
export const employeeApi = {
  /** 获取员工列表 */
  getList(params: PageRequest & { departmentId?: string; status?: string; keyword?: string }) {
    return request.get<PageResponse<Employee>>('/employees', { params });
  },
  
  /** 获取员工详情 */
  getById(id: string) {
    return request.get<Employee>(`/employees/${id}`);
  },
  
  /** 获取员工历史时点数据 */
  getAtDate(id: string, date: string) {
    return request.get<Employee>(`/employees/${id}/at-date`, { params: { date } });
  },
  
  /** 创建员工 */
  create(data: Partial<Employee>) {
    return request.post<Employee>('/employees', data);
  },
  
  /** 更新员工 */
  update(id: string, data: Partial<Employee>) {
    return request.put<Employee>(`/employees/${id}`, data);
  },
  
  /** 删除员工 */
  delete(id: string) {
    return request.delete(`/employees/${id}`);
  },
  
  /** 获取员工职位历史 */
  getJobHistory(id: string) {
    return request.get<JobHistory[]>(`/employees/${id}/job-history`);
  },
  
  /** 调岗/调薪 */
  transfer(id: string, data: { positionId: string; departmentId: string; salary: number; effectiveDate: string; reason: string }) {
    return request.post(`/employees/${id}/transfer`, data);
  },
  
  /** 员工离职 */
  terminate(id: string, data: { terminationDate: string; reason: string }) {
    return request.post(`/employees/${id}/terminate`, data);
  },
};
