<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Promotion, Delete, ChatDotRound } from '@element-plus/icons-vue';
import { agentApi } from '@/api';
import type { ChatMessage } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();

// 消息列表
const messages = ref<ChatMessage[]>([]);
const inputMessage = ref('');
const loading = ref(false);
const chatContainerRef = ref<HTMLElement>();

// 快捷建议
const suggestions = [
  { text: '我还有多少年假？', icon: '🏖️' },
  { text: '帮我请假', icon: '📝' },
  { text: '查询本月薪资', icon: '💰' },
  { text: '今天的考勤状态', icon: '⏰' },
  { text: '公司年假政策是什么？', icon: '📋' },
  { text: '查看组织架构', icon: '🏢' },
];

// 滚动到底部
const scrollToBottom = () => {
  nextTick(() => {
    if (chatContainerRef.value) {
      chatContainerRef.value.scrollTop = chatContainerRef.value.scrollHeight;
    }
  });
};

// 发送消息
const sendMessage = async (text?: string) => {
  const message = text || inputMessage.value.trim();
  if (!message || loading.value) return;
  
  // 添加用户消息
  const userMessage: ChatMessage = {
    id: Date.now().toString(),
    role: 'user',
    content: message,
    timestamp: new Date().toISOString(),
  };
  messages.value.push(userMessage);
  inputMessage.value = '';
  scrollToBottom();
  
  // 添加加载中的助手消息
  const loadingMessage: ChatMessage = {
    id: (Date.now() + 1).toString(),
    role: 'assistant',
    content: '',
    timestamp: new Date().toISOString(),
    isLoading: true,
  };
  messages.value.push(loadingMessage);
  scrollToBottom();
  
  loading.value = true;
  
  try {
    const res = await agentApi.chat(message);
    
    // 更新助手消息
    const lastMessage = messages.value[messages.value.length - 1];
    lastMessage.content = res.data.reply;
    lastMessage.isLoading = false;
    lastMessage.timestamp = new Date().toISOString();
  } catch {
    // 移除加载消息
    messages.value.pop();
    ElMessage.error('发送失败，请重试');
  } finally {
    loading.value = false;
    scrollToBottom();
  }
};

