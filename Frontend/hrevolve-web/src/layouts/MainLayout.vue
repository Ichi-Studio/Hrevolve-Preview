<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { useAppStore, useAuthStore } from '@/stores';
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

// 当前语言信息
const currentLocale = computed(() => {
  return locales.value.find(l => l.code === locale.value) || locales.value[0];
});

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
    locale.value = langCode;
    appStore.setLanguage(langCode);
    
    const langName = locales.value.find(l => l.code === langCode)?.name || langCode;
    ElMessage.success({
      message: langCode === 'en-US' ? `Language switched to ${langName}` : `语言已切换为${langName}`,
      duration: 1500,
    });
  } catch {
    // 即使后端加载失败，也切换到本地语言包
    locale.value = langCode;
    appStore.setLanguage(langCode);
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
  transition: width 0.3s;
  overflow: hidden;
  
  .logo {
    height: 64px;
    display: flex;
    align-items: center;
    padding: 0 16px;
    border-bottom: 1px solid $border-color;
    background: linear-gradient(90deg, rgba(212, 175, 55, 0.08) 0%, transparent 100%);
    
    .logo-icon {
      width: 36px;
      height: 36px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
      border-radius: 8px;
      flex-shrink: 0;
    }
    
    .logo-img {
      width: 24px;
      height: 24px;
      filter: brightness(0) invert(0);
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
    }
  }
  
  .sidebar-menu {
    border-right: none;
    background-color: transparent;
    padding: 8px;
    
    :deep(.el-menu-item),
    :deep(.el-sub-menu__title) {
      color: $text-tertiary;
      border-radius: 8px;
      margin: 4px 0;
      transition: all 0.3s;
      
      .el-icon {
        color: $text-tertiary;
        transition: all 0.3s;
      }
      
      &:hover {
        color: $gold-primary;
        background: linear-gradient(90deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
        
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
    
    :deep(.el-sub-menu .el-menu) {
      background: transparent;
      
      .el-menu-item {
        padding-left: 52px !important;
        font-size: 13px;
      }
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
      border-radius: 6px;
      transition: all 0.3s;
      
      &:hover {
        color: $gold-primary;
        background: rgba(212, 175, 55, 0.1);
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
