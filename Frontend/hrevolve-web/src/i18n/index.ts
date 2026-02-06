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
  // 获取当前本地语言包
  const currentMessages = i18n.global.messages.value[locale as 'zh-CN' | 'zh-TW' | 'en-US'] || {};
  
  // 深度合并函数，本地翻译优先（不会被后端空值覆盖）
  const deepMerge = (local: Record<string, unknown>, remote: Record<string, unknown>): Record<string, unknown> => {
    const result = { ...local };
    for (const key in remote) {
      if (remote[key] !== null && remote[key] !== undefined && remote[key] !== '') {
        if (typeof remote[key] === 'object' && !Array.isArray(remote[key])) {
          // 如果本地有这个键，递归合并；否则使用远程的值
          if (result[key] && typeof result[key] === 'object') {
            result[key] = deepMerge(
              result[key] as Record<string, unknown>,
              remote[key] as Record<string, unknown>
            );
          } else {
            result[key] = remote[key];
          }
        } else {
          // 只有当本地没有这个键时，才使用远程的值
          if (!(key in result)) {
            result[key] = remote[key];
          }
        }
      }
    }
    return result;
  };
  
  // 合并：本地翻译优先，后端翻译作为补充
  const mergedMessages = deepMerge(currentMessages as Record<string, unknown>, messages as Record<string, unknown>) as typeof zhCN;
  
  i18n.global.setLocaleMessage(locale, mergedMessages);
}

export default i18n;
