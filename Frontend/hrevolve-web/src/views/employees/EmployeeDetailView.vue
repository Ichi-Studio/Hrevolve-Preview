<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { employeeApi } from '@/api';
import type { Employee, JobHistory } from '@/types';
import dayjs from 'dayjs';

const route = useRoute();
const { t } = useI18n();

const employee = ref<Employee | null>(null);
const jobHistory = ref<JobHistory[]>([]);
const loading = ref(false);

const fetchEmployee = async () => {
  loading.value = true;
  try {
    const id = route.params.id as string;
    const [empRes, histRes] = await Promise.all([employeeApi.getById(id), employeeApi.getJobHistory(id)]);
    employee.value = empRes.data;
    jobHistory.value = histRes.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD');
const getStatusType = (status: string) => ({ Active: 'success', OnLeave: 'warning', Terminated: 'danger', Probation: 'info' }[status] || 'info');

onMounted(() => fetchEmployee());
</script>

<template>
  <div class="employee-detail" v-loading="loading">
    <el-card v-if="employee">
      <div class="header">
        <el-avatar :size="64">{{ employee.fullName?.charAt(0) }}</el-avatar>
        <div class="info">
          <h2>{{ employee.fullName }}</h2>
          <p>{{ employee.positionName }} · {{ employee.departmentName }}</p>
          <el-tag :type="getStatusType(employee.status)">{{ t(`employee.status${employee.status}`) }}</el-tag>
        </div>
      </div>
      <el-divider />
      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('employee.employeeNo')">{{ employee.employeeNo }}</el-descriptions-item>
        <el-descriptions-item :label="t('employee.email')">{{ employee.email }}</el-descriptions-item>
        <el-descriptions-item :label="t('employee.phone')">{{ employee.phone || '-' }}</el-descriptions-item>
        <el-descriptions-item :label="t('employee.hireDate')">{{ formatDate(employee.hireDate) }}</el-descriptions-item>
        <el-descriptions-item :label="t('employee.manager')">{{ employee.managerName || '-' }}</el-descriptions-item>
      </el-descriptions>
      <h3 style="margin-top: 24px">职位历史</h3>
      <el-timeline>
        <el-timeline-item v-for="item in jobHistory" :key="item.id" :timestamp="formatDate(item.effectiveStartDate)">
          <strong>{{ item.positionName }}</strong> - {{ item.departmentName }}
        </el-timeline-item>
      </el-timeline>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.employee-detail { .header { display: flex; gap: 16px; align-items: center; h2 { margin: 0 0 8px; } p { margin: 0 0 8px; color: #666; } } }
</style>
