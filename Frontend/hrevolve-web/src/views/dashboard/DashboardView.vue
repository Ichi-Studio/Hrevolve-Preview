<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import {
  Clock,
  Calendar,
  Document,
  User,
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();

// 统计卡片数据
const stats = ref([
  { title: t('dashboard.todayAttendance'), value: '--', icon: Clock, color: '#D4AF37' },
  { title: t('dashboard.leaveBalance'), value: '--', icon: Calendar, color: '#52c41a' },
  { title: t('dashboard.pendingApprovals'), value: '3', icon: Document, color: '#faad14' },
  { title: t('dashboard.teamMembers'), value: '12', icon: User, color: '#722ed1' },
]);
</script>

<template>
  <div class="dashboard">
    <!-- 欢迎区域 -->
    <div class="welcome-section">
      <div class="welcome-content">
        <div class="welcome-text">
          <h1>{{ t('dashboard.welcome') }}，{{ authStore.user?.username || 'User' }} 👋</h1>
          <p>{{ new Date().toLocaleDateString('zh-CN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }) }}</p>
        </div>
      </div>
      
      <!-- 装饰元素 -->
      <div class="welcome-decoration">
        <div class="deco-circle"></div>
        <div class="deco-circle small"></div>
      </div>
    </div>
    
    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stats-row">
      <el-col v-for="(stat, index) in stats" :key="stat.title" :xs="24" :sm="12" :lg="6">
        <div class="stat-card" :style="{ '--delay': index * 0.1 + 's' }">
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-title">{{ stat.title }}</p>
              <p class="stat-value">{{ stat.value }}</p>
            </div>
            <div class="stat-icon" :style="{ '--icon-color': stat.color }">
              <el-icon :size="24"><component :is="stat.icon" /></el-icon>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
    
    <!-- 快捷入口 -->
    <el-row :gutter="24">
      <el-col :span="24">
        <div class="content-card">
          <div class="card-header">
            <span class="card-title">快捷入口</span>
          </div>
          
          <div class="quick-links">
            <div class="quick-link" @click="router.push('/self-service/leave')">
              <div class="link-icon" style="--link-color: #D4AF37">
                <el-icon :size="28"><Calendar /></el-icon>
              </div>
              <span>请假申请</span>
            </div>
            <div class="quick-link" @click="router.push('/self-service/attendance')">
              <div class="link-icon" style="--link-color: #52c41a">
                <el-icon :size="28"><Clock /></el-icon>
              </div>
              <span>考勤记录</span>
            </div>
            <div class="quick-link" @click="router.push('/self-service/payroll')">
              <div class="link-icon" style="--link-color: #faad14">
                <el-icon :size="28"><Document /></el-icon>
              </div>
              <span>薪资查询</span>
            </div>
            <div class="quick-link" @click="router.push('/assistant')">
              <div class="link-icon" style="--link-color: #722ed1">
                <el-icon :size="28"><User /></el-icon>
              </div>
              <span>AI助手</span>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped lang="scss">
// 黑金主题变量
$gold-primary: #D4AF37;
$gold-light: #F4D03F;
$gold-dark: #B8860B;
$bg-dark: #0D0D0D;
$bg-card: #1A1A1A;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);
$text-tertiary: rgba(255, 255, 255, 0.65);
$border-color: rgba(212, 175, 55, 0.2);

.dashboard {
  .welcome-section {
    position: relative;
    margin-bottom: 24px;
    padding: 32px;
    background: linear-gradient(135deg, rgba(212, 175, 55, 0.15) 0%, rgba(184, 134, 11, 0.1) 50%, rgba(13, 13, 13, 0.95) 100%);
    border: 1px solid $border-color;
    border-radius: 16px;
    overflow: hidden;
    
    .welcome-content {
      position: relative;
      z-index: 1;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    
    .welcome-text {
      h1 {
        margin: 0 0 8px;
        font-size: 28px;
        font-weight: 700;
        background: linear-gradient(135deg, $gold-primary 0%, $gold-light 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }
      
      p {
        margin: 0;
        color: $text-tertiary;
        font-size: 14px;
      }
    }
    
    .quick-actions {
      .action-btn {
        height: 44px;
        padding: 0 28px;
        font-size: 15px;
        font-weight: 600;
        border-radius: 10px;
        border: none;
        
        &.check-in {
          background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
          color: $bg-dark;
          
          &:hover {
            box-shadow: 0 8px 25px rgba(212, 175, 55, 0.4);
            transform: translateY(-2px);
          }
        }
        
        &.check-out {
          background: linear-gradient(135deg, #52c41a 0%, #73d13d 100%);
          color: #fff;
          
          &:hover {
            box-shadow: 0 8px 25px rgba(82, 196, 26, 0.4);
            transform: translateY(-2px);
          }
        }
      }
      
      .completed-tag {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 12px 24px;
        background: rgba(82, 196, 26, 0.15);
        border: 1px solid rgba(82, 196, 26, 0.3);
        border-radius: 10px;
        color: #52c41a;
        font-weight: 500;
        
        .tag-icon {
          font-size: 18px;
        }
      }
    }
    
    .welcome-decoration {
      position: absolute;
      right: -50px;
      top: -50px;
      
      .deco-circle {
        width: 200px;
        height: 200px;
        border-radius: 50%;
        background: radial-gradient(circle, rgba(212, 175, 55, 0.2) 0%, transparent 70%);
        
        &.small {
          position: absolute;
          width: 100px;
          height: 100px;
          right: 80px;
          top: 100px;
        }
      }
    }
  }
  
  .stats-row {
    margin-bottom: 24px;
  }
  
  .stat-card {
    position: relative;
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 12px;
    padding: 20px;
    margin-bottom: 24px;
    overflow: hidden;
    transition: all 0.3s;
    animation: fadeInUp 0.5s ease forwards;
    animation-delay: var(--delay);
    opacity: 0;
    
    &:hover {
      border-color: rgba(212, 175, 55, 0.4);
      transform: translateY(-4px);
      
      .stat-glow {
        opacity: 1;
      }
    }
    
    .stat-content {
      position: relative;
      z-index: 1;
      display: flex;
      justify-content: space-between;
      align-items: center;
      
      .stat-info {
        .stat-title {
          margin: 0 0 8px;
          font-size: 14px;
          color: $text-tertiary;
        }
        
        .stat-value {
          margin: 0;
          font-size: 28px;
          font-weight: 700;
          color: $text-primary;
        }
      }
      
      .stat-icon {
        width: 52px;
        height: 52px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: linear-gradient(135deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
        color: var(--icon-color, $gold-primary);
      }
    }
  }
  
  @keyframes fadeInUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  .content-card {
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 12px;
    padding: 24px;
    margin-bottom: 24px;
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      padding-bottom: 16px;
      border-bottom: 1px solid $border-color;
      
      .card-title {
        font-size: 16px;
        font-weight: 600;
        color: $text-primary;
        position: relative;
        padding-left: 12px;
        
        &::before {
          content: '';
          position: absolute;
          left: 0;
          top: 50%;
          transform: translateY(-50%);
          width: 3px;
          height: 16px;
          background: linear-gradient(180deg, $gold-primary 0%, $gold-dark 100%);
          border-radius: 2px;
        }
      }
      
      .view-all-btn {
        color: $gold-primary;
        
        &:hover {
          color: $gold-light;
        }
      }
    }
  }
  
  .leave-balances {
    .balance-item {
      margin-bottom: 20px;
      
      &:last-child {
        margin-bottom: 0;
      }
      
      .balance-info {
        display: flex;
        justify-content: space-between;
        margin-bottom: 10px;
        
        .balance-name {
          font-weight: 500;
          color: $text-primary;
        }
        
        .balance-detail {
          font-size: 12px;
          color: $text-tertiary;
        }
      }
      
      .progress-bar {
        height: 6px;
        background: rgba(255, 255, 255, 0.1);
        border-radius: 3px;
        overflow: hidden;
        
        .progress-fill {
          height: 100%;
          background: linear-gradient(90deg, $gold-primary 0%, $gold-light 100%);
          border-radius: 3px;
          transition: width 0.5s ease;
        }
      }
    }
  }
  
  .empty-state {
    text-align: center;
    padding: 40px 20px;
    
    .empty-icon {
      font-size: 48px;
      display: block;
      margin-bottom: 12px;
    }
    
    p {
      margin: 0;
      color: $text-tertiary;
    }
  }
  
  .quick-links {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 16px;
    
    .quick-link {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 20px 16px;
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.3s;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid transparent;
      
      &:hover {
        background: rgba(212, 175, 55, 0.08);
        border-color: $border-color;
        transform: translateY(-4px);
        
        .link-icon {
          transform: scale(1.1);
          box-shadow: 0 8px 20px rgba(212, 175, 55, 0.2);
        }
      }
      
      .link-icon {
        width: 56px;
        height: 56px;
        border-radius: 14px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: linear-gradient(135deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
        color: var(--link-color, $gold-primary);
        transition: all 0.3s;
      }
      
      span {
        font-size: 14px;
        color: $text-secondary;
        font-weight: 500;
      }
    }
  }
}

@media (max-width: 768px) {
  .dashboard {
    .welcome-section {
      .welcome-content {
        flex-direction: column;
        text-align: center;
        gap: 20px;
      }
    }
    
    .quick-links {
      grid-template-columns: repeat(2, 1fr);
    }
  }
}
</style>
