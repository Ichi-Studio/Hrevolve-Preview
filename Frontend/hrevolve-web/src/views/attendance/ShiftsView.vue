<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { attendanceApi } from '@/api';
import type { Shift } from '@/types';

const shifts = ref<Shift[]>([]);
const loading = ref(false);

const fetchShifts = async () => {
  loading.value = true;
  try { const res = await attendanceApi.getShifts(); shifts.value = res.data; } catch { /* ignore */ } finally { loading.value = false; }
};

onMounted(() => fetchShifts());
</script>

<template>
  <div class="shifts-view">
    <el-card>
      <template #header><span>班次管理</span></template>
      <el-table :data="shifts" v-loading="loading" stripe>
        <el-table-column prop="code" label="班次代码" width="120" />
        <el-table-column prop="name" label="班次名称" width="150" />
        <el-table-column prop="startTime" label="上班时间" width="120" />
        <el-table-column prop="endTime" label="下班时间" width="120" />
        <el-table-column prop="breakMinutes" label="休息时长(分钟)" width="120" />
        <el-table-column prop="isFlexible" label="弹性"><template #default="{ row }"><el-tag :type="row.isFlexible ? 'success' : 'info'" size="small">{{ row.isFlexible ? '是' : '否' }}</el-tag></template></el-table-column>
      </el-table>
    </el-card>
  </div>
</template>
