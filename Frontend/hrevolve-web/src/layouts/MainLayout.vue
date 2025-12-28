<script setup lang="ts">
import { computed, ref, onMounted, markRaw, shallowRef } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { useAppStore, useAuthStore } from '@/stores';
import type { Language } from '@/stores/app';
import { supportedLocales, loadLocaleMessages } from '@/i18n';
import { localizationApi, type LocaleInfo } from '@/api';
import {
  HomeFilled,
  User,
  ChatDotRound,
  OfficeBuilding,
  UserFilled,
  Clock,
  Calendar,
  Money,
  Expand,
  Fold,
  Setting,
  SwitchButton,
  Wallet,
  FirstAidKit,
  Document,
  Tools,
} from '@element-plus/icons-vue';

const router = useRouter();
const route = useRoute();
const { t, locale } = useI18n();
const appStore = useAppStore();
const authStore = useAuthStore();

// 语言列表（优先从后端获取）
const locales = ref<LocaleInfo[]>(supportedLocales);
const isChangingLang = ref(false);

// 初始化时从后端加载语言列表
onMounted(async () => {
  try {
    const response = await localizationApi.getLocales();
    if (response.data && response.data.length > 0) {
      locales.value = response.data;
    }
  } catch {
    // 使用本地备份
  }
});

// 菜单配置 - 使用 shallowRef 避免深度响应式，提升性能
// 图标使用 markRaw 包裹避免响应式转换
const menuItems = shallowRef([
  {
    index: '/',
    title: 'menu.dashboard',
    icon: markRaw(HomeFilled),
  },
  {
    index: '/self-service',
    title: 'menu.selfService',
    icon: markRaw(User),
    children: [
      { index: '/self-service/profile', title: 'menu.profile' },
      { index: '/self-service/attendance', title: 'menu.myAttendance' },
      { index: '/self-service/leave', title: 'menu.myLeave' },
      { index: '/self-service/payroll', title: 'menu.myPayroll' },
    ],
  },
  {
    index: '/assistant',
    title: 'menu.assistant',
    icon: markRaw(ChatDotRound),
  },
  {
    index: '/organization',
    title: 'menu.organization',
    icon: markRaw(OfficeBuilding),
    permission: 'organization:read',
    children: [
      { index: '/organization/structure', title: 'menu.orgStructure' },
      { index: '/organization/positions', title: 'menu.positions' },
    ],
  },
  {
    index: '/employees',
    title: 'menu.employees',
    icon: markRaw(UserFilled),
    permission: 'employee:read',
  },
  {
    index: '/attendance',
    title: 'menu.attendance',
    icon: markRaw(Clock),
    permission: 'attendance:read',
    children: [
      { index: '/attendance/records', title: 'menu.attendanceRecords' },
      { index: '/attendance/shifts', title: 'menu.shifts' },
    ],
  },
  {
    index: '/leave',
    title: 'menu.leave',
    icon: markRaw(Calendar),
    permission: 'leave:read',
    children: [
      { index: '/leave/requests', title: 'menu.leaveRequests' },
      { index: '/leave/approvals', title: 'menu.leaveApprovals' },
      { index: '/leave/types', title: 'menu.leaveTypes' },
    ],
  },
  {
    index: '/payroll',
    title: 'menu.payroll',
    icon: markRaw(Money),
    permission: 'payroll:read',
    children: [
      { index: '/payroll/records', title: 'menu.payrollRecords' },
      { index: '/payroll/periods', title: 'menu.payrollPeriods' },
    ],
  },
  {
    index: '/company',
    title: 'menu.company',
    icon: markRaw(Setting),
    permission: 'settings:read',
    children: [
      { index: '/company/tenant', title: 'menu.companyInfo' },
      { index: '/company/cost-centers', title: 'menu.costCenters' },
      { index: '/company/tags', title: 'menu.tags' },
      { index: '/company/clock-devices', title: 'menu.clockDevices' },
      { index: '/company/users', title: 'menu.users' },
    ],
  },
  {
    index: '/schedule',
    title: 'menu.schedule',
    icon: markRaw(Calendar),
    permission: 'attendance:read',
    children: [
      { index: '/schedule/overview', title: 'menu.scheduleOverview' },
      { index: '/schedule/table', title: 'menu.scheduleTable' },
      { index: '/schedule/templates', title: 'menu.shiftTemplates' },
      { index: '/schedule/calendar', title: 'menu.scheduleCalendar' },
    ],
  },
  {
    index: '/expense',
    title: 'menu.expense',
    icon: markRaw(Wallet),
    permission: 'expense:read',
    children: [
      { index: '/expense/requests', title: 'menu.expenseRequests' },
      { index: '/expense/types', title: 'menu.expenseTypes' },
    ],
  },
  {
    index: '/insurance',
    title: 'menu.insurance',
    icon: markRaw(FirstAidKit),
    permission: 'payroll:read',
    children: [
      { index: '/insurance/overview', title: 'menu.insuranceOverview' },
      { index: '/insurance/plans', title: 'menu.insurancePlans' },
      { index: '/insurance/employees', title: 'menu.employeeInsurance' },
      { index: '/insurance/benefits', title: 'menu.benefits' },
    ],
  },
  {
    index: '/tax',
    title: 'menu.tax',
    icon: markRaw(Document),
    permission: 'payroll:read',
    children: [
      { index: '/tax/profiles', title: 'menu.taxProfiles' },
      { index: '/tax/records', title: 'menu.taxRecords' },
      { index: '/tax/settings', title: 'menu.taxSettings' },
    ],
  },
  {
    index: '/settings',
    title: 'menu.settings',
    icon: markRaw(Tools),
    permission: 'settings:read',
    children: [
      { index: '/settings/configs', title: 'menu.systemConfigs' },
      { index: '/settings/roles', title: 'menu.roles' },
      { index: '/settings/approval-flows', title: 'menu.approvalFlows' },
      { index: '/settings/audit-logs', title: 'menu.auditLogs' },
    ],
  },
]);

