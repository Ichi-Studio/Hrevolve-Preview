import { request } from '../request';
import type { ChatMessage, ChatResponse } from '@/types';

/** AI助手API */
export const agentApi = {
  /** 发送消息给AI助手 */
  chat(message: string) {
    return request.post<ChatResponse>('/agent/chat', { message });
  },
  
  /** 获取对话历史 */
  getHistory(limit: number = 20) {
    return request.get<ChatMessage[]>('/agent/history', { params: { limit } });
  },
  
  /** 清除对话历史 */
  clearHistory() {
    return request.delete('/agent/history');
  },
};
