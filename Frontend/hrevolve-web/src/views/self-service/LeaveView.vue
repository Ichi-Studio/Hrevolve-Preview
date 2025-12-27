<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';
import { leaveApi } from '@/api';
import type { LeaveRequest, LeaveBalance, LeaveType, PageResponse } from '@/types';
import type { FormInstance, FormRules } from 'element-plus';
import dayjs from 'dayjs';

const { t } = useI18n();

// 数据
const balances = ref<LeaveBalance[]>([]);
const requests = ref<LeaveRequest[]>([]);
const leaveTypes = ref<LeaveType[]>([]);
const loading = ref(false);
const dialogVisible = ref(false);
const formRef = ref<FormInstance>();

// 表单
const form = reactive({
  leaveTypeId: '',
  startDate: '',
  endDate: '',
  reason: '',
});

const rules: FormRules = {
  leaveTypeId: [{ required: true, message: '请选择假期类型', trigger: 'change' }],
  startDate: [{ required: true, message: '请选择开始日期', trigger: 'change' }],
  endDate: [{ required: true, message: '请选择结束日期', trigger: 'change' }],
  reason: [{ required: true, message: '请输入请假原因', trigger: 'blur' }],
};

// 获取假期余额
const fetchBalances = async () => {
  try {
    const res = await leaveApi.getMyBalances();
    balances.value = res.data;
  } catch {
    // 忽略错误
  }
};

// 获取请假记录
const fetchRequests = async () => {
  loading.value = true;
  try {
    const res = await leaveApi.getMyRequests({ page: 1, pageSize: 50 });
    requests.value = (res.data as PageResponse<LeaveRequest>).items;
  } catch {
    // 忽略错误
  } finally {
    loading.value = false;
  }
};

// 获取假期类型
const fetchLeaveTypes = async () => {
  try {
    const res = await leaveApi.getTypes();
    leaveTypes.value = res.data;
  } catch {
    // 忽略错误
  }
};

// 提交请假
const handleSubmit = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  
  try {
    await leaveApi.submitRequest({
      leaveTypeId: form.leaveTypeId,
      startDate: dayjs(form.startDate).format('YYYY-MM-DD'),
      endDate: dayjs(form.endDate).format('YYYY-MM-DD'),
      reason: form.reason,
    });
    
    ElMessage.success('请假申请已提交');
    dialogVisible.value = false;
    fetchRequests();
    fetchBalances();
    
    // 重置表单
    form.leaveTypeId = '';
    form.startDate = '';
    form.endDate = '';
    form.reason = '';
  } catch {
    ElMessage.error('提交失败');
  }
};

// 取消请假
const handleCancel = async (id: string) => {
  try {
    await leaveApi.cancelRequest(id);
    ElMessage.success('已取消');
    fetchRequests();
    fetchBalances();
  } catch {
    ElMessage.error('取消失败');
  }
};

// 格式化日期
const formatDate = (date: string) => dayjs(date).format('YYYY-MM-DD');

// 获取状态标签类型
const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    Pending: 'warning',
    Approved: 'success',
    Rejected: 'danger',
    Cancelled: 'info',
  };
  return types[status] || 'info';
};

onMounted(() => {
  fetchBalances();
  fetchRequests();
  fetchLeaveTypes();
});
</script>

<template>
  <div class="leave-view">
    <!-- 假期余额 -->
    <el-row :gutter="16" class="balance-row">
      <el-col v-for="balance in balances" :key="balance.leaveTypeId" :span="6">
        <el-card shadow="hover" class="balance-card">
          <div class="balance-header">
            <span class="balance-name">{{ balance.leaveTypeName }}</span>
          </div>
          <div class="balance-value">
            <span class="remaining">{{ balance.remainingDays }}</span>
            <span class="unit">天</span>
          </div>
          <div class="balance-detail">
            总计 {{ balance.totalDays }} 天 · 已用 {{ balance.usedDays }} 天
          </div>
          <el-progress
            :percentage="(balance.usedDays / balance.totalDays) * 100"
            :stroke-width="6"
            :show-text="false"
          />
        </el-card>
      </el-col>
    </el-row>
    
    <!-- 请假记录 -->
    <el-card>
      <template #header>
        <div class="card-header">
          <span>请假记录</span>
          <el-button type="primary" :icon="Plus" @click="dialogVisible = true">
            申请请假
          </el-button>
        </div>
      </template>
      
      <el-table :data="requests" v-loading="loading" stripe>
        <el-table-column prop="leaveTypeName" :label="t('leave.leaveType')" width="100" />
        <el-table-column prop="startDate" :label="t('leave.startDate')" width="120">
          <template #default="{ row }">{{ formatDate(row.startDate) }}</template>
        </el-table-column>
        <el-table-column prop="endDate" :label="t('leave.endDate')" width="120">
          <template #default="{ row }">{{ formatDate(row.endDate) }}</template>
        </el-table-column>
        <el-table-column prop="days" :label="t('leave.days')" width="80" />
        <el-table-column prop="reason" :label="t('leave.reason')" />
        <el-table-column prop="status" :label="t('leave.approvalStatus')" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ t(`leave.status${row.status}`) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="approverName" :label="t('leave.approver')" width="100" />
        <el-table-column :label="t('common.actions')" width="100" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 'Pending'"
              type="danger"
              text
              size="small"
              @click="handleCancel(row.id)"
            >
              取消
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
    
    <!-- 请假对话框 -->
    <el-dialog v-model="dialogVisible" title="申请请假" width="500px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item :label="t('leave.leaveType')" prop="leaveTypeId">
          <el-select v-model="form.leaveTypeId" placeholder="请选择假期类型" style="width: 100%">
            <el-option
              v-for="type in leaveTypes"
              :key="type.id"
              :label="type.name"
              :value="type.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('leave.startDate')" prop="startDate">
          <el-date-picker
            v-model="form.startDate"
            type="date"
            placeholder="选择开始日期"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item :label="t('leave.endDate')" prop="endDate">
          <el-date-picker
            v-model="form.endDate"
            type="date"
            placeholder="选择结束日期"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item :label="t('leave.reason')" prop="reason">
          <el-input
            v-model="form.reason"
            type="textarea"
            :rows="3"
            placeholder="请输入请假原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
.leave-view {
  .balance-row {
    margin-bottom: 16px;
  }
  
  .balance-card {
    text-align: center;
    
    .balance-header {
      margin-bottom: 8px;
      
      .balance-name {
        font-size: 14px;
        color: #666;
      }
    }
    
    .balance-value {
      margin-bottom: 8px;
      
      .remaining {
        font-size: 32px;
        font-weight: 600;
        color: #1890ff;
      }
      
      .unit {
        font-size: 14px;
        color: #666;
        margin-left: 4px;
      }
    }
    
    .balance-detail {
      font-size: 12px;
      color: #999;
      margin-bottom: 8px;
    }
  }
  
  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
}
</style>
