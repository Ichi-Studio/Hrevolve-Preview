import { request } from '../request';
import type { AttendanceRecord, Shift, PageRequest, PageResponse } from '@/types';

/** 考勤管理API */
export const attendanceApi = {
  /** 获取班次列表 */
  getShifts() {
    return request.get<Shift[]>('/attendance/shifts');
  },
  
  /** 签到 */
  checkIn(data: { location?: string; remark?: string }) {
    return request.post<AttendanceRecord>('/attendance/check-in', data);
  },
  
  /** 签退 */
  checkOut(data: { location?: string; remark?: string }) {
    return request.post<AttendanceRecord>('/attendance/check-out', data);
  },
  
  /** 获取今日考勤状态 */
  getTodayStatus() {
    return request.get<AttendanceRecord | null>('/attendance/today');
  },
  
  /** 获取我的考勤记录 */
  getMyRecords(params: PageRequest & { startDate?: string; endDate?: string }) {
    return request.get<PageResponse<AttendanceRecord>>('/attendance/records/my', { params });
  },
  
  /** 获取考勤记录列表（管理员） */
  getRecords(params: PageRequest & { employeeId?: string; departmentId?: string; startDate?: string; endDate?: string; status?: string }) {
    return request.get<PageResponse<AttendanceRecord>>('/attendance/records', { params });
  },
  
  /** 补卡申请 */
  applyCorrection(data: { date: string; checkInTime?: string; checkOutTime?: string; reason: string }) {
    return request.post('/attendance/correction', data);
  },
  
  /** 获取月度考勤统计 */
  getMonthlyStats(year: number, month: number) {
    return request.get<{
      workDays: number;
      attendedDays: number;
      lateDays: number;
      earlyLeaveDays: number;
      absentDays: number;
      leaveDays: number;
      overtimeHours: number;
    }>('/attendance/stats/monthly', { params: { year, month } });
  },
};
