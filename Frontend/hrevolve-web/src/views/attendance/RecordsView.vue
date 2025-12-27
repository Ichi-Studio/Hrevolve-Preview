<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { attendanceApi } from '@/api';
import type { AttendanceRecord } from '@/types';
import dayjs from 'dayjs';

const records = ref<AttendanceRecord[]>([]);
const loading = ref(false);

const fetchRecords = async () => {
  loading.value = true;
  try {
    const res = await attendanceApi.getRecords({ page: 1, pageSize: 50 });
    records.value = res.data.items;
  } catch { /* ignore */ } finally { loading.value = false; }
};

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD');
const formatTime = (t: string | undefined) => t ? dayjs(t).format('HH:mm') : '-';
const getStatusType = (s: string) => ({ Normal: 'success', Late: 'warning', EarlyLeave: 'warning', Absent: 'danger' }[s] || 'info');

onMounted(() => fetchRecords());
</script>

<template>
  <div class="records-view">
    <el-card>
      <template #header><span>考勤记录</span></template>
      <el-table :data="records" v-loading="loading" stripe>
        <el-table-column prop="employeeName" label="员工" width="100" />
        <el-table-column prop="date" label="日期" width="120"><template #default="{ row }">{{ formatDate(row.date) }}</template></el-table-column>
        <el-table-column prop="checkInTime" label="签到" width="100"><template #default="{ row }">{{ formatTime(row.checkInTime) }}</template></el-table-column>
        <el-table-column prop="checkOutTime" label="签退" width="100"><template #default="{ row }">{{ formatTime(row.checkOutTime) }}</template></el-table-column>
        <el-table-column prop="status" label="状态" width="100"><template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag></template></el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
