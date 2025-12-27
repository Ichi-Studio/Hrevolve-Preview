<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { organizationApi } from '@/api';
import type { Position } from '@/types';

const positions = ref<Position[]>([]);
const loading = ref(false);

const fetchPositions = async () => {
  loading.value = true;
  try {
    const res = await organizationApi.getPositions();
    positions.value = res.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

onMounted(() => fetchPositions());
</script>

<template>
  <div class="positions-view">
    <el-card>
      <template #header><span>职位管理</span></template>
      <el-table :data="positions" v-loading="loading" stripe>
        <el-table-column prop="code" label="职位代码" width="120" />
        <el-table-column prop="name" label="职位名称" width="200" />
        <el-table-column prop="level" label="职级" width="100" />
        <el-table-column prop="description" label="描述" />
      </el-table>
    </el-card>
  </div>
</template>
