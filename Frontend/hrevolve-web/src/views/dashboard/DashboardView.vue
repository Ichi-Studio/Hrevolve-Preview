<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import {
  Clock,
  Calendar,
  Document,
  User,
  Check,
  Close,
} from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores';
import { attendanceApi, leaveApi } from '@/api';
import type { AttendanceRecord, LeaveBalance } from '@/types';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();

// 数据
const todayAttendance = ref<AttendanceRecord | null>(null);
const leaveBalances = ref<LeaveBalance[]>([]);
const loading = ref(false);

// 统计卡片数据
const stats = ref([
  { title: t('dashboard.todayAttendance'), value: '--', icon: Clock, color: '#1890ff' },
  { title: t('dashboard.leaveBalance'), value: '--', icon: Calendar, color: '#52c41a' },
  { title: t('dashboard.pendingApprovals'), value: '3', icon: Document, color: '#faad14' },
  { title: t('dashboard.teamMembers'), value: '12', icon: User, color: '#722ed1' },
]);

// 获取今日考勤
const fetchTodayAttendance = async () => {
  try {
    const res = await attendanceApi.getTodayStatus();
    todayAttendance.value = res.data;
    
    if (res.data?.checkInTime) {
      stats.value[0].value = t('dashboard.checked');
    } else {
      stats.value[0].value = t('dashboard.notChecked');
    }
  } catch {
    // 忽略错误
  }
};

// 获取假期余额
const fetchLeaveBalances = async () => {
  try {
    const res = await leaveApi.getMyBalances();
    leaveBalances.value = res.data;
    
    // 计算年假余额
    const annualLeave = res.data.find(b => b.leaveTypeName.includes('年假'));
    if (annualLeave) {
      stats.value[1].value = `${annualLeave.remainingDays}天`;
    }
  } catch {
    // 忽略错误
  }
};

// 签到
const handleCheckIn = async () => {
  loading.value = true;
  try {
    await attendanceApi.checkIn({});
    ElMessage.success('签到成功');
    await fetchTodayAttendance();
  } catch {
    ElMessage.error('签到失败');
  } finally {
    loading.value = false;
  }
};

// 签退
const handleCheckOut = async () => {
  loading.value = true;
  try {
    await attendanceApi.checkOut({});
    ElMessage.success('签退成功');
    await fetchTodayAttendance();
  } catch {
    ElMessage.error('签退失败');
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchTodayAttendance();
  fetchLeaveBalances();
});
</script>

