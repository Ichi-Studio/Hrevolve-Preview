<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { payrollApi } from '@/api';
import type { PayrollRecord } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();

const records = ref<PayrollRecord[]>([]);
const loading = ref(false);
const selectedYear = ref(dayjs().year());
const detailVisible = ref(false);
const selectedRecord = ref<PayrollRecord | null>(null);

// 获取薪资记录
const fetchRecords = async () => {
  loading.value = true;
  try {
    const res = await payrollApi.getMyRecords({ page: 1, pageSize: 12, year: selectedYear.value });
    records.value = res.data.items;
  } catch {
    // 忽略错误
  } finally {
    loading.value = false;
  }
};

// 查看详情
const showDetail = (record: PayrollRecord) => {
  selectedRecord.value = record;
  detailVisible.value = true;
};

// 格式化金额
const formatMoney = (amount: number) => {
  return new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY' }).format(amount);
};

// 获取状态标签类型
const getStatusType = (status: string) => {
  const types: Record<string, string> = { Draft: 'info', Calculated: 'warning', Approved: '', Paid: 'success' };
  return types[status] || 'info';
};

onMounted(() => { fetchRecords(); });
</script>

<template>
  <div class="payroll-view">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>薪资记录</span>
          <el-select v-model="selectedYear" @change="fetchRecords" style="width: 120px">
            <el-option v-for="y in [2024, 2025]" :key="y" :label="`${y}年`" :value="y" />
          </el-select>
        </div>
      </template>
      <el-table :data="records" v-loading="loading" stripe>
        <el-table-column prop="periodName" :label="t('payroll.period')" width="120" />
        <el-table-column prop="baseSalary" :label="t('payroll.baseSalary')" width="120">
          <template #default="{ row }">{{ formatMoney(row.baseSalary) }}</template>
        </el-table-column>
        <el-table-column prop="bonus" :label="t('payroll.bonus')" width="100">
          <template #default="{ row }">{{ formatMoney(row.bonus) }}</template>
        </el-table-column>
        <el-table-column prop="deductions" label="扣款" width="100">
          <template #default="{ row }">{{ formatMoney(row.deductions) }}</template>
        </el-table-column>
        <el-table-column prop="tax" :label="t('payroll.tax')" width="100">
          <template #default="{ row }">{{ formatMoney(row.tax) }}</template>
        </el-table-column>
        <el-table-column prop="netSalary" :label="t('payroll.netSalary')" width="120">
          <template #default="{ row }">
            <span style="color: #1890ff; font-weight: 600">{{ formatMoney(row.netSalary) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('common.status')" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ t(`payroll.status${row.status}`) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="80">
          <template #default="{ row }">
            <el-button text type="primary" size="small" @click="showDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
    <el-dialog v-model="detailVisible" title="薪资详情" width="500px">
      <el-descriptions v-if="selectedRecord" :column="1" border>
        <el-descriptions-item :label="t('payroll.baseSalary')">{{ formatMoney(selectedRecord.baseSalary) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.bonus')">{{ formatMoney(selectedRecord.bonus) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.allowances')">{{ formatMoney(selectedRecord.allowances) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.deductions')">{{ formatMoney(selectedRecord.deductions) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.socialInsurance')">{{ formatMoney(selectedRecord.socialInsurance) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.housingFund')">{{ formatMoney(selectedRecord.housingFund) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.tax')">{{ formatMoney(selectedRecord.tax) }}</el-descriptions-item>
        <el-descriptions-item :label="t('payroll.netSalary')">
          <span style="color: #1890ff; font-weight: 600; font-size: 18px">{{ formatMoney(selectedRecord.netSalary) }}</span>
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
.payroll-view { .card-header { display: flex; justify-content: space-between; align-items: center; } }
</style>
