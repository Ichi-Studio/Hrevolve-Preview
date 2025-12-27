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
    <div class="chat-card">
      <!-- 头部 -->
      <div class="chat-header">
        <div class="header-left">
          <div class="header-icon">
            <el-icon :size="20"><ChatDotRound /></el-icon>
          </div>
          <div class="header-info">
            <span class="title">{{ t('assistant.title') }}</span>
            <span class="subtitle">7×24小时智能服务</span>
          </div>
        </div>
        <el-button
          class="clear-btn"
          text
          :icon="Delete"
          @click="clearHistory"
        >
          {{ t('assistant.clearHistory') }}
        </el-button>
      </div>
      
      <!-- 消息区域 -->
      <div ref="chatContainerRef" class="chat-messages">
        <!-- 欢迎消息 -->
        <div v-if="messages.length === 0" class="welcome-message">
          <div class="welcome-avatar">
            <span>🤖</span>
          </div>
          <h3>您好！我是Hrevolve HR智能助手</h3>
          <p>我可以帮您查询假期余额、薪资信息、考勤记录，也可以协助您提交请假申请。</p>
          
          <div class="suggestions">
            <p class="suggestions-title">{{ t('assistant.suggestions') }}</p>
            <div class="suggestion-list">
              <button
                v-for="suggestion in suggestions"
                :key="suggestion.text"
                class="suggestion-btn"
                @click="sendMessage(suggestion.text)"
              >
                <span class="suggestion-icon">{{ suggestion.icon }}</span>
                <span class="suggestion-text">{{ suggestion.text }}</span>
              </button>
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
        <div class="input-wrapper">
          <input
            v-model="inputMessage"
            type="text"
            :placeholder="t('assistant.placeholder')"
            :disabled="loading"
            @keyup.enter="sendMessage()"
          />
          <button
            class="send-btn"
            :disabled="loading || !inputMessage.trim()"
            @click="sendMessage()"
          >
            <el-icon v-if="!loading" :size="20"><Promotion /></el-icon>
            <span v-else class="btn-loading"></span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
// 黑金主题变量
$gold-primary: #D4AF37;
$gold-light: #F4D03F;
$gold-dark: #B8860B;
$bg-dark: #0D0D0D;
$bg-card: #1A1A1A;
$bg-darker: #121212;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);
$text-tertiary: rgba(255, 255, 255, 0.65);
$border-color: rgba(212, 175, 55, 0.2);