// 清空对话
const clearHistory = async () => {
  try {
    await ElMessageBox.confirm('确定要清空所有对话记录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    });
    
    await agentApi.clearHistory();
    messages.value = [];
    ElMessage.success('对话已清空');
  } catch {
    // 用户取消
  }
};

// 加载历史记录
const loadHistory = async () => {
  try {
    const res = await agentApi.getHistory(20);
    messages.value = res.data.map((msg, index) => ({
      id: index.toString(),
      role: msg.role as 'user' | 'assistant',
      content: msg.content,
      timestamp: msg.timestamp,
    }));
    scrollToBottom();
  } catch {
    // 忽略错误
  }
};

// 格式化时间
const formatTime = (timestamp: string) => {
  return dayjs(timestamp).format('HH:mm');
};

onMounted(() => {
  loadHistory();
});
</script>

<template>
  <div class="assistant-container">
    <el-card class="chat-card">
      <!-- 头部 -->
      <template #header>
        <div class="chat-header">
          <div class="header-left">
            <el-icon :size="24" color="#1890ff"><ChatDotRound /></el-icon>
            <span class="title">{{ t('assistant.title') }}</span>
          </div>
          <el-button
            text
            type="danger"
            :icon="Delete"
            @click="clearHistory"
          >
            {{ t('assistant.clearHistory') }}
          </el-button>
        </div>
      </template>
      
      <!-- 消息区域 -->
      <div ref="chatContainerRef" class="chat-messages">
        <!-- 欢迎消息 -->
        <div v-if="messages.length === 0" class="welcome-message">
          <div class="welcome-icon">🤖</div>
          <h3>您好！我是Hrevolve HR智能助手</h3>
          <p>我可以帮您查询假期余额、薪资信息、考勤记录，也可以协助您提交请假申请。</p>
          
          <div class="suggestions">
            <p class="suggestions-title">{{ t('assistant.suggestions') }}</p>
            <div class="suggestion-list">
              <el-button
                v-for="suggestion in suggestions"
                :key="suggestion.text"
                class="suggestion-btn"
                @click="sendMessage(suggestion.text)"
              >
                <span class="suggestion-icon">{{ suggestion.icon }}</span>
                {{ suggestion.text }}
              </el-button>
            </div>
          </div>
        </div>
        
        <!-- 消息列表 -->
        <div
          v-for="message in messages"
          :key="message.id"
          :class="['message', message.role]"
        >
          <div class="message-avatar">
            <template v-if="message.role === 'user'">👤</template>
            <template v-else>🤖</template>
          </div>
          
          <div class="message-content">
            <div v-if="message.isLoading" class="loading-dots">
              <span></span>
              <span></span>
              <span></span>
            </div>
            <template v-else>
              <div class="message-text" v-html="message.content.replace(/\n/g, '<br>')"></div>
              <div class="message-time">{{ formatTime(message.timestamp) }}</div>
            </template>
          </div>
        </div>
      </div>
      
      <!-- 输入区域 -->
      <div class="chat-input">
        <el-input
          v-model="inputMessage"
          :placeholder="t('assistant.placeholder')"
          :disabled="loading"
          @keyup.enter="sendMessage()"
        >
          <template #append>
            <el-button
              type="primary"
              :icon="Promotion"
              :loading="loading"
              @click="sendMessage()"
            >
              {{ t('assistant.send') }}
            </el-button>
          </template>
        </el-input>
      </div>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.assistant-container {
  height: calc(100vh - 180px);
  
  .chat-card {
    height: 100%;
    display: flex;
    flex-direction: column;
    
    :deep(.el-card__header) {
      padding: 16px 20px;
      border-bottom: 1px solid #f0f0f0;
    }
    
    :deep(.el-card__body) {
      flex: 1;
      display: flex;
      flex-direction: column;
      padding: 0;
      overflow: hidden;
    }
  }
  
  .chat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    
    .header-left {
      display: flex;
      align-items: center;
      gap: 8px;
      
      .title {
        font-size: 16px;
        font-weight: 600;
      }
    }
  }
  
  .chat-messages {
    flex: 1;
    overflow-y: auto;
    padding: 20px;
    background-color: #f5f7fa;
  }
  
  .welcome-message {
    text-align: center;
    padding: 40px 20px;
    
    .welcome-icon {
      font-size: 48px;
      margin-bottom: 16px;
    }
    
    h3 {
      margin: 0 0 8px;
      font-size: 20px;
      color: #1a1a1a;
    }
    
    p {
      margin: 0 0 24px;
      color: #666;
    }
    
    .suggestions {
      .suggestions-title {
        font-size: 14px;
        color: #999;
        margin-bottom: 12px;
      }
      
      .suggestion-list {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 8px;
        
        .suggestion-btn {
          .suggestion-icon {
            margin-right: 4px;
          }
        }
      }
    }
  }
  
  .message {
    display: flex;
    gap: 12px;
    margin-bottom: 16px;
    
    &.user {
      flex-direction: row-reverse;
      
      .message-content {
        background-color: #1890ff;
        color: #fff;
        border-radius: 12px 12px 0 12px;
        
        .message-time {
          color: rgba(255, 255, 255, 0.7);
        }
      }
    }
    
    &.assistant {
      .message-content {
        background-color: #fff;
        border-radius: 12px 12px 12px 0;
      }
    }
    
    .message-avatar {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background-color: #fff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      flex-shrink: 0;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }
    
    .message-content {
      max-width: 70%;
      padding: 12px 16px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
      
      .message-text {
        line-height: 1.6;
        word-break: break-word;
      }
      
      .message-time {
        font-size: 12px;
        color: #999;
        margin-top: 4px;
        text-align: right;
      }
    }
  }
  
  .loading-dots {
    display: flex;
    gap: 4px;
    padding: 4px 0;
    
    span {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: #999;
      animation: bounce 1.4s infinite ease-in-out both;
      
      &:nth-child(1) { animation-delay: -0.32s; }
      &:nth-child(2) { animation-delay: -0.16s; }
    }
  }
  
  @keyframes bounce {
    0%, 80%, 100% { transform: scale(0); }
    40% { transform: scale(1); }
  }
  
  .chat-input {
    padding: 16px 20px;
    border-top: 1px solid #f0f0f0;
    background-color: #fff;
    
    :deep(.el-input-group__append) {
      padding: 0;
      
      .el-button {
        border-radius: 0 4px 4px 0;
        margin: -1px;
      }
    }
  }
}
</style>
