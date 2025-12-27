import { request } from '../request';
import type { LoginRequest, LoginResponse, User } from '@/types';

/** 认证相关API */
export const authApi = {
  /** 用户登录 */
  login(data: LoginRequest) {
    return request.post<LoginResponse>('/auth/login', data);
  },
  
  /** 刷新Token */
  refreshToken(refreshToken: string) {
    return request.post<LoginResponse>('/auth/refresh', { refreshToken });
  },
  
  /** 获取当前用户信息 */
  getCurrentUser() {
    return request.get<User>('/auth/me');
  },
  
  /** 退出登录 */
  logout() {
    return request.post('/auth/logout');
  },
  
  /** 修改密码 */
  changePassword(data: { oldPassword: string; newPassword: string }) {
    return request.post('/auth/change-password', data);
  },
};
