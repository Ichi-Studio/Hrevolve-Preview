<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { leaveApi } from '@/api';
import type { LeaveRequest } from '@/types';
import dayjs from 'dayjs';

const requests = ref<LeaveRequest[]>([]);
const loading = ref(false);

const fetchRequests = async () => {
  loading.value = true;
  try { const res = await leaveApi.getRequests({ page: 1, pageSize: 50 }); requests.value = res.data.items; } catch { /* ignore */ } finally { loading.value = false; }
};

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD');
const getStatusType = (s: string) => ({ Pending: 'warning', Approved: 'success', Rejected: 'danger', Cancelled: 'info' }[s] || 'info');

onMounted(() => fetchRequests());
</script>

<template>
  <div class="requests-view">
    <el-card>
      <template #header><span>请假申请</span></template>
      <el-table :data="requests" v-loading="loading" stripe>
        <el-table-column prop="employeeName" label="员工" width="100" />
        <el-table-column prop="leaveTypeName" label="类型" width="100" />
        <el-table-column prop="startDate" label="开始" width="120"><template #default="{ row }">{{ formatDate(row.startDate) }}</template></el-table-column>
        <el-table-column prop="endDate" label="结束" width="120"><template #default="{ row }">{{ formatDate(row.endDate) }}</template></el-table-column>
        <el-table-column prop="days" label="天数" width="80" />
        <el-table-column prop="reason" label="原因" />
        <el-table-column prop="status" label="状态" width="100"><template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag></template></el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
