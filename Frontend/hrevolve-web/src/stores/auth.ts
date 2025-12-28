import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { User, LoginRequest } from '@/types';
import { authApi } from '@/api';

export const useAuthStore = defineStore('auth', () => {
  // 状态
  const token = ref<string | null>(localStorage.getItem('token'));
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'));
  const user = ref<User | null>(null);
  const tenantId = ref<string | null>(localStorage.getItem('tenantId'));
  
  // 计算属性
  const isAuthenticated = computed(() => !!token.value);
  const userRoles = computed(() => user.value?.roles || []);
  const userPermissions = computed(() => user.value?.permissions || []);
  
  // 检查是否有指定权限
  const hasPermission = (permission: string): boolean => {
    return userPermissions.value.includes(permission) || userRoles.value.includes('Admin');
  };
  
  // 检查是否有指定角色
  const hasRole = (role: string): boolean => {
    return userRoles.value.includes(role);
  };
  
  // 登录
  const login = async (credentials: LoginRequest): Promise<void> => {
    const response = await authApi.login(credentials);
    
    token.value = response.data.accessToken;
    refreshToken.value = response.data.refreshToken;
    
    // 持久化存储
    localStorage.setItem('token', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    
    // 登录成功后获取用户信息
    await fetchUser();
  };
  
  // 获取当前用户信息
  const fetchUser = async (): Promise<void> => {
    if (!token.value) return;
    
    try {
      const response = await authApi.getCurrentUser();
      // 后端返回的字段名是驼峰式
      user.value = {
        id: response.data.id,
        username: response.data.username,
        email: response.data.email,
        displayName: response.data.displayName || response.data.username,
        avatar: response.data.avatar,
        tenantId: response.data.tenantId,
        employeeId: response.data.employeeId,
        permissions: response.data.permissions || [],
        roles: response.data.roles || [],
      };
      tenantId.value = response.data.tenantId;
      localStorage.setItem('tenantId', response.data.tenantId || '');
    } catch {
      // Token无效，清除登录状态
      logout();
    }
  };
  
  // 刷新Token
  const refreshAccessToken = async (): Promise<boolean> => {
    if (!refreshToken.value) return false;
    
    try {
      const response = await authApi.refreshToken(refreshToken.value);
      token.value = response.data.accessToken;
      refreshToken.value = response.data.refreshToken;
      
      localStorage.setItem('token', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      
      return true;
    } catch {
      logout();
      return false;
    }
  };
  
  // 退出登录
  const logout = (): void => {
    token.value = null;
    refreshToken.value = null;
    user.value = null;
    tenantId.value = null;
    
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tenantId');
  };
  
  return {
    // 状态
    token,
    refreshToken,
    user,
    tenantId,
    // 计算属性
    isAuthenticated,
    userRoles,
    userPermissions,
    // 方法
    hasPermission,
    hasRole,
    login,
    fetchUser,
    refreshAccessToken,
    logout,
  };
});
