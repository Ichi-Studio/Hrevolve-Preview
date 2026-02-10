import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import MainLayout from '@/layouts/MainLayout.vue';
import DashboardView from '@/views/dashboard/DashboardView.vue';

// 路由配置
const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { requiresAuth: false, titleKey: 'auth.login' },
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: DashboardView,
        meta: { titleKey: 'menu.dashboard', icon: 'HomeFilled' },
      },
      // 员工自助
      {
        path: 'self-service',
        name: 'SelfService',
        redirect: '/self-service/profile',
        meta: { titleKey: 'menu.selfService', icon: 'User' },
        children: [
          {
            path: 'profile',
            name: 'MyProfile',
            component: () => import('@/views/self-service/ProfileView.vue'),
            meta: { titleKey: 'menu.profile' },
          },
          {
            path: 'attendance',
            name: 'MyAttendance',
            component: () => import('@/views/self-service/AttendanceView.vue'),
            meta: { titleKey: 'menu.myAttendance' },
          },
          {
            path: 'leave',
            name: 'MyLeave',
            component: () => import('@/views/self-service/LeaveView.vue'),
            meta: { titleKey: 'menu.myLeave' },
          },
          {
            path: 'payroll',
            name: 'MyPayroll',
            component: () => import('@/views/self-service/PayrollView.vue'),
            meta: { titleKey: 'menu.myPayroll' },
          },
        ],
      },
      // AI助手
      {
        path: 'assistant',
        name: 'Assistant',
        component: () => import('@/views/assistant/AssistantView.vue'),
        meta: { titleKey: 'menu.assistant', icon: 'ChatDotRound' },
      },
      // 组织管理
      {
        path: 'organization',
        name: 'Organization',
        redirect: '/organization/structure',
        meta: { titleKey: 'menu.organization', icon: 'OfficeBuilding', permission: 'organization:read' },
        children: [
          {
            path: 'structure',
            name: 'OrgStructure',
            component: () => import('@/views/organization/StructureView.vue'),
            meta: { titleKey: 'menu.orgStructure' },
          },
          {
            path: 'positions',
            name: 'Positions',
            component: () => import('@/views/organization/PositionsView.vue'),
            meta: { titleKey: 'menu.positions' },
          },
        ],
      },
      // 员工管理
      {
        path: 'employees',
        name: 'Employees',
        redirect: '/employees',
        meta: { titleKey: 'menu.employees', icon: 'UserFilled', permission: 'employee:read' },
        children: [
          {
            path: '',
            name: 'EmployeeList',
            component: () => import('@/views/employees/EmployeeListView.vue'),
            meta: { titleKey: 'menu.employeeList' },
          },
          {
            path: ':id',
            name: 'EmployeeDetail',
            component: () => import('@/views/employees/EmployeeDetailView.vue'),
            meta: { titleKey: 'employee.detail', hidden: true },
          },
        ],
      },
      // 考勤管理
      {
        path: 'attendance',
        name: 'Attendance',
        redirect: '/attendance/records',
        meta: { titleKey: 'menu.attendance', icon: 'Clock', permission: 'attendance:read' },
        children: [
          {
            path: 'records',
            name: 'AttendanceRecords',
            component: () => import('@/views/attendance/RecordsView.vue'),
            meta: { titleKey: 'menu.attendanceRecords' },
          },
          {
            path: 'shifts',
            name: 'Shifts',
            component: () => import('@/views/attendance/ShiftsView.vue'),
            meta: { titleKey: 'menu.shifts' },
          },
        ],
      },
      // 假期管理
      {
        path: 'leave',
        name: 'Leave',
        redirect: '/leave/requests',
        meta: { titleKey: 'menu.leave', icon: 'Calendar', permission: 'leave:read' },
        children: [
          {
            path: 'requests',
            name: 'LeaveRequests',
            component: () => import('@/views/leave/RequestsView.vue'),
            meta: { titleKey: 'menu.leaveRequests' },
          },
          {
            path: 'approvals',
            name: 'LeaveApprovals',
            component: () => import('@/views/leave/ApprovalsView.vue'),
            meta: { titleKey: 'menu.leaveApprovals' },
          },
          {
            path: 'types',
            name: 'LeaveTypes',
            component: () => import('@/views/leave/TypesView.vue'),
            meta: { titleKey: 'menu.leaveTypes' },
          },
        ],
      },
      // 薪酬管理
      {
        path: 'payroll',
        name: 'Payroll',
        redirect: '/payroll/records',
        meta: { titleKey: 'menu.payroll', icon: 'Money', permission: 'payroll:read' },
        children: [
          {
            path: 'records',
            name: 'PayrollRecords',
            component: () => import('@/views/payroll/RecordsView.vue'),
            meta: { titleKey: 'menu.payrollRecords' },
          },
          {
            path: 'periods',
            name: 'PayrollPeriods',
            component: () => import('@/views/payroll/PeriodsView.vue'),
            meta: { titleKey: 'menu.payrollPeriods' },
          },
        ],
      },
      // 公司设置
      {
        path: 'company',
        name: 'Company',
        redirect: '/company/tenant',
        meta: { titleKey: 'menu.company', icon: 'Setting', permission: 'settings:read' },
        children: [
          {
            path: 'tenant',
            name: 'CompanyTenant',
            component: () => import('@/views/company/TenantView.vue'),
            meta: { titleKey: 'menu.companyInfo' },
          },
          {
            path: 'cost-centers',
            name: 'CostCenters',
            component: () => import('@/views/company/CostCentersView.vue'),
            meta: { titleKey: 'menu.costCenters' },
          },
          {
            path: 'tags',
            name: 'Tags',
            component: () => import('@/views/company/TagsView.vue'),
            meta: { titleKey: 'menu.tags' },
          },
          {
            path: 'clock-devices',
            name: 'ClockDevices',
            component: () => import('@/views/company/ClockDevicesView.vue'),
            meta: { titleKey: 'menu.clockDevices' },
          },
          {
            path: 'users',
            name: 'CompanyUsers',
            component: () => import('@/views/company/UsersView.vue'),
            meta: { titleKey: 'menu.users' },
          },
        ],
      },
      // 排班管理
      {
        path: 'schedule',
        name: 'Schedule',
        redirect: '/schedule/overview',
        meta: { titleKey: 'menu.schedule', icon: 'Calendar', permission: 'attendance:read' },
        children: [
          {
            path: 'overview',
            name: 'ScheduleOverview',
            component: () => import('@/views/schedule/OverviewView.vue'),
            meta: { titleKey: 'menu.scheduleOverview' },
          },
          {
            path: 'table',
            name: 'ScheduleTable',
            component: () => import('@/views/schedule/ScheduleTableView.vue'),
            meta: { titleKey: 'menu.scheduleTable' },
          },
          {
            path: 'templates',
            name: 'ShiftTemplates',
            component: () => import('@/views/schedule/ShiftTemplatesView.vue'),
            meta: { titleKey: 'menu.shiftTemplates' },
          },
          {
            path: 'calendar',
            name: 'ScheduleCalendar',
            component: () => import('@/views/schedule/CalendarView.vue'),
            meta: { titleKey: 'menu.scheduleCalendar' },
          },
        ],
      },
      // 报销管理
      {
        path: 'expense',
        name: 'Expense',
        redirect: '/expense/requests',
        meta: { titleKey: 'menu.expense', icon: 'Wallet', permission: 'expense:read' },
        children: [
          {
            path: 'requests',
            name: 'ExpenseRequests',
            component: () => import('@/views/expense/RequestsView.vue'),
            meta: { titleKey: 'menu.expenseRequests' },
          },
          {
            path: 'types',
            name: 'ExpenseTypes',
            component: () => import('@/views/expense/TypesView.vue'),
            meta: { titleKey: 'menu.expenseTypes' },
          },
        ],
      },
      // 保险福利
      {
        path: 'insurance',
        name: 'Insurance',
        redirect: '/insurance/overview',
        meta: { titleKey: 'menu.insurance', icon: 'FirstAidKit', permission: 'payroll:read' },
        children: [
          {
            path: 'overview',
            name: 'InsuranceOverview',
            component: () => import('@/views/insurance/OverviewView.vue'),
            meta: { titleKey: 'menu.insuranceOverview' },
          },
          {
            path: 'plans',
            name: 'InsurancePlans',
            component: () => import('@/views/insurance/PlansView.vue'),
            meta: { titleKey: 'menu.insurancePlans' },
          },
          {
            path: 'employees',
            name: 'EmployeeInsurance',
            component: () => import('@/views/insurance/EmployeeInsuranceView.vue'),
            meta: { titleKey: 'menu.employeeInsurance' },
          },
          {
            path: 'benefits',
            name: 'Benefits',
            component: () => import('@/views/insurance/BenefitsView.vue'),
            meta: { titleKey: 'menu.benefits' },
          },
        ],
      },
      // 报税管理
      {
        path: 'tax',
        name: 'Tax',
        redirect: '/tax/profiles',
        meta: { titleKey: 'menu.tax', icon: 'Document', permission: 'payroll:read' },
        children: [
          {
            path: 'profiles',
            name: 'TaxProfiles',
            component: () => import('@/views/tax/ProfilesView.vue'),
            meta: { titleKey: 'menu.taxProfiles' },
          },
          {
            path: 'records',
            name: 'TaxRecords',
            component: () => import('@/views/tax/RecordsView.vue'),
            meta: { titleKey: 'menu.taxRecords' },
          },
          {
            path: 'settings',
            name: 'TaxSettings',
            component: () => import('@/views/tax/SettingsView.vue'),
            meta: { titleKey: 'menu.taxSettings' },
          },
        ],
      },
      // 系统设置
      {
        path: 'settings',
        name: 'Settings',
        redirect: '/settings/configs',
        meta: { titleKey: 'menu.settings', icon: 'Tools', permission: 'settings:read' },
        children: [
          {
            path: 'configs',
            name: 'SystemConfigs',
            component: () => import('@/views/settings/ConfigsView.vue'),
            meta: { titleKey: 'menu.systemConfigs' },
          },
          {
            path: 'roles',
            name: 'Roles',
            component: () => import('@/views/settings/RolesView.vue'),
            meta: { titleKey: 'menu.roles' },
          },
          {
            path: 'approval-flows',
            name: 'ApprovalFlows',
            component: () => import('@/views/settings/ApprovalFlowsView.vue'),
            meta: { titleKey: 'menu.approvalFlows' },
          },
          {
            path: 'audit-logs',
            name: 'AuditLogs',
            component: () => import('@/views/settings/AuditLogsView.vue'),
            meta: { titleKey: 'menu.auditLogs' },
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
    meta: { titleKey: 'error.notFound' },
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

// 路由守卫
router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore();
  
  // 设置页面标题 - 使用 titleKey，实际翻译在组件中处理
  document.title = 'Hrevolve HRM';
  
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
  
  // 检查权限 - 开发环境跳过权限检查
  const requiredPermission = to.meta.permission as string | undefined;
  if (requiredPermission && !import.meta.env.DEV) {
    if (!authStore.hasPermission(requiredPermission)) {
      next({ name: 'Dashboard' });
      return;
    }
  }
  
  next();
});

export default router;
