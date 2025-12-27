import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export type Language = 'zh-CN' | 'en-US';
export type Theme = 'light' | 'dark' | 'auto';

export const useAppStore = defineStore('app', () => {
  // 状态
  const sidebarCollapsed = ref(false);
  const language = ref<Language>((localStorage.getItem('language') as Language) || 'zh-CN');
  const theme = ref<Theme>((localStorage.getItem('theme') as Theme) || 'light');
  const loading = ref(false);
  
  // 计算属性
  const isDark = computed(() => {
    if (theme.value === 'auto') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    return theme.value === 'dark';
  });
  
  // 切换侧边栏
  const toggleSidebar = (): void => {
    sidebarCollapsed.value = !sidebarCollapsed.value;
  };
  
  // 设置语言
  const setLanguage = (lang: Language): void => {
    language.value = lang;
    localStorage.setItem('language', lang);
  };
  
  // 设置主题
  const setTheme = (newTheme: Theme): void => {
    theme.value = newTheme;
    localStorage.setItem('theme', newTheme);
    
    // 更新HTML class
    if (isDark.value) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  };
  
  // 设置全局加载状态
  const setLoading = (value: boolean): void => {
    loading.value = value;
  };
  
  return {
    // 状态
    sidebarCollapsed,
    language,
    theme,
    loading,
    // 计算属性
    isDark,
    // 方法
    toggleSidebar,
    setLanguage,
    setTheme,
    setLoading,
  };
});