.assistant-container {
  height: calc(100vh - 140px);
  
  .chat-card {
    height: 100%;
    display: flex;
    flex-direction: column;
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 16px;
    overflow: hidden;
  }
  
  .chat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 16px 24px;
    background: linear-gradient(90deg, rgba(212, 175, 55, 0.1) 0%, transparent 100%);
    border-bottom: 1px solid $border-color;
    
    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;
      
      .header-icon {
        width: 40px;
        height: 40px;
        border-radius: 10px;
        background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
        display: flex;
        align-items: center;
        justify-content: center;
        color: $bg-dark;
      }
      
      .header-info {
        display: flex;
        flex-direction: column;
        
        .title {
          font-size: 16px;
          font-weight: 600;
          color: $text-primary;
        }
        
        .subtitle {
          font-size: 12px;
          color: $text-tertiary;
        }
      }
    }
    
    .clear-btn {
      color: $text-tertiary;
      
      &:hover {
        color: #ff4d4f;
      }
    }
  }
  
  .chat-messages {
    flex: 1;
    overflow-y: auto;
    padding: 24px;
    background: linear-gradient(180deg, $bg-darker 0%, $bg-card 100%);
  }
  
  .welcome-message {
    text-align: center;
    padding: 40px 20px;
    
    .welcome-avatar {
      width: 80px;
      height: 80px;
      margin: 0 auto 20px;
      border-radius: 20px;
      background: linear-gradient(135deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.05) 100%);
      border: 1px solid $border-color;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 40px;
    }
    
    h3 {
      margin: 0 0 12px;
      font-size: 22px;
      font-weight: 600;
      background: linear-gradient(135deg, $gold-primary 0%, $gold-light 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }
    
    p {
      margin: 0 0 32px;
      color: $text-tertiary;
      font-size: 14px;
      line-height: 1.6;
    }
    
    .suggestions {
      .suggestions-title {
        font-size: 13px;
        color: $text-tertiary;
        margin-bottom: 16px;
      }
      
      .suggestion-list {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 10px;
        
        .suggestion-btn {
          display: flex;
          align-items: center;
          gap: 6px;
          padding: 10px 16px;
          background: rgba(255, 255, 255, 0.03);
          border: 1px solid $border-color;
          border-radius: 20px;
          color: $text-secondary;
          font-size: 13px;
          cursor: pointer;
          transition: all 0.3s;
          
          &:hover {
            background: rgba(212, 175, 55, 0.1);
            border-color: $gold-primary;
            color: $gold-primary;
            transform: translateY(-2px);
          }
          
          .suggestion-icon {
            font-size: 16px;
          }
        }
      }
    }
  }
  
  .message {
    display: flex;
    gap: 12px;
    margin-bottom: 20px;
    animation: fadeIn 0.3s ease;
    
    &.user {
      flex-direction: row-reverse;
      
      .message-content {
        background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
        color: $bg-dark;
        border-radius: 16px 16px 4px 16px;
        
        .message-time {
          color: rgba(0, 0, 0, 0.5);
        }
      }
    }
    
    &.assistant {
      .message-content {
        background: rgba(255, 255, 255, 0.05);
        border: 1px solid $border-color;
        border-radius: 16px 16px 16px 4px;
      }
    }
    
    .message-avatar {
      width: 40px;
      height: 40px;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid $border-color;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      flex-shrink: 0;
    }
    
    .message-content {
      max-width: 70%;
      padding: 14px 18px;
      
      .message-text {
        line-height: 1.7;
        word-break: break-word;
        font-size: 14px;
      }
      
      .message-time {
        font-size: 11px;
        color: $text-tertiary;
        margin-top: 6px;
        text-align: right;
      }
    }
  }
  
  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(10px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  .loading-dots {
    display: flex;
    gap: 6px;
    padding: 4px 0;
    
    span {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: $gold-primary;
      animation: bounce 1.4s infinite ease-in-out both;
      
      &:nth-child(1) { animation-delay: -0.32s; }
      &:nth-child(2) { animation-delay: -0.16s; }
    }
  }
  
  @keyframes bounce {
    0%, 80%, 100% { transform: scale(0); opacity: 0.5; }
    40% { transform: scale(1); opacity: 1; }
  }
  
  .chat-input {
    padding: 20px 24px;
    background: $bg-card;
    border-top: 1px solid $border-color;
    
    .input-wrapper {
      display: flex;
      gap: 12px;
      padding: 6px;
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid $border-color;
      border-radius: 12px;
      transition: all 0.3s;
      
      &:focus-within {
        border-color: $gold-primary;
        box-shadow: 0 0 0 3px rgba(212, 175, 55, 0.1);
      }
      
      input {
        flex: 1;
        background: transparent;
        border: none;
        outline: none;
        padding: 12px 16px;
        font-size: 14px;
        color: $text-primary;
        
        &::placeholder {
          color: $text-tertiary;
        }
      }
      
      .send-btn {
        width: 48px;
        height: 48px;
        border-radius: 10px;
        border: none;
        background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
        color: $bg-dark;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.3s;
        
        &:hover:not(:disabled) {
          box-shadow: 0 4px 15px rgba(212, 175, 55, 0.4);
          transform: scale(1.05);
        }
        
        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
        
        .btn-loading {
          width: 20px;
          height: 20px;
          border: 2px solid transparent;
          border-top-color: $bg-dark;
          border-radius: 50%;
          animation: spin 0.8s linear infinite;
        }
      }
    }
  }
  
  @keyframes spin {
    to { transform: rotate(360deg); }
  }
}
</style>
