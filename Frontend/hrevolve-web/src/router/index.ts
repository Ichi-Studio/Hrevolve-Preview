import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

// 路由配置
const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { requiresAuth: false, title: '登录' },
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: '工作台', icon: 'HomeFilled' },
      },
      // 员工自助
      {
        path: 'self-service',
        name: 'SelfService',
        meta: { title: '员工自助', icon: 'User' },
        children: [
          {
            path: 'profile',
            name: 'MyProfile',
            component: () => import('@/views/self-service/ProfileView.vue'),
            meta: { title: '个人信息' },
          },
          {
            path: 'attendance',
            name: 'MyAttendance',
            component: () => import('@/views/self-service/AttendanceView.vue'),
            meta: { title: '我的考勤' },
          },
          {
            path: 'leave',
            name: 'MyLeave',
            component: () => import('@/views/self-service/LeaveView.vue'),
            meta: { title: '我的假期' },
          },
          {
            path: 'payroll',
            name: 'MyPayroll',
            component: () => import('@/views/self-service/PayrollView.vue'),
            meta: { title: '我的薪资' },
          },
        ],
      },
      // AI助手
      {
        path: 'assistant',
        name: 'Assistant',
        component: () => import('@/views/assistant/AssistantView.vue'),
        meta: { title: 'AI助手', icon: 'ChatDotRound' },
      },
      // 组织管理
      {
        path: 'organization',
        name: 'Organization',
        meta: { title: '组织管理', icon: 'OfficeBuilding', permission: 'organization:read' },
        children: [
          {
            path: 'structure',
            name: 'OrgStructure',
            component: () => import('@/views/organization/StructureView.vue'),
            meta: { title: '组织架构' },
          },
          {
            path: 'positions',
            name: 'Positions',
            component: () => import('@/views/organization/PositionsView.vue'),
            meta: { title: '职位管理' },
          },
        ],
      },
      // 员工管理
      {
        path: 'employees',
        name: 'Employees',
        meta: { title: '员工管理', icon: 'UserFilled', permission: 'employee:read' },
        children: [
          {
            path: '',
            name: 'EmployeeList',
            component: () => import('@/views/employees/EmployeeListView.vue'),
            meta: { title: '员工列表' },
          },
          {
            path: ':id',
            name: 'EmployeeDetail',
            component: () => import('@/views/employees/EmployeeDetailView.vue'),
            meta: { title: '员工详情', hidden: true },
          },
        ],
      },
      // 考勤管理
      {
        path: 'attendance',
        name: 'Attendance',
        meta: { title: '考勤管理', icon: 'Clock', permission: 'attendance:read' },
        children: [
          {
            path: 'records',
            name: 'AttendanceRecords',
            component: () => import('@/views/attendance/RecordsView.vue'),
            meta: { title: '考勤记录' },
          },
          {
            path: 'shifts',
            name: 'Shifts',
            component: () => import('@/views/attendance/ShiftsView.vue'),
            meta: { title: '班次管理' },
          },
        ],
      },
      // 假期管理
      {
        path: 'leave',
        name: 'Leave',
        meta: { title: '假期管理', icon: 'Calendar', permission: 'leave:read' },
        children: [
          {
            path: 'requests',
            name: 'LeaveRequests',
            component: () => import('@/views/leave/RequestsView.vue'),
            meta: { title: '请假申请' },
          },
          {
            path: 'approvals',
            name: 'LeaveApprovals',
            component: () => import('@/views/leave/ApprovalsView.vue'),
            meta: { title: '假期审批' },
          },
          {
            path: 'types',
            name: 'LeaveTypes',
            component: () => import('@/views/leave/TypesView.vue'),
            meta: { title: '假期类型' },
          },
        ],
      },
      // 薪酬管理
      {
        path: 'payroll',
        name: 'Payroll',
        meta: { title: '薪酬管理', icon: 'Money', permission: 'payroll:read' },
        children: [
          {
            path: 'records',
            name: 'PayrollRecords',
            component: () => import('@/views/payroll/RecordsView.vue'),
            meta: { title: '薪资记录' },
          },
          {
            path: 'periods',
            name: 'PayrollPeriods',
            component: () => import('@/views/payroll/PeriodsView.vue'),
            meta: { title: '薪资周期' },
          },
        ],
      },
    ],
  },
  // 404
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/error/NotFoundView.vue'),
    meta: { title: '页面不存在' },
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

// 路由守卫
router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore();
  
  // 设置页面标题
  document.title = `${to.meta.title || 'Hrevolve'} - Hrevolve HRM`;
  
  // 不需要认证的页面
  if (to.meta.requiresAuth === false) {
    if (authStore.isAuthenticated && to.name === 'Login') {
      next({ name: 'Dashboard' });
    } else {
      next();
    }
    return;
  }
  
  // 需要认证的页面
  if (!authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } });
    return;
  }
  
  // 获取用户信息
  if (!authStore.user) {
    await authStore.fetchUser();
  }
  
  // 检查权限
  const requiredPermission = to.meta.permission as string | undefined;
  if (requiredPermission && !authStore.hasPermission(requiredPermission)) {
    next({ name: 'Dashboard' });
    return;
  }
  
  next();
});

export default router;
