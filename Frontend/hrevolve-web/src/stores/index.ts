import { createPinia } from 'pinia';

const pinia = createPinia();

export default pinia;

export { useAuthStore } from './auth';
export { useAppStore } from './app';
