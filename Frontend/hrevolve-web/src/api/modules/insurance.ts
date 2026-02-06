import { request } from '../request';
import type { 
  InsurancePlan, EmployeeInsurance, BenefitItem,
  PageRequest, PageResponse 
} from '@/types';

/** 保险福利 API */
export const insuranceApi = {
  // ==================== 保险方案 ====================
  
  /** 获取保险方案列表 */
  getInsurancePlans(params?: { type?: string; isActive?: boolean }) {
    return request.get<InsurancePlan[]>('/insurance/plans', { params });
  },
  
  /** 获取保险方案详情 */
  getInsurancePlan(id: string) {
    return request.get<InsurancePlan>(`/insurance/plans/${id}`);
  },
  
  /** 创建保险方案 */
  createInsurancePlan(data: Partial<InsurancePlan>) {
    return request.post<InsurancePlan>('/insurance/plans', data);
  },
  
  /** 更新保险方案 */
  updateInsurancePlan(id: string, data: Partial<InsurancePlan>) {
    return request.put<InsurancePlan>(`/insurance/plans/${id}`, data);
  },
  
  /** 删除保险方案 */
  deleteInsurancePlan(id: string) {
    return request.delete(`/insurance/plans/${id}`);
  },
  
  // ==================== 员工保险 ====================
  
  /** 获取员工保险列表 */
  getEmployeeInsurances(params: PageRequest & {
    employeeId?: string;
    insurancePlanId?: string;
    status?: string;
  }) {
    return request.get<PageResponse<EmployeeInsurance>>('/insurance/employees', { params });
  },
  
  /** 获取员工保险详情 */
  getEmployeeInsurance(id: string) {
    return request.get<EmployeeInsurance>(`/insurance/employees/${id}`);
  },
  
  /** 获取员工的所有保险 */
  getEmployeeAllInsurances(employeeId: string) {
    return request.get<EmployeeInsurance[]>(`/insurance/employees/by-employee/${employeeId}`);
  },
  
  /** 创建员工保险 */
  createEmployeeInsurance(data: Partial<EmployeeInsurance>) {
    return request.post<EmployeeInsurance>('/insurance/employees', data);
  },
  
  /** 批量创建员工保险 */
  batchCreateEmployeeInsurances(data: {
    employeeIds: string[];
    insurancePlanId: string;
    base: number;
    effectiveDate: string;
  }) {
    return request.post<EmployeeInsurance[]>('/insurance/employees/batch', data);
  },
  
  /** 更新员工保险 */
  updateEmployeeInsurance(id: string, data: Partial<EmployeeInsurance>) {
    return request.put<EmployeeInsurance>(`/insurance/employees/${id}`, data);
  },
  
  /** 终止员工保险 */
  terminateEmployeeInsurance(id: string, data?: { expiryDate?: string }) {
    return request.post(`/insurance/employees/${id}/terminate`, data ?? {});
  },
  
  /** 删除员工保险 */
  deleteEmployeeInsurance(id: string) {
    return request.delete(`/insurance/employees/${id}`);
  },
  
  // ==================== 福利项目 ====================
  
  /** 获取福利项目列表 */
  getBenefitItems(params?: { type?: string; isActive?: boolean }) {
    return request.get<BenefitItem[]>('/insurance/benefits', { params });
  },
  
  /** 获取福利项目详情 */
  getBenefitItem(id: string) {
    return request.get<BenefitItem>(`/insurance/benefits/${id}`);
  },
  
  /** 创建福利项目 */
  createBenefitItem(data: Partial<BenefitItem>) {
    return request.post<BenefitItem>('/insurance/benefits', data);
  },
  
  /** 更新福利项目 */
  updateBenefitItem(id: string, data: Partial<BenefitItem>) {
    return request.put<BenefitItem>(`/insurance/benefits/${id}`, data);
  },
  
  /** 删除福利项目 */
  deleteBenefitItem(id: string) {
    return request.delete(`/insurance/benefits/${id}`);
  },
  
  // ==================== 保险统计 ====================
  
  /** 获取保险概览统计 */
  getInsuranceOverview() {
    return request.get<{
      totalEmployees: number;
      coveredEmployees: number;
      coverageRate: number;
      totalCompanyCost: number;
      totalEmployeeCost: number;
      byPlan: { planName: string; employeeCount: number; totalCost: number }[];
      byDepartment: { departmentName: string; employeeCount: number; totalCost: number }[];
    }>('/insurance/overview');
  },
  
  /** 获取保险报表 */
  getInsuranceReport(params: { year: number; month: number }) {
    return request.get<{
      items: {
        employeeId: string;
        employeeName: string;
        employeeNo: string;
        departmentName: string;
        plans: {
          planName: string;
          base: number;
          employeeAmount: number;
          companyAmount: number;
        }[];
        totalEmployeeAmount: number;
        totalCompanyAmount: number;
      }[];
      summary: {
        totalEmployeeAmount: number;
        totalCompanyAmount: number;
        totalAmount: number;
      };
    }>('/insurance/report', { params });
  },
  
  // ==================== 福利项目（简化） ====================
  
  /** 获取福利列表 */
  getBenefits() {
    return request.get<{ id: string; name: string; type: string; amount: number; description?: string; isActive: boolean }[]>('/insurance/benefits-simple');
  },
  
  /** 创建福利 */
  createBenefit(data: { name: string; type: string; amount?: number; description?: string; isActive?: boolean }) {
    return request.post('/insurance/benefits-simple', data);
  },
  
  /** 更新福利 */
  updateBenefit(id: string, data: { name?: string; type?: string; amount?: number; description?: string; isActive?: boolean }) {
    return request.put(`/insurance/benefits-simple/${id}`, data);
  },
  
  /** 删除福利 */
  deleteBenefit(id: string) {
    return request.delete(`/insurance/benefits-simple/${id}`);
  },
  
  /** 获取保险统计 */
  getInsuranceStats() {
    return request.get<{
      totalPlans: number;
      enrolledEmployees: number;
      monthlyPremium: number;
      pendingClaims: number;
    }>('/insurance/stats');
  },
  
  /** 员工参保 */
  enrollEmployeeInsurance(data: { employeeId: string; planId: string; startDate: string }) {
    return request.post('/insurance/enroll', data);
  },
};
