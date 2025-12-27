<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { attendanceApi } from '@/api';
import type { AttendanceRecord } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();

const records = ref<AttendanceRecord[]>([]);
const loading = ref(false);
const dateRange = ref<[Date, Date]>([
  dayjs().startOf('month').toDate(),
  dayjs().endOf('month').toDate(),
]);

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
  try {
    const res = await attendanceApi.getMyRecords({
      page: 1,
      pageSize: 100,
      startDate: dayjs(dateRange.value[0]).format('YYYY-MM-DD'),
      endDate: dayjs(dateRange.value[1]).format('YYYY-MM-DD'),
    });
    records.value = res.data.items;
  } catch {
    // 忽略错误
  } finally {
    loading.value = false;
  }
};

// 获取月度统计
const fetchStats = async () => {
  try {
    const res = await attendanceApi.getMonthlyStats(
      dayjs(dateRange.value[0]).year(),
      dayjs(dateRange.value[0]).month() + 1
    );
    monthlyStats.value = res.data;
  } catch {
    // 忽略错误
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
        <el-statistic title="应出勤" :value="monthlyStats.workDays" suffix="天" />
      </el-col>
      <el-col :span="4">
        <el-statistic title="实出勤" :value="monthlyStats.attendedDays" suffix="天" />
      </el-col>
      <el-col :span="4">
        <el-statistic title="迟到" :value="monthlyStats.lateDays" suffix="次" />
      </el-col>
      <el-col :span="4">
        <el-statistic title="早退" :value="monthlyStats.earlyLeaveDays" suffix="次" />
      </el-col>
      <el-col :span="4">
        <el-statistic title="请假" :value="monthlyStats.leaveDays" suffix="天" />
      </el-col>
      <el-col :span="4">
        <el-statistic title="加班" :value="monthlyStats.overtimeHours" suffix="小时" />
      </el-col>
    </el-row>
    
    <!-- 筛选 -->
    <el-card class="filter-card">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        @change="handleDateChange"
      />
    </el-card>
    
    <!-- 记录列表 -->
    <el-card>
      <el-table :data="records" v-loading="loading" stripe>
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
      background: #fff;
      padding: 16px;
      border-radius: 8px;
    }
  }
  
  .filter-card {
    margin-bottom: 16px;
  }
}
</style>
