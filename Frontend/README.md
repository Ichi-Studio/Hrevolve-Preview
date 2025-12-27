# Hrevolve 前端

企业级SaaS人力资源管理系统前端项目。

## 技术栈

- **框架**: Vue 3 + TypeScript
- **构建工具**: Vite (Rolldown)
- **UI组件库**: Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **HTTP客户端**: Axios
- **国际化**: Vue I18n
- **图表**: ECharts

## 项目结构

```
hrevolve-web/
├── src/
│   ├── api/              # API接口模块
│   ├── assets/           # 静态资源
│   ├── i18n/             # 国际化配置
│   ├── layouts/          # 布局组件
│   ├── router/           # 路由配置
│   ├── stores/           # Pinia状态管理
│   ├── styles/           # 全局样式
│   ├── types/            # TypeScript类型定义
│   ├── views/            # 页面组件
│   ├── App.vue           # 根组件
│   └── main.ts           # 入口文件
├── .env.development      # 开发环境配置
├── .env.production       # 生产环境配置
└── vite.config.ts        # Vite配置
```

## 功能模块

- 🏠 工作台仪表盘
- 👤 员工自助（个人信息、考勤、假期、薪资）
- 🤖 AI智能助手
- 🏢 组织架构管理
- 👥 员工管理
- ⏰ 考勤管理
- 📅 假期管理
- 💰 薪酬管理

## 快速开始

```bash
# 进入前端目录
cd Frontend/hrevolve-web

# 安装依赖
npm install

# 启动开发服务器
npm run dev

# 构建生产版本
npm run build
```

## 开发说明

- 开发服务器默认运行在 `http://localhost:5173`
- API请求会代理到后端 `http://localhost:5000`
- 支持中英文切换

## 环境变量

| 变量 | 说明 |
|------|------|
| `VITE_API_BASE_URL` | API基础路径 |
