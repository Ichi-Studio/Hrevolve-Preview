<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { attendanceApi } from '@/api';
import type { AttendanceRecord } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();
const records = ref<AttendanceRecord[]>([]);
const loading = ref(false);

const fetchRecords = async () => {
  loading.value = true;
  try {
    const res = await attendanceApi.getRecords({ page: 1, pageSize: 50 });
    records.value = res.data.items;
  } catch {
    ElMessage.error(t('common.failed'));
  } finally { loading.value = false; }
};

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD');
const formatTime = (t: string | undefined) => t ? dayjs(t).format('HH:mm') : '-';
const getStatusType = (s: string) => ({ Normal: 'success', Late: 'warning', EarlyLeave: 'warning', Absent: 'danger' }[s] || 'info');

// 状态标签映射 - 使用 computed 实现响应式翻译
const statusLabels = computed(() => ({
  Normal: t('attendance.statusNormal'),
  Late: t('attendance.statusLate'),
  EarlyLeave: t('attendance.statusEarlyLeave'),
  Absent: t('attendance.statusAbsent'),
  Leave: t('attendance.statusLeave'),
  Holiday: t('attendance.statusHoliday'),
} as Record<string, string>));

onMounted(() => fetchRecords());
</script>

<template>
  <div class="records-view">
    <el-card>
      <template #header><span>{{ t('attendanceAdmin.records') }}</span></template>
      <el-table :data="records" v-loading="loading" stripe>
        <template #empty>
          <el-empty :description="t('common.noData')" />
        </template>
        <el-table-column prop="employeeName" :label="t('schedule.employee')" width="100" />
        <el-table-column prop="date" :label="t('attendance.date')" width="120"><template #default="{ row }">{{ formatDate(row.date) }}</template></el-table-column>
        <el-table-column prop="checkInTime" :label="t('attendanceAdmin.checkIn')" width="100"><template #default="{ row }">{{ formatTime(row.checkInTime) }}</template></el-table-column>
        <el-table-column prop="checkOutTime" :label="t('attendanceAdmin.checkOut')" width="100"><template #default="{ row }">{{ formatTime(row.checkOutTime) }}</template></el-table-column>
        <el-table-column prop="status" :label="t('common.status')" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ statusLabels[row.status] || row.status }}</el-tag>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
