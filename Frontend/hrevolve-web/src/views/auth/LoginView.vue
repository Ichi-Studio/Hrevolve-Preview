<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { User, Lock } from '@element-plus/icons-vue';
import { useAuthStore } from '@/stores';
import type { FormInstance, FormRules } from 'element-plus';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const authStore = useAuthStore();

const formRef = ref<FormInstance>();
const loading = ref(false);

const loginForm = reactive({
  username: '',
  password: '',
  rememberMe: false,
});

const rules: FormRules = {
  username: [
    { required: true, message: () => t('auth.pleaseInputUsername'), trigger: 'blur' },
  ],
  password: [
    { required: true, message: () => t('auth.pleaseInputPassword'), trigger: 'blur' },
  ],
};

const handleLogin = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  
  loading.value = true;
  
  try {
    await authStore.login({
      username: loginForm.username,
      password: loginForm.password,
      rememberMe: loginForm.rememberMe,
    });
    
    ElMessage.success(t('auth.loginSuccess'));
    
    // 跳转到之前的页面或首页
    const redirect = (route.query.redirect as string) || '/';
    router.push(redirect);
  } catch (error) {
    ElMessage.error(t('auth.loginFailed'));
  } finally {
    loading.value = false;
  }
};

// 社交登录处理
const handleSocialLogin = (provider: string) => {
  const providerNames: Record<string, string> = {
    google: 'Google',
    microsoft: 'Microsoft',
    wechat: '微信',
    whatsapp: 'WhatsApp',
  };
  ElMessage.info(`${providerNames[provider]} 登录功能开发中，敬请期待`);
  // TODO: 实现OAuth跳转
  // window.location.href = `/api/auth/external/${provider}`;
};
</script>

<template>
  <div class="login-container">
    <!-- 背景装饰 -->
    <div class="bg-decoration">
      <div class="glow glow-1"></div>
      <div class="glow glow-2"></div>
      <div class="glow glow-3"></div>
    </div>
    
    <div class="login-card">
      <div class="login-header">
        <div class="logo-wrapper">
          <img src="@/assets/logo.svg" alt="Logo" class="logo" />
        </div>
        <h1 class="title">Hrevolve</h1>
        <p class="subtitle">企业级SaaS人力资源管理系统</p>
      </div>
      
      <el-form
        ref="formRef"
        :model="loginForm"
        :rules="rules"
        class="login-form"
        @keyup.enter="handleLogin"
      >
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            :placeholder="t('auth.username')"
            :prefix-icon="User"
            size="large"
          />
        </el-form-item>
        
        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            :placeholder="t('auth.password')"
            :prefix-icon="Lock"
            size="large"
            show-password
          />
        </el-form-item>
        
        <div class="login-options">
          <el-checkbox v-model="loginForm.rememberMe">
            {{ t('auth.rememberMe') }}
          </el-checkbox>
          <el-link type="primary" :underline="false" class="forgot-link">
            {{ t('auth.forgotPassword') }}
          </el-link>
        </div>
        
        <el-button
          type="primary"
          size="large"
          :loading="loading"
          class="login-btn"
          @click="handleLogin"
        >
          {{ t('auth.login') }}
        </el-button>
      </el-form>
      
      <el-divider class="divider">其他登录方式</el-divider>
      
      <div class="social-login">
        <el-tooltip content="Google 登录（开发中）" placement="top">
          <button class="social-btn" @click="handleSocialLogin('google')">
            <svg viewBox="0 0 24 24" width="20" height="20">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
          </button>
        </el-tooltip>
        <el-tooltip content="Microsoft 登录（开发中）" placement="top">
          <button class="social-btn" @click="handleSocialLogin('microsoft')">
            <svg viewBox="0 0 24 24" width="20" height="20">
              <path fill="#F25022" d="M1 1h10v10H1z"/>
              <path fill="#00A4EF" d="M1 13h10v10H1z"/>
              <path fill="#7FBA00" d="M13 1h10v10H13z"/>
              <path fill="#FFB900" d="M13 13h10v10H13z"/>
            </svg>
          </button>
        </el-tooltip>
        <el-tooltip content="微信登录（开发中）" placement="top">
          <button class="social-btn" @click="handleSocialLogin('wechat')">
            <svg viewBox="0 0 24 24" width="22" height="22">
              <path fill="#07C160" d="M8.691 2.188C3.891 2.188 0 5.476 0 9.53c0 2.212 1.17 4.203 3.002 5.55a.59.59 0 0 1 .213.665l-.39 1.48c-.019.07-.048.141-.048.213 0 .163.13.295.29.295a.326.326 0 0 0 .167-.054l1.903-1.114a.864.864 0 0 1 .717-.098 10.16 10.16 0 0 0 2.837.403c.276 0 .543-.027.811-.05-.857-2.578.157-4.972 1.932-6.446 1.703-1.415 3.882-1.98 5.853-1.838-.576-3.583-4.196-6.348-8.596-6.348zM5.785 5.991c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178A1.17 1.17 0 0 1 4.623 7.17c0-.651.52-1.18 1.162-1.18zm5.813 0c.642 0 1.162.529 1.162 1.18a1.17 1.17 0 0 1-1.162 1.178 1.17 1.17 0 0 1-1.162-1.178c0-.651.52-1.18 1.162-1.18zm5.34 2.867c-1.797-.052-3.746.512-5.28 1.786-1.72 1.428-2.687 3.72-1.78 6.22.942 2.453 3.666 4.229 6.884 4.229.826 0 1.622-.12 2.361-.336a.722.722 0 0 1 .598.082l1.584.926a.272.272 0 0 0 .14.047c.134 0 .24-.111.24-.247 0-.06-.023-.12-.038-.177l-.327-1.233a.582.582 0 0 1-.023-.156.49.49 0 0 1 .201-.398C23.024 18.48 24 16.82 24 14.98c0-3.21-2.931-5.837-6.656-6.088V8.89c-.135-.01-.27-.027-.407-.03zm-2.53 3.274c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.97-.982zm4.844 0c.535 0 .969.44.969.982a.976.976 0 0 1-.969.983.976.976 0 0 1-.969-.983c0-.542.434-.982.969-.982z"/>
            </svg>
          </button>
        </el-tooltip>
        <el-tooltip content="WhatsApp 登录（开发中）" placement="top">
          <button class="social-btn" @click="handleSocialLogin('whatsapp')">
            <svg viewBox="0 0 24 24" width="22" height="22">
              <path fill="#25D366" d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/>
            </svg>
          </button>
        </el-tooltip>
      </div>
    </div>
    
    <div class="login-footer">
      <p>© {{ new Date().getFullYear() }} Hrevolve. All rights reserved.</p>
    </div>
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

