<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { OfficeBuilding, Connection, Operation } from '@element-plus/icons-vue';
import { organizationApi } from '@/api';
import type { OrganizationUnit } from '@/types';

const { t } = useI18n();
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
    <el-card shadow="hover" class="structure-card">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <span class="title">{{ t('orgAdmin.structure') }}</span>
          </div>
          <el-tag type="info" effect="plain" round size="small" class="count-badge">
            {{ t('orgAdmin.totalDepartments', { count: treeData.length ? 1 : 0 }) }}
          </el-tag>
        </div>
      </template>
      
      <div class="tree-container">
        <el-tree
          v-loading="loading"
          :data="treeData"
          :props="defaultProps"
          default-expand-all
          node-key="id"
          :indent="32"
          class="custom-tree"
          :highlight-current="true"
        >
          <template #default="{ node, data }">
            <div class="custom-tree-node" :class="{ 'is-root': node.level === 1 }">
              <div class="node-content">
                <div class="icon-wrapper" :class="[`level-${node.level}`, { 'has-children': data.children?.length }]">
                  <el-icon>
                    <OfficeBuilding v-if="node.level === 1" />
                    <Operation v-else-if="data.children && data.children.length > 0" />
                    <Connection v-else />
                  </el-icon>
                </div>
                <span class="node-label">{{ node.label }}</span>
              </div>
              
              <div class="node-meta">
                <div class="member-count" :class="{ 'is-active': data.employeeCount > 0 }">
                  <span class="count-num">{{ data.employeeCount || 0 }}</span>
                  <span class="count-label">人</span>
                </div>
              </div>
            </div>
          </template>
        </el-tree>
        <el-empty v-if="!loading && treeData.length === 0" :description="t('orgAdmin.noData')" />
      </div>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
// Gold theme variables matching MainLayout
$gold-primary: #D4AF37;
$gold-light: #F4D03F;

// Dark theme backgrounds
$bg-card: #1A1A1A;
$text-primary: #FFFFFF;
$text-secondary: rgba(255, 255, 255, 0.85);

// Define active styles mixin for reuse - MUST BE DEFINED BEFORE USAGE
@mixin active-node-styles {
  .node-content {
    .node-label {
      color: $gold-primary;
    }
    
    .icon-wrapper {
      background: rgba(212, 175, 55, 0.2);
      color: $gold-primary;
    }
  }
  
  .node-meta .member-count {
    background: rgba(212, 175, 55, 0.1);
    color: $gold-primary;
    
    .count-num, .count-label {
        color: $gold-primary;
    }
  }
}

.org-structure {
  .structure-card {
    border-radius: 12px;
    border: 1px solid var(--el-border-color-lighter);
    background-color: $bg-card; // Ensure card background is dark
    transition: all 0.3s ease;
    
    &:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2); // Darker shadow
      border-color: rgba(212, 175, 55, 0.3); // Gold border on hover
    }
    
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 4px;
      
      .title {
        font-weight: 600;
        font-size: 16px;
        color: $text-primary;
      }
    }
  }

  .tree-container {
    padding: 8px 0;
  }

  :deep(.el-tree) {
    background-color: transparent; // Transparent tree bg
    color: $text-secondary; // Default text color
    --el-tree-node-content-height: 50px;
    --el-tree-node-hover-bg-color: transparent; // Disable default hover bg
    --el-tree-text-color: $text-secondary;
    
    // Override Element Plus default white background for current node
    --el-color-primary-light-9: rgba(212, 175, 55, 0.15);
  }

  :deep(.el-tree-node__content) {
    border-radius: 8px;
    margin-bottom: 8px;
    border: 1px solid transparent;
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    overflow: hidden; // For the shine effect
    background-color: rgba(255, 255, 255, 0.02); // Slight background for nodes
    
    // Shine effect element
    &::before {
      content: '';
      position: absolute;
      top: 0;
      left: -100%;
      width: 100%;
      height: 100%;
      background: linear-gradient(90deg, 
        transparent 0%, 
        rgba(212, 175, 55, 0.1) 50%, 
        transparent 100%
      );
      transition: left 0.5s;
      pointer-events: none;
    }
    
    &:hover {
      background: linear-gradient(90deg, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0.05) 100%);
      border-color: rgba(212, 175, 55, 0.2);
      
      &::before {
        left: 100%;
      }
      
      // Apply active styles on hover
      .custom-tree-node {
        @include active-node-styles;
      }
    }
  }

  // Handle is-current state (Active/Selected)
  :deep(.el-tree-node.is-current > .el-tree-node__content) {
    background: linear-gradient(90deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.08) 100%) !important;
    border-color: rgba(212, 175, 55, 0.3) !important;
    
    .custom-tree-node {
      @include active-node-styles;
    }
  }

  // Tree Guide Lines
  :deep(.el-tree-node) {
    position: relative;
    
    // Vertical line
    &::before {
      content: "";
      position: absolute;
      top: 0;
      bottom: 0;
      left: -18px; 
      border-left: 1px solid rgba(255, 255, 255, 0.1); // Dark mode line color
      width: 1px;
    }

    &:first-child::before {
      top: 25px; // Half of content height
    }

    // Horizontal line connector
    &::after {
      content: "";
      position: absolute;
      left: -18px;
      top: 25px; // Center of the 50px high content
      width: 18px;
      height: 1px;
      border-top: 1px solid rgba(255, 255, 255, 0.1); // Dark mode line color
    }

    // Fix focus state - remove default background and apply subtle one
    &:focus > .el-tree-node__content {
        background-color: rgba(212, 175, 55, 0.05);
    }
  }
  
  // Hide lines for root
  :deep(> .el-tree-node) {
    &::before { display: none; }
    &::after { display: none; }
  }

  .custom-tree-node {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-size: 14px;
    padding-right: 12px;
    position: relative; // Ensure z-index works if needed
    z-index: 2; // Above the shine
    
    .node-content {
      display: flex;
      align-items: center;
      gap: 12px;
      
      .icon-wrapper {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(255, 255, 255, 0.05); // Dark mode icon bg
        color: $text-secondary;
        transition: all 0.2s;
        
        &.level-1 {
          background: rgba(212, 175, 55, 0.2);
          color: $gold-primary;
        }
        
        &.has-children:not(.level-1) {
          // Keep neutral for non-hover state to avoid clutter
        }
      }
      
      .node-label {
        font-weight: 500;
        color: $text-primary;
        font-size: 14px;
        transition: color 0.3s;
      }
    }

    .node-meta {
      .member-count {
        display: flex;
        align-items: center;
        gap: 2px;
        padding: 4px 10px;
        border-radius: 12px;
        background: rgba(255, 255, 255, 0.05); // Dark mode badge bg
        color: $text-secondary;
        font-size: 12px;
        transition: all 0.2s;
        
        &.is-active {
          color: $text-primary;
          font-weight: 500;
        }
        
        .count-num {
          font-family: var(--el-font-family-monospace);
        }
        
        .count-label {
          font-size: 10px;
          transform: scale(0.9);
        }
      }
    }
  }
}
</style>
