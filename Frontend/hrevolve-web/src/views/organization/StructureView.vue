<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { organizationApi } from '@/api';
import type { OrganizationUnit } from '@/types';

const treeData = ref<OrganizationUnit[]>([]);
const loading = ref(false);
const defaultProps = { children: 'children', label: 'name' };

const fetchTree = async () => {
  loading.value = true;
  try {
    const res = await organizationApi.getTree();
    treeData.value = res.data;
  } catch { /* ignore */ } finally { loading.value = false; }
};

onMounted(() => fetchTree());
</script>

<template>
  <div class="org-structure">
    <el-card>
      <template #header><span>组织架构</span></template>
      <el-tree v-loading="loading" :data="treeData" :props="defaultProps" default-expand-all node-key="id">
        <template #default="{ node, data }">
          <span class="tree-node">
            <span>{{ node.label }}</span>
            <span class="node-info">{{ data.employeeCount || 0 }}人</span>
          </span>
        </template>
      </el-tree>
      <el-empty v-if="!loading && treeData.length === 0" description="暂无组织架构数据" />
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.org-structure { .tree-node { display: flex; gap: 8px; .node-info { color: #999; font-size: 12px; } } }
</style>
