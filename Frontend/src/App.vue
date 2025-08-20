<!-- LoginSignup.vue -->
<template>
  <div class="auth-container">
    <a-card class="auth-card" :bordered="false">
      <div class="auth-header">
        <a-typography-title :heading="2" align="center">
          {{ isLogin ? '登录' : '注册' }}
        </a-typography-title>
        <a-typography-paragraph align="center">
          {{ isLogin ? '欢迎回来' : '创建新账户' }}
        </a-typography-paragraph>
      </div>
      
      <a-form :model="form" @submit="handleSubmit" ref="formRef">
        <!-- 用户名字段 (仅注册时显示) -->
        <a-form-item v-if="!isLogin" field="username" label="用户名">
          <a-input 
            v-model="form.username" 
            placeholder="请输入用户名"
            allow-clear
          />
        </a-form-item>
        
        <!-- 邮箱字段 -->
        <a-form-item 
          field="email" 
          label="邮箱" 
          :rules="[{ required: true, message: '请输入邮箱' }]"
        >
          <a-input 
            v-model="form.email" 
            placeholder="请输入邮箱"
            allow-clear
          />
        </a-form-item>
        
        <!-- 密码字段 -->
        <a-form-item 
          field="password" 
          label="密码" 
          :rules="[{ required: true, message: '请输入密码' }]"
        >
          <a-input-password 
            v-model="form.password" 
            placeholder="请输入密码"
          />
        </a-form-item>
        
        <!-- 确认密码字段 (仅注册时显示) -->
        <a-form-item 
          v-if="!isLogin" 
          field="confirmPassword" 
          label="确认密码" 
          :rules="[{ required: true, message: '请再次输入密码' }]"
        >
          <a-input-password 
            v-model="form.confirmPassword" 
            placeholder="请再次输入密码"
          />
        </a-form-item>
        
        <!-- 错误信息显示 -->
        <a-alert 
          v-if="errorMessage" 
          :show-icon="true" 
          type="error" 
          class="error-alert"
        >
          {{ errorMessage }}
        </a-alert>
        
        <!-- 提交按钮 -->
        <a-form-item>
          <a-button 
            type="primary" 
            html-type="submit" 
            :loading="loading" 
            long
          >
            {{ isLogin ? '登录' : '注册' }}
          </a-button>
        </a-form-item>
      </a-form>
      
      <!-- 切换登录/注册 -->
      <div class="auth-toggle">
        <a-typography-text>
          {{ isLogin ? '还没有账户?' : '已有账户?' }}
          <a @click="toggleAuthMode">
            {{ isLogin ? '立即注册' : '立即登录' }}
          </a>
        </a-typography-text>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { FormInstance } from '@arco-design/web-vue'

// 定义响应式数据
const isLogin = ref(true)
const loading = ref(false)
const errorMessage = ref('')
const formRef = ref<FormInstance | null>(null)

// 表单数据
const form = reactive({
  username: '',
  email: '',
  password: '',
  confirmPassword: ''
})

// 切换登录/注册模式
const toggleAuthMode = () => {
  isLogin.value = !isLogin.value
  errorMessage.value = ''
  // 清空表单数据
  form.username = ''
  form.email = ''
  form.password = ''
  form.confirmPassword = ''
  
  // 重置表单验证
  formRef.value?.resetFields()
}

// 处理表单提交
const handleSubmit = async () => {
  const isValid = await formRef.value?.validate()
  if (!isValid) return
  
  errorMessage.value = ''
  loading.value = true
  
  try {
    // 表单验证
    if (!isLogin.value) {
      // 注册时验证密码确认
      if (form.password !== form.confirmPassword) {
        throw new Error('两次输入的密码不一致')
      }
      
      // 注册逻辑
      await registerUser({
        username: form.username,
        email: form.email,
        password: form.password
      })
    } else {
      // 登录逻辑
      await loginUser({
        email: form.email,
        password: form.password
      })
    }
    
    // 成功处理
    console.log(isLogin.value ? '登录成功' : '注册成功')
    // 这里可以添加跳转逻辑或通知
    
  } catch (error: any) {
    errorMessage.value = error.message || (isLogin.value ? '登录失败' : '注册失败')
  } finally {
    loading.value = false
  }
}

// 模拟注册函数
const registerUser = (userData: { username: string; email: string; password: string }) => {
  return new Promise<void>((resolve, reject) => {
    setTimeout(() => {
      // 模拟注册逻辑
      if (userData.email === 'exist@example.com') {
        reject(new Error('该邮箱已被注册'))
      } else {
        resolve()
      }
    }, 1000)
  })
}

// 模拟登录函数
const loginUser = (credentials: { email: string; password: string }) => {
  return new Promise<void>((resolve, reject) => {
    setTimeout(() => {
      // 模拟登录逻辑
      if (credentials.email === 'user@example.com' && credentials.password === 'password') {
        resolve()
      } else {
        reject(new Error('邮箱或密码错误'))
      }
    }, 1000)
  })
}
</script>

<style scoped>
.auth-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f5f5f5;
  padding: 20px;
}

.auth-card {
  width: 100%;
  max-width: 400px;
}

.auth-header {
  margin-bottom: 24px;
}

.auth-toggle {
  text-align: center;
  margin-top: 20px;
}

.error-alert {
  margin-bottom: 20px;
}
</style>