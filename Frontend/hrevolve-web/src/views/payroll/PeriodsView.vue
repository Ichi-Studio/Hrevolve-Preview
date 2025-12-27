<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { payrollApi } from '@/api';
import type { PayrollPeriod } from '@/types';
import dayjs from 'dayjs';

const periods = ref<PayrollPeriod[]>([]);
const loading = ref(false);

const fetchPeriods = async () => {
  loading.value = true;
  try { const res = await payrollApi.getPeriods(); periods.value = res.data; } catch { /* ignore */ } finally { loading.value = false; }
};

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD');
const getStatusType = (s: string) => ({ Open: 'success', Closed: 'info', Locked: 'danger' }[s] || 'info');

onMounted(() => fetchPeriods());
</script>

<template>
  <div class="periods-view">
    <el-card>
      <template #header><span>薪资周期</span></template>
      <el-table :data="periods" v-loading="loading" stripe>
        <el-table-column prop="name" label="名称" width="150" />
        <el-table-column prop="year" label="年份" width="80" />
        <el-table-column prop="month" label="月份" width="80" />
        <el-table-column prop="startDate" label="开始" width="120"><template #default="{ row }">{{ formatDate(row.startDate) }}</template></el-table-column>
        <el-table-column prop="endDate" label="结束" width="120"><template #default="{ row }">{{ formatDate(row.endDate) }}</template></el-table-column>
        <el-table-column prop="status" label="状态" width="100"><template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag></template></el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
