<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { leaveApi } from '@/api';
import type { LeaveRequest } from '@/types';
import dayjs from 'dayjs';

const requests = ref<LeaveRequest[]>([]);
const loading = ref(false);

const fetchRequests = async () => {
  loading.value = true;
  try { const res = await leaveApi.getPendingApprovals({ page: 1, pageSize: 50 }); requests.value = res.data.items; } catch { /* ignore */ } finally { loading.value = false; }
};

const handleApprove = async (id: string, approved: boolean) => {
  try { await leaveApi.approveRequest(id, { approved }); ElMessage.success(approved ? '已批准' : '已拒绝'); fetchRequests(); } catch { ElMessage.error('操作失败'); }
};

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD');

onMounted(() => fetchRequests());
</script>

<template>
  <div class="approvals-view">
    <el-card>
      <template #header><span>待审批</span></template>
      <el-table :data="requests" v-loading="loading" stripe>
        <el-table-column prop="employeeName" label="员工" width="100" />
        <el-table-column prop="leaveTypeName" label="类型" width="100" />
        <el-table-column prop="startDate" label="开始" width="120"><template #default="{ row }">{{ formatDate(row.startDate) }}</template></el-table-column>
        <el-table-column prop="endDate" label="结束" width="120"><template #default="{ row }">{{ formatDate(row.endDate) }}</template></el-table-column>
        <el-table-column prop="days" label="天数" width="80" />
        <el-table-column prop="reason" label="原因" />
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button type="success" size="small" @click="handleApprove(row.id, true)">批准</el-button>
            <el-button type="danger" size="small" @click="handleApprove(row.id, false)">拒绝</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="!loading && requests.length === 0" description="暂无待审批申请" />
    </el-card>
  </div>
</template>
