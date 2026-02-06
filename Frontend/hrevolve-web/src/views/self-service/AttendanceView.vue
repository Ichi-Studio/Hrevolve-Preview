<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { attendanceApi } from '@/api';
import type { AttendanceRecord } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();

const records = ref<AttendanceRecord[]>([]);
const loading = ref(false);
const dateRange = ref<[Date, Date] | null>([
  dayjs().startOf('month').toDate(),
  dayjs().endOf('month').toDate(),
]);

const getSafeRange = (): [Date, Date] => {
  const fallback: [Date, Date] = [
    dayjs().startOf('month').toDate(),
    dayjs().endOf('month').toDate(),
  ];

  const value = dateRange.value;
  if (!value || value.length !== 2 || !value[0] || !value[1]) {
    dateRange.value = fallback;
    return fallback;
  }
  return value;
};

// 月度统计
const monthlyStats = ref({
  workDays: 0,
  attendedDays: 0,
  lateDays: 0,
  earlyLeaveDays: 0,
  absentDays: 0,
  leaveDays: 0,
  overtimeHours: 0,
});

// 获取考勤记录
const fetchRecords = async () => {
  loading.value = true;
  const [start, end] = getSafeRange();
  try {
    const res = await attendanceApi.getMyRecords({
      page: 1,
      pageSize: 100,
      startDate: dayjs(start).format('YYYY-MM-DD'),
      endDate: dayjs(end).format('YYYY-MM-DD'),
    });
    records.value = res.data.items;
  } catch {
    ElMessage.error(t('common.failed'));
  } finally {
    loading.value = false;
  }
};

// 获取月度统计
const fetchStats = async () => {
  const [start] = getSafeRange();
  try {
    const res = await attendanceApi.getMonthlyStats(
      dayjs(start).year(),
      dayjs(start).month() + 1
    );
    monthlyStats.value = res.data;
  } catch {
    ElMessage.error(t('common.failed'));
  }
};

// 格式化日期
const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD');
const formatTime = (time: string | undefined) => time ? dayjs(time).format('HH:mm') : '-';

// 获取状态标签类型
const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    Normal: 'success',
    Late: 'warning',
    EarlyLeave: 'warning',
    Absent: 'danger',
    Leave: 'info',
    Holiday: '',
  };
  return types[status] || 'info';
};

// 日期变化
const handleDateChange = () => {
  getSafeRange();
  fetchRecords();
  fetchStats();
};

onMounted(() => {
  fetchRecords();
  fetchStats();
});
</script>

<template>
  <div class="attendance-view">
    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stats-row">
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.workDays')" :value="monthlyStats.workDays" :suffix="t('attendance.stats.daysUnit')" />
      </el-col>
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.attendedDays')" :value="monthlyStats.attendedDays" :suffix="t('attendance.stats.daysUnit')" />
      </el-col>
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.lateDays')" :value="monthlyStats.lateDays" :suffix="t('attendance.stats.timesUnit')" />
      </el-col>
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.earlyLeaveDays')" :value="monthlyStats.earlyLeaveDays" :suffix="t('attendance.stats.timesUnit')" />
      </el-col>
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.leaveDays')" :value="monthlyStats.leaveDays" :suffix="t('attendance.stats.daysUnit')" />
      </el-col>
      <el-col :span="4">
        <el-statistic :title="t('attendance.stats.overtimeHours')" :value="monthlyStats.overtimeHours" :suffix="t('attendance.stats.hoursUnit')" />
      </el-col>
    </el-row>
    
    <!-- 筛选 -->
    <el-card class="filter-card">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        :range-separator="t('attendance.datePicker.to')"
        :start-placeholder="t('attendance.datePicker.startDate')"
        :end-placeholder="t('attendance.datePicker.endDate')"
        @change="handleDateChange"
      />
    </el-card>
    
    <!-- 记录列表 -->
    <el-card>
      <el-table :data="records" v-loading="loading" stripe>
        <template #empty>
          <el-empty :description="t('common.noData')" />
        </template>
        <el-table-column prop="date" :label="t('attendance.date')" width="120">
          <template #default="{ row }">
            {{ formatDate(row.date) }}
          </template>
        </el-table-column>
        <el-table-column prop="shiftName" :label="t('attendance.shift')" width="100" />
        <el-table-column prop="checkInTime" :label="t('attendance.checkInTime')" width="100">
          <template #default="{ row }">
            {{ formatTime(row.checkInTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="checkOutTime" :label="t('attendance.checkOutTime')" width="100">
          <template #default="{ row }">
            {{ formatTime(row.checkOutTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="workHours" :label="t('attendance.workHours')" width="100">
          <template #default="{ row }">
            {{ row.workHours ? `${row.workHours}h` : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="overtimeHours" :label="t('attendance.overtimeHours')" width="100">
          <template #default="{ row }">
            {{ row.overtimeHours ? `${row.overtimeHours}h` : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('common.status')" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ t(`attendance.status${row.status}`) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" :label="t('common.remark')" />
      </el-table>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.attendance-view {
  .stats-row {
    margin-bottom: 16px;
    
    .el-col {
      background: var(--el-bg-color-overlay);
      border: 1px solid var(--el-border-color-lighter);
      padding: 16px;
      border-radius: 8px;
    }
  }
  
  .filter-card {
    margin-bottom: 16px;
  }
}
</style>
