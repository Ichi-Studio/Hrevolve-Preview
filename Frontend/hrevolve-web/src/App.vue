<script setup lang="ts">
import { onMounted } from 'vue';
import { useAppStore } from '@/stores';
import zhCn from 'element-plus/es/locale/lang/zh-cn';
import en from 'element-plus/es/locale/lang/en';
import { computed } from 'vue';

const appStore = useAppStore();

const locale = computed(() => appStore.language === 'zh-CN' ? zhCn : en);

onMounted(() => {
  // 初始化主题
  appStore.setTheme(appStore.theme);
});
</script>

<template>
  <el-config-provider :locale="locale">
    <router-view />
  </el-config-provider>
</template>

<style>
html, body, #app {
  margin: 0;
  padding: 0;
  height: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
}
</style>
