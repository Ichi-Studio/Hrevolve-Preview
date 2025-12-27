<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Search, Plus } from '@element-plus/icons-vue';
import { employeeApi } from '@/api';
import type { Employee } from '@/types';

const router = useRouter();
const { t } = useI18n();

const employees = ref<Employee[]>([]);
const loading = ref(false);
const keyword = ref('');
const pagination = ref({ page: 1, pageSize: 20, total: 0 });

const fetchEmployees = async () => {
  loading.value = true;
  try {
    const res = await employeeApi.getList({ ...pagination.value, keyword: keyword.value });
    employees.value = res.data.items;
    pagination.value.total = res.data.total;
  } catch { /* ignore */ } finally { loading.value = false; }
};

const handleSearch = () => { pagination.value.page = 1; fetchEmployees(); };
const handlePageChange = (page: number) => { pagination.value.page = page; fetchEmployees(); };
const viewDetail = (id: string) => router.push(`/employees/${id}`);

const getStatusType = (status: string) => {
  const types: Record<string, string> = { Active: 'success', OnLeave: 'warning', Terminated: 'danger', Probation: 'info' };
  return types[status] || 'info';
};

onMounted(() => fetchEmployees());
</script>

<template>
  <div class="employee-list">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-input v-model="keyword" :placeholder="t('common.search')" :prefix-icon="Search" style="width: 300px" @keyup.enter="handleSearch" />
          <el-button type="primary" :icon="Plus">{{ t('common.add') }}</el-button>
        </div>
      </template>
      <el-table :data="employees" v-loading="loading" stripe @row-click="(row: Employee) => viewDetail(row.id)">
        <el-table-column prop="employeeNo" :label="t('employee.employeeNo')" width="100" />
        <el-table-column prop="fullName" :label="t('employee.name')" width="120" />
        <el-table-column prop="email" :label="t('employee.email')" width="200" />
        <el-table-column prop="departmentName" :label="t('employee.department')" width="150" />
        <el-table-column prop="positionName" :label="t('employee.position')" width="150" />
        <el-table-column prop="status" :label="t('employee.status')" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">{{ t(`employee.status${row.status}`) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="hireDate" :label="t('employee.hireDate')" width="120" />
      </el-table>
      <el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="handlePageChange" style="margin-top: 16px" />
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.employee-list { .card-header { display: flex; justify-content: space-between; } }
</style>