// 过滤有权限的菜单 - 使用缓存避免频繁计算
// 注意：开发环境暂时显示所有菜单，生产环境需要恢复权限检查
const filteredMenuItems = computed(() => {
  // 开发模式下直接返回所有菜单，避免权限检查开销
  if (import.meta.env.DEV) return menuItems.value;
  
  return menuItems.value.filter((item) => {
    if (!item.permission) return true;
    return authStore.hasPermission(item.permission);
  });
});

// 当前激活的菜单
const activeMenu = computed(() => route.path);

// 切换语言
const changeLanguage = async (langCode: string) => {
  if (langCode === locale.value || isChangingLang.value) return;
  
  isChangingLang.value = true;
  
  try {
    // 从后端加载语言包
    const response = await localizationApi.getMessages(langCode);
    if (response.data) {
      await loadLocaleMessages(langCode, response.data);
    }
    
    // 切换语言
    locale.value = langCode as 'zh-CN' | 'zh-TW' | 'en-US';
    appStore.setLanguage(langCode as Language);
    
    const langName = locales.value.find(l => l.code === langCode)?.name || langCode;
    ElMessage.success({
      message: langCode === 'en-US' ? `Language switched to ${langName}` : `语言已切换为${langName}`,
      duration: 1500,
    });
  } catch {
    // 即使后端加载失败，也切换到本地语言包
    locale.value = langCode as 'zh-CN' | 'zh-TW' | 'en-US';
    appStore.setLanguage(langCode as Language);
  } finally {
    isChangingLang.value = false;
  }
};

// 退出登录
const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};
</script>