<template>
  <div class="dashboard">
    <!-- 欢迎区域 -->
    <div class="welcome-section">
      <div class="welcome-text">
        <h1>{{ t('dashboard.welcome') }}，{{ authStore.user?.displayName }} 👋</h1>
        <p>{{ new Date().toLocaleDateString('zh-CN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }) }}</p>
      </div>
      
      <div class="quick-actions">
        <el-button
          v-if="!todayAttendance?.checkInTime"
          type="primary"
          :icon="Check"
          :loading="loading"
          @click="handleCheckIn"
        >
          {{ t('dashboard.checkIn') }}
        </el-button>
        <el-button
          v-else-if="!todayAttendance?.checkOutTime"
          type="success"
          :icon="Close"
          :loading="loading"
          @click="handleCheckOut"
        >
          {{ t('dashboard.checkOut') }}
        </el-button>
        <el-tag v-else type="success" size="large">
          今日已完成打卡
        </el-tag>
      </div>
    </div>
    
    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stats-row">
      <el-col v-for="stat in stats" :key="stat.title" :xs="24" :sm="12" :lg="6">
        <el-card class="stat-card" shadow="hover">
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-title">{{ stat.title }}</p>
              <p class="stat-value">{{ stat.value }}</p>
            </div>
            <div class="stat-icon" :style="{ backgroundColor: stat.color + '20', color: stat.color }">
              <el-icon :size="24"><component :is="stat.icon" /></el-icon>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    
    <!-- 主要内容区 -->
    <el-row :gutter="24">
      <!-- 假期余额 -->
      <el-col :xs="24" :lg="12">
        <el-card class="content-card">
          <template #header>
            <div class="card-header">
              <span>{{ t('dashboard.leaveBalance') }}</span>
              <el-button text type="primary" @click="router.push('/self-service/leave')">
                查看全部
              </el-button>
            </div>
          </template>
          
          <div v-if="leaveBalances.length" class="leave-balances">
            <div v-for="balance in leaveBalances" :key="balance.leaveTypeId" class="balance-item">
              <div class="balance-info">
                <span class="balance-name">{{ balance.leaveTypeName }}</span>
                <span class="balance-detail">
                  {{ t('leave.used') }}: {{ balance.usedDays }}天 / 
                  {{ t('leave.remaining') }}: {{ balance.remainingDays }}天
                </span>
              </div>
              <el-progress
                :percentage="(balance.usedDays / balance.totalDays) * 100"
                :stroke-width="8"
                :show-text="false"
              />
            </div>
          </div>
          <el-empty v-else description="暂无假期数据" />
        </el-card>
      </el-col>
      
      <!-- 快捷入口 -->
      <el-col :xs="24" :lg="12">
        <el-card class="content-card">
          <template #header>
            <span>快捷入口</span>
          </template>
          
          <div class="quick-links">
            <div class="quick-link" @click="router.push('/self-service/leave')">
              <el-icon :size="32" color="#1890ff"><Calendar /></el-icon>
              <span>请假申请</span>
            </div>
            <div class="quick-link" @click="router.push('/self-service/attendance')">
              <el-icon :size="32" color="#52c41a"><Clock /></el-icon>
              <span>考勤记录</span>
            </div>
            <div class="quick-link" @click="router.push('/self-service/payroll')">
              <el-icon :size="32" color="#faad14"><Document /></el-icon>
              <span>薪资查询</span>
            </div>
            <div class="quick-link" @click="router.push('/assistant')">
              <el-icon :size="32" color="#722ed1"><User /></el-icon>
              <span>AI助手</span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<style scoped lang="scss">
.dashboard {
  .welcome-section {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24px;
    padding: 24px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 12px;
    color: #fff;
    
    .welcome-text {
      h1 {
        margin: 0 0 8px;
        font-size: 24px;
        font-weight: 600;
      }
      
      p {
        margin: 0;
        opacity: 0.9;
      }
    }
  }
  
  .stats-row {
    margin-bottom: 24px;
  }
  
  .stat-card {
    .stat-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      
      .stat-info {
        .stat-title {
          margin: 0 0 8px;
          font-size: 14px;
          color: #666;
        }
        
        .stat-value {
          margin: 0;
          font-size: 24px;
          font-weight: 600;
          color: #1a1a1a;
        }
      }
      
      .stat-icon {
        width: 48px;
        height: 48px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
      }
    }
  }
  
  .content-card {
    margin-bottom: 24px;
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
  }
  
  .leave-balances {
    .balance-item {
      margin-bottom: 16px;
      
      &:last-child {
        margin-bottom: 0;
      }
      
      .balance-info {
        display: flex;
        justify-content: space-between;
        margin-bottom: 8px;
        
        .balance-name {
          font-weight: 500;
        }
        
        .balance-detail {
          font-size: 12px;
          color: #666;
        }
      }
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
      gap: 8px;
      padding: 16px;
      border-radius: 8px;
      cursor: pointer;
      transition: background-color 0.2s;
      
      &:hover {
        background-color: #f5f5f5;
      }
      
      span {
        font-size: 14px;
        color: #333;
      }
    }
  }
}

@media (max-width: 768px) {
  .dashboard {
    .welcome-section {
      flex-direction: column;
      text-align: center;
      gap: 16px;
    }
    
    .quick-links {
      grid-template-columns: repeat(2, 1fr);
    }
  }
}
</style>
