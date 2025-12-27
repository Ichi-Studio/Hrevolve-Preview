<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores';
import { employeeApi } from '@/api';
import type { Employee, JobHistory } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();
const authStore = useAuthStore();

const employee = ref<Employee | null>(null);
const jobHistory = ref<JobHistory[]>([]);
const loading = ref(false);
const activeTab = ref('basic');

// 获取员工信息
const fetchEmployee = async () => {
  if (!authStore.user?.employeeId) return;
  
  loading.value = true;
  try {
    const res = await employeeApi.getById(authStore.user.employeeId);
    employee.value = res.data;
  } catch {
    // 忽略错误
  } finally {
    loading.value = false;
  }
};

// 获取职位历史
const fetchJobHistory = async () => {
  if (!authStore.user?.employeeId) return;
  
  try {
    const res = await employeeApi.getJobHistory(authStore.user.employeeId);
    jobHistory.value = res.data;
  } catch {
    // 忽略错误
  }
};

// 格式化日期
const formatDate = (date: string) => {
  return dayjs(date).format('YYYY-MM-DD');
};

// 获取状态标签类型
const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    Active: 'success',
    OnLeave: 'warning',
    Terminated: 'danger',
    Probation: 'info',
  };
  return types[status] || 'info';
};

onMounted(() => {
  fetchEmployee();
  fetchJobHistory();
});
</script>

<template>
  <div class="profile-view">
    <el-card v-loading="loading">
      <!-- 头部信息 -->
      <div class="profile-header">
        <el-avatar :size="80" :src="employee?.avatar">
          {{ employee?.fullName?.charAt(0) }}
        </el-avatar>
        <div class="profile-info">
          <h2>{{ employee?.fullName }}</h2>
          <p>{{ employee?.positionName }} · {{ employee?.departmentName }}</p>
          <el-tag :type="getStatusType(employee?.status || '')">
            {{ t(`employee.status${employee?.status}`) }}
          </el-tag>
        </div>
      </div>
      
      <el-divider />
      
      <!-- 标签页 -->
      <el-tabs v-model="activeTab">
        <el-tab-pane label="基本信息" name="basic">
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="t('employee.employeeNo')">
              {{ employee?.employeeNo }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.email')">
              {{ employee?.email }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.phone')">
              {{ employee?.phone || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="性别">
              {{ employee?.gender === 'Male' ? '男' : employee?.gender === 'Female' ? '女' : '其他' }}
            </el-descriptions-item>
            <el-descriptions-item label="出生日期">
              {{ employee?.birthDate ? formatDate(employee.birthDate) : '-' }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.hireDate')">
              {{ employee?.hireDate ? formatDate(employee.hireDate) : '-' }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.department')">
              {{ employee?.departmentName }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.position')">
              {{ employee?.positionName }}
            </el-descriptions-item>
            <el-descriptions-item :label="t('employee.manager')">
              {{ employee?.managerName || '-' }}
            </el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>
        
        <el-tab-pane label="职位历史" name="history">
          <el-timeline>
            <el-timeline-item
              v-for="item in jobHistory"
              :key="item.id"
              :timestamp="formatDate(item.effectiveStartDate)"
              placement="top"
            >
              <el-card shadow="hover">
                <h4>{{ item.positionName }}</h4>
                <p>{{ item.departmentName }}</p>
                <p class="history-detail">
                  <span>生效日期: {{ formatDate(item.effectiveStartDate) }}</span>
                  <span v-if="item.effectiveEndDate !== '9999-12-31'">
                    ~ {{ formatDate(item.effectiveEndDate) }}
                  </span>
                  <span v-else>至今</span>
                </p>
                <p v-if="item.changeReason" class="history-reason">
                  变更原因: {{ item.changeReason }}
                </p>
              </el-card>
            </el-timeline-item>
          </el-timeline>
          
          <el-empty v-if="jobHistory.length === 0" description="暂无职位历史" />
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.profile-view {
  .profile-header {
    display: flex;
    align-items: center;
    gap: 24px;
    
    .profile-info {
      h2 {
        margin: 0 0 8px;
        font-size: 24px;
      }
      
      p {
        margin: 0 0 8px;
        color: #666;
      }
    }
  }
  
  .history-detail {
    font-size: 12px;
    color: #999;
    margin: 8px 0 0;
  }
  
  .history-reason {
    font-size: 12px;
    color: #666;
    margin: 4px 0 0;
  }
}
</style>
