<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { leaveApi } from '@/api';
import type { LeaveType } from '@/types';

const types = ref<LeaveType[]>([]);
const loading = ref(false);

const fetchTypes = async () => {
  loading.value = true;
  try { const res = await leaveApi.getTypes(); types.value = res.data; } catch { /* ignore */ } finally { loading.value = false; }
};

onMounted(() => fetchTypes());
</script>

<template>
  <div class="types-view">
    <el-card>
      <template #header><span>假期类型</span></template>
      <el-table :data="types" v-loading="loading" stripe>
        <el-table-column prop="code" label="代码" width="100" />
        <el-table-column prop="name" label="名称" width="150" />
        <el-table-column prop="defaultDays" label="默认天数" width="100" />
        <el-table-column prop="maxCarryOver" label="最大结转" width="100" />
        <el-table-column prop="isPaid" label="带薪" width="80"><template #default="{ row }"><el-tag :type="row.isPaid ? 'success' : 'info'" size="small">{{ row.isPaid ? '是' : '否' }}</el-tag></template></el-table-column>
        <el-table-column prop="description" label="描述" />
      </el-table>
    </el-card>
  </div>
</template>