<template>
  <el-container class="main-layout">
    <!-- 侧边栏 -->
    <el-aside :width="appStore.sidebarCollapsed ? '64px' : '240px'" class="sidebar">
      <div class="logo">
        <div class="logo-icon">
          <img src="@/assets/logo.svg" alt="Logo" class="logo-img" />
        </div>
        <span v-show="!appStore.sidebarCollapsed" class="logo-text">Hrevolve</span>
      </div>
      
      <el-menu
        :default-active="activeMenu"
        :collapse="appStore.sidebarCollapsed"
        :collapse-transition="false"
        :unique-opened="true"
        router
        class="sidebar-menu"
      >
        <template v-for="item in filteredMenuItems" :key="item.index">
          <!-- 有子菜单 -->
          <el-sub-menu v-if="item.children" :index="item.index" :popper-class="'sidebar-submenu-popper'">
            <template #title>
              <el-icon><component :is="item.icon" /></el-icon>
              <span>{{ t(item.title) }}</span>
            </template>
            <el-menu-item
              v-for="child in item.children"
              :key="child.index"
              :index="child.index"
            >
              {{ t(child.title) }}
            </el-menu-item>
          </el-sub-menu>
          
          <!-- 无子菜单 -->
          <el-menu-item v-else :index="item.index">
            <el-icon><component :is="item.icon" /></el-icon>
            <template #title>{{ t(item.title) }}</template>
          </el-menu-item>
        </template>
      </el-menu>
    </el-aside>
    
    <el-container>
      <!-- 顶部导航 -->
      <el-header class="header">
        <div class="header-left">
          <el-icon
            class="collapse-btn"
            @click="appStore.toggleSidebar"
          >
            <Fold v-if="!appStore.sidebarCollapsed" />
            <Expand v-else />
          </el-icon>
          
          <!-- 面包屑 -->
          <el-breadcrumb separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">{{ t('menu.dashboard') }}</el-breadcrumb-item>
            <el-breadcrumb-item v-if="route.meta.title">
              {{ route.meta.title }}
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        
        <div class="header-right">
          <!-- 语言切换下拉菜单 -->
          <el-dropdown trigger="click" @command="changeLanguage" :disabled="isChangingLang">
            <div class="lang-switcher" :class="{ 'is-loading': isChangingLang }">
              <svg class="lang-icon" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="2" y1="12" x2="22" y2="12"></line>
                <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
              </svg>
              <span class="lang-code">{{ locale.split('-')[0].toUpperCase() }}</span>
            </div>
            <template #dropdown>
              <el-dropdown-menu class="lang-dropdown">
                <el-dropdown-item
                  v-for="lang in locales"
                  :key="lang.code"
                  :command="lang.code"
                  :class="{ 'is-active': locale === lang.code }"
                >
                  {{ lang.name }}
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
          
          <!-- 用户下拉菜单 -->
          <el-dropdown trigger="click">
            <div class="user-info">
              <el-avatar :size="36" :src="authStore.user?.avatar" class="user-avatar">
                {{ authStore.user?.displayName?.charAt(0) }}
              </el-avatar>
              <span class="user-name">{{ authStore.user?.displayName }}</span>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item :icon="User" @click="router.push('/self-service/profile')">
                  {{ t('menu.profile') }}
                </el-dropdown-item>
                <el-dropdown-item :icon="Setting">
                  {{ t('common.settings') || '设置' }}
                </el-dropdown-item>
                <el-dropdown-item divided :icon="SwitchButton" @click="handleLogout">
                  {{ t('auth.logout') }}
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>
      
      <!-- 主内容区 -->
      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped lang="scss">
// 黑金主题变量
$gold-primary: #D4AF37;
$gold-light: #F4D03F;
$gold-dark: #B8860B;
$bg-dark: #0D0D0D;
$bg-card: #1A1A1A;
$bg-main: #121212;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);
$text-tertiary: rgba(255, 255, 255, 0.65);
$border-color: rgba(212, 175, 55, 0.2);

.main-layout {
  height: 100vh;
  background: $bg-main;
}