.login-container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: radial-gradient(ellipse at top, #1a1a1a 0%, #0a0a0a 50%, #000000 100%);
  padding: 20px;
  position: relative;
  overflow: hidden;
}

// 背景发光装饰
.bg-decoration {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
  
  .glow {
    position: absolute;
    border-radius: 50%;
    filter: blur(100px);
    opacity: 0.3;
  }
  
  .glow-1 {
    width: 500px;
    height: 500px;
    background: radial-gradient(circle, $gold-primary 0%, transparent 70%);
    top: -150px;
    right: -150px;
    animation: float 10s ease-in-out infinite;
  }
  
  .glow-2 {
    width: 400px;
    height: 400px;
    background: radial-gradient(circle, $gold-dark 0%, transparent 70%);
    bottom: -100px;
    left: -100px;
    animation: float 12s ease-in-out infinite reverse;
  }
  
  .glow-3 {
    width: 300px;
    height: 300px;
    background: radial-gradient(circle, $gold-light 0%, transparent 70%);
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    animation: pulse 8s ease-in-out infinite;
  }
}

@keyframes float {
  0%, 100% { 
    transform: translateY(0) translateX(0) rotate(0deg); 
    opacity: 0.3;
  }
  50% { 
    transform: translateY(-40px) translateX(20px) rotate(5deg); 
    opacity: 0.5;
  }
}

@keyframes pulse {
  0%, 100% { 
    opacity: 0.2; 
    transform: translate(-50%, -50%) scale(1); 
  }
  50% { 
    opacity: 0.4; 
    transform: translate(-50%, -50%) scale(1.2); 
  }
}

@keyframes shimmer {
  0% { background-position: -1000px 0; }
  100% { background-position: 1000px 0; }
}

.login-card {
  width: 100%;
  max-width: 440px;
  background: linear-gradient(145deg, rgba(26, 26, 26, 0.98) 0%, rgba(13, 13, 13, 0.99) 100%);
  border: 1px solid $border-color;
  border-radius: 20px;
  padding: 56px 48px;
  box-shadow: 
    0 30px 60px rgba(0, 0, 0, 0.6),
    0 0 120px rgba(212, 175, 55, 0.15),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  position: relative;
  z-index: 1;
  backdrop-filter: blur(10px);
  animation: cardFadeIn 0.6s ease-out;
  
  // 顶部金色渐变边框
  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 15%;
    right: 15%;
    height: 3px;
    background: linear-gradient(90deg, transparent, $gold-primary, $gold-light, $gold-primary, transparent);
    border-radius: 3px;
    animation: shimmer 3s linear infinite;
    background-size: 1000px 100%;
  }
  
  // 底部微光效果
  &::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(212, 175, 55, 0.3), transparent);
  }
}

