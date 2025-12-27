import { request } from '../request';
import type { OrganizationUnit, Position } from '@/types';

/** 组织架构API */
export const organizationApi = {
  /** 获取组织架构树 */
  getTree() {
    return request.get<OrganizationUnit[]>('/organizations/tree');
  },
  
  /** 获取组织单元详情 */
  getById(id: string) {
    return request.get<OrganizationUnit>(`/organizations/${id}`);
  },
  
  /** 创建组织单元 */
  create(data: Partial<OrganizationUnit>) {
    return request.post<OrganizationUnit>('/organizations', data);
  },
  
  /** 更新组织单元 */
  update(id: string, data: Partial<OrganizationUnit>) {
    return request.put<OrganizationUnit>(`/organizations/${id}`, data);
  },
  
  /** 删除组织单元 */
  delete(id: string) {
    return request.delete(`/organizations/${id}`);
  },
  
  /** 获取职位列表 */
  getPositions() {
    return request.get<Position[]>('/organizations/positions');
  },
  
  /** 创建职位 */
  createPosition(data: Partial<Position>) {
    return request.post<Position>('/organizations/positions', data);
  },
  
  /** 更新职位 */
  updatePosition(id: string, data: Partial<Position>) {
    return request.put<Position>(`/organizations/positions/${id}`, data);
  },
};
