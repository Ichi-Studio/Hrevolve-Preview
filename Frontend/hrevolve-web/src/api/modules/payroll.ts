import { request } from '../request';
import type { PayrollRecord, PayrollPeriod, PageRequest, PageResponse } from '@/types';

/** 薪酬管理API */
export const payrollApi = {
  /** 获取薪资周期列表 */
  getPeriods(year?: number) {
    return request.get<PayrollPeriod[]>('/payroll/periods', { params: { year } });
  },
  
  /** 获取我的薪资记录 */
  getMyRecords(params: PageRequest & { year?: number }) {
    return request.get<PageResponse<PayrollRecord>>('/payroll/records/my', { params });
  },
  
  /** 获取薪资记录列表（管理员） */
  getRecords(params: PageRequest & { periodId?: string; employeeId?: string; departmentId?: string }) {
    return request.get<PageResponse<PayrollRecord>>('/payroll/records', { params });
  },
  
  /** 获取薪资详情 */
  getRecordById(id: string) {
    return request.get<PayrollRecord>(`/payroll/records/${id}`);
  },
  
  /** 计算薪资（管理员） */
  calculate(periodId: string, employeeIds?: string[]) {
    return request.post('/payroll/calculate', { periodId, employeeIds });
  },
  
  /** 审批薪资（管理员） */
  approve(periodId: string) {
    return request.post(`/payroll/periods/${periodId}/approve`);
  },
};
