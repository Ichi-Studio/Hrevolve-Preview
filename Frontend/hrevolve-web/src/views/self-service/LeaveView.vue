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
    <el-row :gutter="20" class="balance-row">
      <el-col v-for="(balance, index) in balances" :key="balance.leaveTypeId" :xs="24" :sm="12" :lg="6">
        <div class="balance-card" :style="{ '--delay': index * 0.1 + 's' }">
          <div class="card-glow"></div>
          <div class="balance-header">
            <span class="balance-icon">📅</span>
            <span class="balance-name">{{ balance.leaveTypeName }}</span>
          </div>
          <div class="balance-value">
            <span class="remaining">{{ balance.remainingDays }}</span>
            <span class="unit">天</span>
          </div>
          <div class="balance-detail">
            总计 {{ balance.totalDays }} 天 · 已用 {{ balance.usedDays }} 天
          </div>
          <div class="progress-bar">
            <div 
              class="progress-fill" 
              :style="{ width: (balance.usedDays / balance.totalDays) * 100 + '%' }"
            ></div>
          </div>
        </div>
      </el-col>
    </el-row>
    
    <!-- 请假记录 -->
    <div class="records-card">
      <div class="card-header">
        <div class="header-title">
          <span class="title-icon">📋</span>
          <span>请假记录</span>
        </div>
        <el-button class="add-btn" :icon="Plus" @click="dialogVisible = true">
          申请请假
        </el-button>
      </div>
      
      <div class="table-wrapper">
        <el-table :data="requests" v-loading="loading">
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
              <span :class="['status-tag', row.status.toLowerCase()]">
                {{ t(`leave.status${row.status}`) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="approverName" :label="t('leave.approver')" width="100" />
          <el-table-column :label="t('common.actions')" width="100" fixed="right">
            <template #default="{ row }">
              <el-button
                v-if="row.status === 'Pending'"
                class="cancel-btn"
                text
                size="small"
                @click="handleCancel(row.id)"
              >
                取消
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
    
    <!-- 请假对话框 -->
    <el-dialog v-model="dialogVisible" title="申请请假" width="500px" class="leave-dialog">
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
        <el-button class="dialog-cancel-btn" @click="dialogVisible = false">取消</el-button>
        <el-button class="dialog-submit-btn" @click="handleSubmit">提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped lang="scss">
// 黑金主题变量
$gold-primary: #D4AF37;
$gold-light: #F4D03F;
$gold-dark: #B8860B;
$bg-dark: #0D0D0D;
$bg-card: #1A1A1A;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);
$text-tertiary: rgba(255, 255, 255, 0.65);
$border-color: rgba(212, 175, 55, 0.2);

.leave-view {
  .balance-row {
    margin-bottom: 24px;
  }
  
  .balance-card {
    position: relative;
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 16px;
    padding: 24px;
    margin-bottom: 20px;
    text-align: center;
    overflow: hidden;
    animation: fadeInUp 0.5s ease forwards;
    animation-delay: var(--delay);
    opacity: 0;
    transition: all 0.3s;
    
    &:hover {
      border-color: rgba(212, 175, 55, 0.4);
      transform: translateY(-4px);
      
      .card-glow {
        opacity: 1;
      }
    }
    
    .card-glow {
      position: absolute;
      top: -50%;
      left: -50%;
      width: 200%;
      height: 200%;
      background: radial-gradient(circle, rgba(212, 175, 55, 0.1) 0%, transparent 50%);
      opacity: 0;
      transition: opacity 0.3s;
      pointer-events: none;
    }
    
    .balance-header {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      margin-bottom: 16px;
      
      .balance-icon {
        font-size: 20px;
      }
      
      .balance-name {
        font-size: 14px;
        color: $text-tertiary;
        font-weight: 500;
      }
    }
    
    .balance-value {
      margin-bottom: 12px;
      
      .remaining {
        font-size: 42px;
        font-weight: 700;
        background: linear-gradient(135deg, $gold-primary 0%, $gold-light 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }
      
      .unit {
        font-size: 16px;
        color: $text-tertiary;
        margin-left: 4px;
      }
    }
    
    .balance-detail {
      font-size: 12px;
      color: $text-tertiary;
      margin-bottom: 16px;
    }
    
    .progress-bar {
      height: 6px;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 3px;
      overflow: hidden;
      
      .progress-fill {
        height: 100%;
        background: linear-gradient(90deg, $gold-primary 0%, $gold-light 100%);
        border-radius: 3px;
        transition: width 0.5s ease;
      }
    }
  }
  
  @keyframes fadeInUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  .records-card {
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 16px;
    overflow: hidden;
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px 24px;
      background: linear-gradient(90deg, rgba(212, 175, 55, 0.1) 0%, transparent 100%);
      border-bottom: 1px solid $border-color;
      
      .header-title {
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: 16px;
        font-weight: 600;
        color: $text-primary;
        
        .title-icon {
          font-size: 20px;
        }
      }
      
      .add-btn {
        background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
        border: none;
        color: $bg-dark;
        font-weight: 600;
        padding: 10px 20px;
        border-radius: 8px;
        
        &:hover {
          box-shadow: 0 4px 15px rgba(212, 175, 55, 0.4);
        }
      }
    }
    
    .table-wrapper {
      padding: 0;
      
      :deep(.el-table) {
        background: transparent;
        
        &::before {
          display: none;
        }
        
        th.el-table__cell {
          background: rgba(212, 175, 55, 0.08);
          color: $gold-primary;
          border-bottom: 1px solid $border-color;
          font-weight: 600;
        }
        
        td.el-table__cell {
          background: transparent;
          color: $text-secondary;
          border-bottom: 1px solid rgba(255, 255, 255, 0.05);
        }
        
        tr:hover > td.el-table__cell {
          background: rgba(212, 175, 55, 0.05);
        }
        
        .el-table__body-wrapper {
          background: transparent;
        }
      }
    }
  }
  
  .status-tag {
    display: inline-block;
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: 500;
    
    &.pending {
      background: rgba(250, 173, 20, 0.15);
      color: #faad14;
    }
    
    &.approved {
      background: rgba(82, 196, 26, 0.15);
      color: #52c41a;
    }
    
    &.rejected {
      background: rgba(255, 77, 79, 0.15);
      color: #ff4d4f;
    }
    
    &.cancelled {
      background: rgba(255, 255, 255, 0.1);
      color: $text-tertiary;
    }
  }
  
  .cancel-btn {
    color: #ff4d4f;
    
    &:hover {
      color: #ff7875;
    }
  }
  
  // 对话框样式
  :deep(.el-dialog) {
    background: $bg-card;
    border: 1px solid $border-color;
    border-radius: 16px;
    
    .el-dialog__header {
      background: linear-gradient(90deg, rgba(212, 175, 55, 0.1) 0%, transparent 100%);
      border-bottom: 1px solid $border-color;
      padding: 20px 24px;
      
      .el-dialog__title {
        color: $gold-primary;
        font-weight: 600;
      }
    }
    
    .el-dialog__body {
      padding: 24px;
    }
    
    .el-dialog__footer {
      border-top: 1px solid $border-color;
      padding: 16px 24px;
    }
  }
  
  .dialog-cancel-btn {
    background: transparent;
    border: 1px solid $border-color;
    color: $text-tertiary;
    
    &:hover {
      border-color: $gold-primary;
      color: $gold-primary;
    }
  }
  
  .dialog-submit-btn {
    background: linear-gradient(135deg, $gold-primary 0%, $gold-dark 100%);
    border: none;
    color: $bg-dark;
    font-weight: 600;
    
    &:hover {
      box-shadow: 0 4px 15px rgba(212, 175, 55, 0.4);
    }
  }
}
</style>