.sidebar {
  background: linear-gradient(180deg, #0A0A0A 0%, #151515 50%, #0D0D0D 100%);
  border-right: 1px solid $border-color;
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
  position: relative;
  
  // 添加侧边栏阴影效果
  &::after {
    content: '';
    position: absolute;
    top: 0;
    right: 0;
    width: 1px;
    height: 100%;
    background: linear-gradient(180deg, 
      transparent 0%, 
      rgba(212, 175, 55, 0.3) 50%, 
      transparent 100%
    );
    opacity: 0;
    transition: opacity 0.4s;
  }
  
  &:hover::after {
    opacity: 1;
  }
  
  .logo {
    height: 64px;
    display: flex;
    align-items: center;
    padding: 0 16px;
    border-bottom: 1px solid $border-color;
    background: linear-gradient(90deg, rgba(212, 175, 55, 0.08) 0%, transparent 100%);
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
    
    .logo-icon {
      width: 36px;
      height: 36px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
      border-radius: 8px;
      flex-shrink: 0;
      transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
      box-shadow: 0 2px 8px rgba(212, 175, 55, 0.2);
      
      &:hover {
        transform: scale(1.05) rotate(5deg);
        box-shadow: 0 4px 16px rgba(212, 175, 55, 0.4);
      }
    }
    
    .logo-img {
      width: 24px;
      height: 24px;
      filter: brightness(0) invert(0);
      transition: transform 0.3s;
    }
    
    .logo-text {
      font-size: 20px;
      font-weight: 700;
      margin-left: 12px;
      white-space: nowrap;
      background: linear-gradient(135deg, $gold-primary 0%, $gold-light 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
      opacity: 1;
      transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
    }
  }
  
  .sidebar-menu {
    border-right: none;
    background-color: transparent;
    padding: 8px;
    
    // 使用 GPU 加速优化动画性能
    :deep(.el-menu-item),
    :deep(.el-sub-menu__title) {
      color: $text-tertiary;
      border-radius: 8px;
      margin: 4px 0;
      transition: color 0.25s ease, background-color 0.25s ease;
      position: relative;
      overflow: hidden;
      backface-visibility: hidden; // 防止闪烁
      
      // 添加悬停时的光效
      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: -100%;
        width: 100%;
        height: 100%;
        background: linear-gradient(90deg, 
          transparent 0%, 
          rgba(212, 175, 55, 0.1) 50%, 
          transparent 100%
        );
        transition: left 0.5s;
      }
      
      .el-icon {
        color: $text-tertiary;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      }
      
      &:hover {
        color: $gold-primary;
        background: linear-gradient(90deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
        
        &::before {
          left: 100%;
        }
        
        .el-icon {
          color: $gold-primary;
        }
      }
    }
    
    :deep(.el-menu-item.is-active) {
      color: $bg-dark;
      background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
      font-weight: 600;
      box-shadow: 0 4px 15px rgba(212, 175, 55, 0.3);
      
      &::before {
        display: none;
      }
      
      .el-icon {
        color: $bg-dark;
      }
    }
    
    :deep(.el-sub-menu.is-active > .el-sub-menu__title) {
      color: $gold-primary;
      
      .el-icon {
        color: $gold-primary;
      }
    }
    
    // 完全自定义子菜单展开动画 - 避免 Element Plus 的"向上弹"问题
    :deep(.el-sub-menu) {
      .el-menu {
        background: transparent;
        max-height: 0;
        overflow: hidden;
        opacity: 0;
        transform: scaleY(0.8);
        transform-origin: top;
        transition: max-height 0.35s cubic-bezier(0.4, 0, 0.2, 1),
                    opacity 0.25s ease,
                    transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        
        .el-menu-item {
          padding-left: 52px !important;
          font-size: 13px;
        }
      }
      
      // 展开状态 - 使用足够大的 max-height
      &.is-opened .el-menu {
        max-height: 800px;
        opacity: 1;
        transform: scaleY(1);
        transition: max-height 0.4s cubic-bezier(0.4, 0, 0.2, 1),
                    opacity 0.3s ease 0.05s,
                    transform 0.35s cubic-bezier(0.4, 0, 0.2, 1) 0.05s;
      }
    }
    
    // 优化子菜单箭头动画
    :deep(.el-sub-menu__icon-arrow) {
      transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    }
  }
}

// 全局子菜单弹出层样式优化
:deep(.sidebar-submenu-popper) {
  background: linear-gradient(145deg, rgba(26, 26, 26, 0.98) 0%, rgba(13, 13, 13, 0.98) 100%) !important;
  border: 1px solid $border-color !important;
  border-radius: 10px !important;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.6) !important;
  backdrop-filter: blur(10px);
  
  .el-menu-item {
    color: $text-secondary !important;
    transition: all 0.2s ease;
    
    &:hover {
      background: rgba(212, 175, 55, 0.12) !important;
      color: $gold-primary !important;
    }
    
    &.is-active {
      background: rgba(212, 175, 55, 0.2) !important;
      color: $gold-primary !important;
      font-weight: 500;
    }
  }
}

.header {
  background: linear-gradient(90deg, rgba(13, 13, 13, 0.98) 0%, rgba(26, 26, 26, 0.98) 100%);
  border-bottom: 1px solid $border-color;
  backdrop-filter: blur(10px);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  
  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
    
    .collapse-btn {
      font-size: 20px;
      cursor: pointer;
      color: $text-tertiary;
      padding: 8px;
      border-radius: 8px;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;
      
      &::before {
        content: '';
        position: absolute;
        inset: 0;
        border-radius: 8px;
        background: rgba(212, 175, 55, 0.1);
        opacity: 0;
        transition: opacity 0.3s;
      }
      
      &:hover {
        color: $gold-primary;
        transform: scale(1.1);
        
        &::before {
          opacity: 1;
        }
      }
      
      &:active {
        transform: scale(0.95);
      }
    }
  }
  
  .header-right {
    display: flex;
    align-items: center;
    gap: 16px;
    
    .lang-switcher {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 12px;
      border-radius: 20px;
      cursor: pointer;
      background: rgba(212, 175, 55, 0.08);
      border: 1px solid rgba(212, 175, 55, 0.2);
      transition: all 0.3s ease;
      
      &:hover {
        background: rgba(212, 175, 55, 0.15);
        border-color: rgba(212, 175, 55, 0.4);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(212, 175, 55, 0.15);
      }
      
      &.is-loading {
        opacity: 0.6;
        pointer-events: none;
      }
      
      .lang-icon {
        color: $gold-primary;
      }
      
      .lang-code {
        font-size: 12px;
        font-weight: 600;
        color: $gold-primary;
        letter-spacing: 0.5px;
      }
    }
    
    :deep(.lang-dropdown) {
      min-width: 120px;
      padding: 6px;
      background: #1A1A1A;
      border: 1px solid rgba(212, 175, 55, 0.2);
      border-radius: 10px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
      
      .el-dropdown-menu__item {
        padding: 10px 16px;
        margin: 2px 0;
        border-radius: 6px;
        font-size: 14px;
        color: $text-secondary;
        transition: all 0.2s;
        
        &.is-active {
          color: $gold-primary;
          background: rgba(212, 175, 55, 0.12);
          font-weight: 500;
        }
        
        &:hover:not(.is-active) {
          background: rgba(255, 255, 255, 0.05);
          color: $text-primary;
        }
      }
    }
    
    .user-info {
      display: flex;
      align-items: center;
      gap: 10px;
      cursor: pointer;
      padding: 6px 12px;
      border-radius: 8px;
      transition: all 0.3s;
      
      &:hover {
        background: rgba(212, 175, 55, 0.1);
      }
      
      .user-avatar {
        background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
        color: $bg-dark;
        font-weight: 600;
      }
      
      .user-name {
        font-size: 14px;
        color: $text-secondary;
        font-weight: 500;
      }
    }
  }
}

.main-content {
  background: linear-gradient(135deg, #0D0D0D 0%, #151515 50%, #121212 100%);
  padding: 24px;
  overflow-y: auto;
}

// 页面切换动画
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
