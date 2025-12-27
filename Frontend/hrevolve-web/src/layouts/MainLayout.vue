<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAppStore, useAuthStore } from '@/stores';
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
} from '@element-plus/icons-vue';

const router = useRouter();
const route = useRoute();
const { t, locale } = useI18n();
const appStore = useAppStore();
const authStore = useAuthStore();

// 菜单配置
const menuItems = computed(() => [
  {
    index: '/',
    title: t('menu.dashboard'),
    icon: HomeFilled,
  },
  {
    index: '/self-service',
    title: t('menu.selfService'),
    icon: User,
    children: [
      { index: '/self-service/profile', title: t('menu.profile') },
      { index: '/self-service/attendance', title: t('menu.myAttendance') },
      { index: '/self-service/leave', title: t('menu.myLeave') },
      { index: '/self-service/payroll', title: t('menu.myPayroll') },
    ],
  },
  {
    index: '/assistant',
    title: t('menu.assistant'),
    icon: ChatDotRound,
  },
  {
    index: '/organization',
    title: t('menu.organization'),
    icon: OfficeBuilding,
    permission: 'organization:read',
    children: [
      { index: '/organization/structure', title: t('menu.orgStructure') },
      { index: '/organization/positions', title: t('menu.positions') },
    ],
  },
  {
    index: '/employees',
    title: t('menu.employees'),
    icon: UserFilled,
    permission: 'employee:read',
  },
  {
    index: '/attendance',
    title: t('menu.attendance'),
    icon: Clock,
    permission: 'attendance:read',
    children: [
      { index: '/attendance/records', title: t('menu.attendanceRecords') },
      { index: '/attendance/shifts', title: t('menu.shifts') },
    ],
  },
  {
    index: '/leave',
    title: t('menu.leave'),
    icon: Calendar,
    permission: 'leave:read',
    children: [
      { index: '/leave/requests', title: t('menu.leaveRequests') },
      { index: '/leave/approvals', title: t('menu.leaveApprovals') },
      { index: '/leave/types', title: t('menu.leaveTypes') },
    ],
  },
  {
    index: '/payroll',
    title: t('menu.payroll'),
    icon: Money,
    permission: 'payroll:read',
    children: [
      { index: '/payroll/records', title: t('menu.payrollRecords') },
      { index: '/payroll/periods', title: t('menu.payrollPeriods') },
    ],
  },
]);

// 过滤有权限的菜单
const filteredMenuItems = computed(() => {
  return menuItems.value.filter((item) => {
    if (!item.permission) return true;
    return authStore.hasPermission(item.permission);
  });
});

// 当前激活的菜单
const activeMenu = computed(() => route.path);

// 切换语言
const toggleLanguage = () => {
  const newLang = locale.value === 'zh-CN' ? 'en-US' : 'zh-CN';
  locale.value = newLang;
  appStore.setLanguage(newLang);
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
    <el-aside :width="appStore.sidebarCollapsed ? '64px' : '220px'" class="sidebar">
      <div class="logo">
        <img src="@/assets/logo.svg" alt="Logo" class="logo-img" />
        <span v-show="!appStore.sidebarCollapsed" class="logo-text">Hrevolve</span>
      </div>
      
      <el-menu
        :default-active="activeMenu"
        :collapse="appStore.sidebarCollapsed"
        :collapse-transition="false"
        router
        class="sidebar-menu"
      >
        <template v-for="item in filteredMenuItems" :key="item.index">
          <!-- 有子菜单 -->
          <el-sub-menu v-if="item.children" :index="item.index">
            <template #title>
              <el-icon><component :is="item.icon" /></el-icon>
              <span>{{ item.title }}</span>
            </template>
            <el-menu-item
              v-for="child in item.children"
              :key="child.index"
              :index="child.index"
            >
              {{ child.title }}
            </el-menu-item>
          </el-sub-menu>
          
          <!-- 无子菜单 -->
          <el-menu-item v-else :index="item.index">
            <el-icon><component :is="item.icon" /></el-icon>
            <template #title>{{ item.title }}</template>
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
          <!-- 语言切换 -->
          <el-tooltip :content="locale === 'zh-CN' ? 'English' : '中文'">
            <el-button text @click="toggleLanguage">
              {{ locale === 'zh-CN' ? 'EN' : '中' }}
            </el-button>
          </el-tooltip>
          
          <!-- 用户下拉菜单 -->
          <el-dropdown trigger="click">
            <div class="user-info">
              <el-avatar :size="32" :src="authStore.user?.avatar">
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
.main-layout {
  height: 100vh;
}

.sidebar {
  background-color: #001529;
  transition: width 0.3s;
  overflow: hidden;
  
  .logo {
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0 16px;
    
    .logo-img {
      width: 32px;
      height: 32px;
    }
    
    .logo-text {
      color: #fff;
      font-size: 18px;
      font-weight: 600;
      margin-left: 12px;
      white-space: nowrap;
    }
  }
  
  .sidebar-menu {
    border-right: none;
    background-color: transparent;
    
    :deep(.el-menu-item),
    :deep(.el-sub-menu__title) {
      color: rgba(255, 255, 255, 0.65);
      
      &:hover {
        color: #fff;
        background-color: rgba(255, 255, 255, 0.08);
      }
    }
    
    :deep(.el-menu-item.is-active) {
      color: #fff;
      background-color: #1890ff;
    }
  }
}

.header {
  background-color: #fff;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
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
      
      &:hover {
        color: #1890ff;
      }
    }
  }
  
  .header-right {
    display: flex;
    align-items: center;
    gap: 16px;
    
    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
      cursor: pointer;
      
      .user-name {
        font-size: 14px;
      }
    }
  }
}

.main-content {
  background-color: #f0f2f5;
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
