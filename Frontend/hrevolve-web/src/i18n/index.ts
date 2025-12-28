import { createI18n } from 'vue-i18n';
import zhCN from './locales/zh-CN';
import zhTW from './locales/zh-TW';
import enUS from './locales/en-US';

// 支持的语言列表（本地备份，后端优先）
export const supportedLocales = [
  { code: 'zh-CN', name: '简体中文', flag: '🇨🇳' },
  { code: 'zh-TW', name: '繁體中文', flag: '🇹🇼' },
  { code: 'en-US', name: 'English', flag: '🇺🇸' },
];

const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('language') || 'zh-CN',
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'zh-TW': zhTW,
    'en-US': enUS,
  },
});

/** 从后端加载语言包并合并 */
export async function loadLocaleMessages(locale: string, messages: Record<string, unknown>) {
  // 将后端返回的扁平结构转换为前端需要的格式
  const currentMessages = i18n.global.messages.value[locale as 'zh-CN' | 'zh-TW' | 'en-US'];
  
  const formattedMessages = {
    ...messages,
    // 处理命名差异
    dashboard: messages.dashboard_page,
    attendance: { ...(messages.attendance_page as object), ...(currentMessages?.attendance || {}) },
    leave: { ...(messages.leave_page as object), ...(currentMessages?.leave || {}) },
    payroll: { ...(messages.payroll_page as object), ...(currentMessages?.payroll || {}) },
    assistant: messages.assistant_page,
  };
  
  i18n.global.setLocaleMessage(locale, {
    ...currentMessages,
    ...formattedMessages,
  });
}

export default i18n;
