import { request } from '../request';

/** 语言信息类型 */
export interface LocaleInfo {
  code: string;
  name: string;
  flag: string;
}

/** 本地化API */
export const localizationApi = {
  /** 获取支持的语言列表 */
  getLocales() {
    return request.get<LocaleInfo[]>('/localization/locales');
  },
  
  /** 获取指定语言的翻译包 */
  getMessages(locale: string) {
    return request.get<Record<string, unknown>>(`/localization/messages/${locale}`);
  },
  
  /** 获取所有语言包 */
  getAllMessages() {
    return request.get<Record<string, Record<string, unknown>>>('/localization/messages');
  },
};