@keyframes cardFadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.login-header {
  text-align: center;
  margin-bottom: 40px;
  
  .logo-wrapper {
    width: 80px;
    height: 80px;
    margin: 0 auto 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
    border-radius: 20px;
    box-shadow: 
      0 10px 40px rgba(212, 175, 55, 0.4),
      inset 0 1px 0 rgba(255, 255, 255, 0.3);
    position: relative;
    animation: logoFloat 3s ease-in-out infinite;
    
    &::before {
      content: '';
      position: absolute;
      inset: -2px;
      background: linear-gradient(135deg, $gold-light, $gold-primary, $gold-dark);
      border-radius: 22px;
      z-index: -1;
      opacity: 0;
      transition: opacity 0.3s;
    }
    
    &:hover::before {
      opacity: 0.5;
    }
  }
  
  @keyframes logoFloat {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-5px); }
  }
  
  .logo {
    width: 44px;
    height: 44px;
    filter: brightness(0) invert(0);
  }
  
  .title {
    font-size: 36px;
    font-weight: 700;
    margin: 0 0 12px;
    background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-primary 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
    letter-spacing: -0.5px;
  }
  
  .subtitle {
    font-size: 14px;
    color: $text-tertiary;
    margin: 0;
    font-weight: 400;
  }
}

.login-form {
  // 优化表单项间距
  :deep(.el-form-item) {
    margin-bottom: 20px;
    
    .el-input {
      .el-input__wrapper {
        // 多层背景叠加，创造深度感
        background: 
          linear-gradient(135deg, rgba(255, 255, 255, 0.05) 0%, rgba(255, 255, 255, 0.02) 100%),
          rgba(26, 26, 26, 0.6);
        border: 1px solid rgba(212, 175, 55, 0.15);
        border-radius: 12px;
        box-shadow: 
          inset 0 1px 2px rgba(0, 0, 0, 0.3),
          0 1px 0 rgba(255, 255, 255, 0.05);
        transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
        padding: 10px 18px;
        min-height: 52px;
        position: relative;
        backdrop-filter: blur(8px);
        
        // 顶部微光效果
        &::before {
          content: '';
          position: absolute;
          top: 0;
          left: 10%;
          right: 10%;
          height: 1px;
          background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent);
          opacity: 0;
          transition: opacity 0.4s;
        }
        
        &:hover {
          background: 
            linear-gradient(135deg, rgba(255, 255, 255, 0.08) 0%, rgba(255, 255, 255, 0.04) 100%),
            rgba(26, 26, 26, 0.7);
          border-color: rgba(212, 175, 55, 0.35);
          box-shadow: 
            inset 0 1px 3px rgba(0, 0, 0, 0.2),
            0 2px 8px rgba(212, 175, 55, 0.1),
            0 1px 0 rgba(255, 255, 255, 0.08);
          transform: translateY(-1px);
          
          &::before {
            opacity: 1;
          }
        }
        
        &.is-focus {
          background: 
            linear-gradient(135deg, rgba(212, 175, 55, 0.08) 0%, rgba(212, 175, 55, 0.03) 100%),
            rgba(26, 26, 26, 0.8);
          border-color: $gold-primary;
          box-shadow: 
            inset 0 1px 3px rgba(0, 0, 0, 0.2),
            0 0 0 3px rgba(212, 175, 55, 0.15),
            0 4px 12px rgba(212, 175, 55, 0.2),
            0 1px 0 rgba(255, 255, 255, 0.1);
          transform: translateY(-2px);
          
          &::before {
            opacity: 1;
            background: linear-gradient(90deg, transparent, rgba(212, 175, 55, 0.3), transparent);
          }
        }
      }
      
      .el-input__inner {
        color: $text-primary;
        font-size: 15px;
        font-weight: 400;
        line-height: 1.6;
        letter-spacing: 0.3px;
        transition: all 0.3s;
        height: 40px;
        
        &::placeholder {
          color: $text-tertiary;
          font-weight: 400;
          letter-spacing: 0.2px;
          transition: all 0.3s;
        }
        
        // 聚焦时占位符淡出
        &:focus::placeholder {
          opacity: 0.5;
          transform: translateX(4px);
        }
      }
      
      .el-input__prefix {
        color: $gold-primary;
        margin-right: 12px;
        display: flex;
        align-items: center;
        transition: all 0.3s;
        
        .el-icon {
          font-size: 18px;
          transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        }
      }
      
      .el-input__suffix {
        display: flex;
        align-items: center;
        
        .el-icon {
          transition: all 0.3s;
          color: $text-tertiary;
          
          &:hover {
            color: $gold-primary;
          }
        }
      }
      
      // 聚焦时图标动画
      &:focus-within {
        .el-input__prefix .el-icon {
          transform: scale(1.1);
          color: $gold-light;
        }
      }
    }
  }
  
  .login-options {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 28px;
    
    :deep(.el-checkbox) {
      .el-checkbox__label {
        color: $text-secondary;
        font-size: 14px;
      }
      
      .el-checkbox__inner {
        background: rgba(255, 255, 255, 0.04);
        border-color: rgba(212, 175, 55, 0.3);
        
        &:hover {
          border-color: $gold-primary;
        }
      }
      
      &.is-checked {
        .el-checkbox__inner {
          background: $gold-primary;
          border-color: $gold-primary;
        }
        
        .el-checkbox__label {
          color: $text-primary;
        }
      }
    }
    
    .forgot-link {
      color: $gold-primary;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.3s;
      
      &:hover {
        color: $gold-light;
      }
    }
  }
  
  .login-btn {
    width: 100%;
    height: 52px;
    font-size: 16px;
    font-weight: 600;
    background: linear-gradient(135deg, $gold-primary 0%, $gold-light 50%, $gold-dark 100%);
    border: none;
    border-radius: 12px;
    color: $bg-dark;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    overflow: hidden;
    
    &::before {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(135deg, $gold-light 0%, $gold-primary 50%, $gold-light 100%);
      opacity: 0;
      transition: opacity 0.3s;
    }
    
    &:hover {
      box-shadow: 0 10px 30px rgba(212, 175, 55, 0.5);
      transform: translateY(-2px);
      
      &::before {
        opacity: 1;
      }
    }
    
    &:active {
      transform: translateY(0);
      box-shadow: 0 5px 15px rgba(212, 175, 55, 0.3);
    }
    
    span {
      position: relative;
      z-index: 1;
    }
  }
}

