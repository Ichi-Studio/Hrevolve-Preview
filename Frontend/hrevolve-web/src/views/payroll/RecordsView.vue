<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { payrollApi } from '@/api';
import type { PayrollRecord } from '@/types';

const records = ref<PayrollRecord[]>([]);
const loading = ref(false);

const fetchRecords = async () => {
  loading.value = true;
  try { const res = await payrollApi.getRecords({ page: 1, pageSize: 50 }); records.value = res.data.items; } catch { /* ignore */ } finally { loading.value = false; }
};

const formatMoney = (n: number) => new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY' }).format(n);
const getStatusType = (s: string) => ({ Draft: 'info', Calculated: 'warning', Approved: '', Paid: 'success' }[s] || 'info');

onMounted(() => fetchRecords());
</script>

<template>
  <div class="records-view">
    <el-card>
      <template #header><span>薪资记录</span></template>
      <el-table :data="records" v-loading="loading" stripe>
        <el-table-column prop="employeeName" label="员工" width="100" />
        <el-table-column prop="periodName" label="周期" width="120" />
        <el-table-column prop="baseSalary" label="基本工资" width="120"><template #default="{ row }">{{ formatMoney(row.baseSalary) }}</template></el-table-column>
        <el-table-column prop="netSalary" label="实发" width="120"><template #default="{ row }"><span style="color:#1890ff;font-weight:600">{{ formatMoney(row.netSalary) }}</span></template></el-table-column>
        <el-table-column prop="status" label="状态" width="100"><template #default="{ row }"><el-tag :type="getStatusType(row.status)" size="small">{{ row.status }}</el-tag></template></el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
