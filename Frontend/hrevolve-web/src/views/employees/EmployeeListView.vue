<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { Search, Plus } from '@element-plus/icons-vue';
import { employeeApi } from '@/api';
import type { Employee, JobHistory } from '@/types';
import dayjs from 'dayjs';

const { t } = useI18n();

const employees = ref<Employee[]>([]);
const loading = ref(false);
const keyword = ref('');
const pagination = ref({ page: 1, pageSize: 20, total: 0 });

// Detail panel state
const selectedEmployeeId = ref<string | null>(null);
const selectedEmployee = ref<Employee | null>(null);
const selectedJobHistory = ref<JobHistory[]>([]);
const detailLoading = ref(false);

const fetchEmployees = async () => {
  loading.value = true;
  try {
    const res = await employeeApi.getList({ ...pagination.value, keyword: keyword.value });
    employees.value = res.data.items;
    pagination.value.total = res.data.total;
    
    // Select first row by default if available and no selection
    if (employees.value.length > 0 && !selectedEmployeeId.value) {
      handleRowClick(employees.value[0]);
    }
  } catch { /* ignore */ } finally { loading.value = false; }
};

const handleSearch = () => { pagination.value.page = 1; fetchEmployees(); };
const handlePageChange = (page: number) => { pagination.value.page = page; fetchEmployees(); };

// Replaced navigation with selection
const handleRowClick = async (row: Employee) => {
  selectedEmployeeId.value = row.id;
  detailLoading.value = true;
  try {
    const [empRes, histRes] = await Promise.all([
      employeeApi.getById(row.id),
      employeeApi.getJobHistory(row.id)
    ]);
    selectedEmployee.value = empRes.data;
    selectedJobHistory.value = histRes.data;
  } catch { 
    /* ignore */ 
  } finally { 
    detailLoading.value = false; 
  }
};

const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD');
const getStatusType = (status: string) => {
  const types: Record<string, string> = { Active: 'success', OnLeave: 'warning', Terminated: 'danger', Probation: 'info' };
  return types[status] || 'info';
};

onMounted(() => fetchEmployees());
</script>

<template>
  <div class="employee-container">
    <div class="employee-list">
      <el-card class="list-card">
        <template #header>
          <div class="card-header">
            <el-input v-model="keyword" :placeholder="t('common.search')" :prefix-icon="Search" style="width: 300px" @keyup.enter="handleSearch" />
            <el-button type="primary" :icon="Plus">{{ t('common.add') }}</el-button>
          </div>
        </template>
        <el-table 
          :data="employees" 
          v-loading="loading" 
          stripe 
          highlight-current-row
          @row-click="handleRowClick"
          height="calc(100vh - 240px)"
        >
          <el-table-column prop="employeeNo" :label="t('employee.employeeNo')" width="100" />
          <el-table-column prop="fullName" :label="t('employee.name')" width="120" />
          <el-table-column prop="email" :label="t('employee.email')" min-width="200" show-overflow-tooltip />
          <el-table-column prop="departmentName" :label="t('employee.department')" width="150" />
          <el-table-column prop="positionName" :label="t('employee.position')" width="150" />
          <el-table-column prop="status" :label="t('employee.status')" width="100">
            <template #default="{ row }">
              <el-tag :type="getStatusType(row.status)" size="small">{{ t(`employee.status${row.status}`) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="hireDate" :label="t('employee.hireDate')" width="120">
            <template #default="{ row }">
              {{ formatDate(row.hireDate) }}
            </template>
          </el-table-column>
        </el-table>
        <el-pagination v-model:current-page="pagination.page" :page-size="pagination.pageSize" :total="pagination.total" layout="total, prev, pager, next" @current-change="handlePageChange" style="margin-top: 16px" />
      </el-card>
    </div>

    <!-- Right Detail Panel -->
    <div class="employee-detail-panel">
      <el-card class="detail-card" v-loading="detailLoading">
        <template v-if="selectedEmployee">
          <div class="detail-header">
            <el-avatar :size="64" class="avatar">{{ selectedEmployee.fullName?.charAt(0) }}</el-avatar>
            <div class="info">
              <h2>{{ selectedEmployee.fullName }}</h2>
              <p>{{ selectedEmployee.positionName }} · {{ selectedEmployee.departmentName }}</p>
              <el-tag :type="getStatusType(selectedEmployee.status)">{{ t(`employee.status${selectedEmployee.status}`) }}</el-tag>
            </div>
          </div>
          
          <el-divider />
          
          <el-descriptions :column="1" border size="small">
            <el-descriptions-item :label="t('employee.employeeNo')">{{ selectedEmployee.employeeNo }}</el-descriptions-item>
            <el-descriptions-item :label="t('employee.email')">{{ selectedEmployee.email }}</el-descriptions-item>
            <el-descriptions-item :label="t('employee.phone')">{{ selectedEmployee.phone || '-' }}</el-descriptions-item>
            <el-descriptions-item :label="t('employee.hireDate')">{{ formatDate(selectedEmployee.hireDate) }}</el-descriptions-item>
            <el-descriptions-item :label="t('employee.manager')">{{ selectedEmployee.managerName || '-' }}</el-descriptions-item>
          </el-descriptions>

          <h4 style="margin-top: 24px; margin-bottom: 16px;">{{ t('profile.jobHistory') }}</h4>
          <el-timeline>
            <el-timeline-item v-for="item in selectedJobHistory" :key="item.id" :timestamp="formatDate(item.effectiveStartDate)" placement="top">
              <p><strong>{{ item.positionName }}</strong></p>
              <p style="color: #909399; font-size: 12px;">{{ item.departmentName }}</p>
            </el-timeline-item>
          </el-timeline>
        </template>
        <template v-else>
          <el-empty :description="t('common.selectRowToView')" />
        </template>
      </el-card>
    </div>
  </div>
</template>

<style scoped lang="scss">
.employee-container {
  display: flex;
  gap: 16px;
  height: calc(100vh - 84px); /* Adjust based on layout header/padding */
  
  .employee-list {
    flex: 1;
    min-width: 0; /* Prevent flex overflow */
    display: flex;
    flex-direction: column;

    .list-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      :deep(.el-card__body) {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }
    }
  }

  .employee-detail-panel {
    width: 350px;
    flex-shrink: 0;

    .detail-card {
      height: 100%;
      overflow-y: auto;
    }
  }
}

.card-header { 
  display: flex; 
  justify-content: space-between; 
}

.detail-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 12px;
  
  h2 { margin: 0; font-size: 1.25rem; }
  p { margin: 0; color: #909399; font-size: 0.9rem; }
  .avatar { background-color: var(--el-color-primary); font-size: 1.5rem; }
}
</style>