.divider {
  margin: 32px 0 28px;
  
  :deep(.el-divider__text) {
    background: transparent;
    color: $text-tertiary;
    font-size: 13px;
    padding: 0 20px;
    font-weight: 500;
  }
  
  &::before, &::after {
    border-color: rgba(212, 175, 55, 0.15);
  }
}

.social-login {
  display: flex;
  justify-content: center;
  gap: 20px;
  
  .social-btn {
    width: 56px;
    height: 56px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid $border-color;
    color: $text-tertiary;
    cursor: pointer;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    
    &::before {
      content: '';
      position: absolute;
      inset: -2px;
      border-radius: 50%;
      background: linear-gradient(135deg, $gold-primary, $gold-light);
      opacity: 0;
      transition: opacity 0.3s;
      z-index: -1;
    }
    
    &:hover {
      border-color: transparent;
      background: rgba(212, 175, 55, 0.15);
      transform: translateY(-4px) scale(1.05);
      box-shadow: 0 10px 25px rgba(212, 175, 55, 0.3);
      
      &::before {
        opacity: 1;
      }
    }
    
    &:active {
      transform: translateY(-2px) scale(1.02);
    }
  }
}

.login-footer {
  margin-top: 40px;
  color: $text-tertiary;
  font-size: 13px;
  position: relative;
  z-index: 1;
  text-align: center;
  
  p {
    margin: 0;
    font-weight: 400;
  }
}

// 响应式优化
@media (max-width: 768px) {
  .login-card {
    max-width: 100%;
    padding: 40px 32px;
    border-radius: 16px;
  }
  
  .login-header {
    margin-bottom: 32px;
    
    .logo-wrapper {
      width: 70px;
      height: 70px;
      margin-bottom: 20px;
    }
    
    .title {
      font-size: 30px;
    }
    
    .subtitle {
      font-size: 13px;
    }
  }
  
  .login-form {
    .login-btn {
      height: 48px;
      font-size: 15px;
    }
  }
  
  .social-login {
    gap: 16px;
    
    .social-btn {
      width: 50px;
      height: 50px;
    }
  }
}

@media (max-width: 480px) {
  .login-container {
    padding: 16px;
  }
  
  .login-card {
    padding: 32px 24px;
  }
  
  .login-header {
    .logo-wrapper {
      width: 64px;
      height: 64px;
    }
    
    .title {
      font-size: 26px;
    }
  }
  
  .login-form {
    .login-options {
      flex-direction: column;
      align-items: flex-start;
      gap: 12px;
    }
  }
}
</style>
