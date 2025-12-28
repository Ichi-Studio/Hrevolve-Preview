# TypeScript 类型错误修复清单

## ✅ 已修复
1. **i18n/index.ts** - 动态索引访问类型错误
2. **stores/auth.ts** - User 对象缺少 displayName 和类型转换
3. **layouts/MainLayout.vue** - 语言切换类型错误

## 🔧 待修复的关键错误

### 高优先级（影响核心功能）

#### 1. ClockDevicesView.vue
- 问题：使用了不存在的 `serialNumber` 和 `status` 字段
- 修复：移除或使用正确的字段名

#### 2. UsersView.vue  
- 问题：User 类型缺少 `phone` 和 `isActive` 字段
- 修复：扩展 User 类型或使用 UserExtended

#### 3. ExpenseRequestsView.vue
- 问题：使用了不存在的 `title` 和 `totalAmount` 字段
- 修复：使用正确的字段名 `description` 和 `amount`

#### 4. ScheduleCalendarView.vue / ScheduleTableView.vue
- 问题：API 返回 PageResponse 但代码期望数组
- 修复：使用 `res.data.items` 而不是 `res.data`

#### 5. InsuranceViews (多个文件)
- 问题：字段名不匹配（planId vs insurancePlanId）
- 修复：统一使用 insurancePlanId

### 中优先级（影响部分功能）

#### 6. TaxProfilesView.vue
- 问题：使用 `taxNumber` 而不是 `taxId`
- 修复：统一字段名

#### 7. ApprovalFlowsView.vue
- 问题：使用小写的枚举值（'leave' vs 'Leave'）
- 修复：使用正确的大写枚举值

#### 8. AuditLogsView.vue
- 问题：后端返回的字段名与类型定义不匹配
- 修复：映射字段名或更新类型定义

### 低优先级（不影响功能）

#### 9. 未使用的变量
- DashboardView.vue: `ref` 未使用
- LeaveView.vue: `getStatusType` 未使用
- MainLayout.vue: `currentLocale` 未使用

## 📝 修复建议

### 方案 A：快速修复（推荐）
为每个视图组件添加类型断言和字段映射，保持现有逻辑不变。

### 方案 B：规范修复
1. 统一前后端字段命名规范
2. 更新类型定义文件
3. 修改所有使用这些类型的组件

### 方案 C：渐进式修复
1. 先修复高优先级错误（核心功能）
2. 添加 `// @ts-ignore` 注释临时跳过低优先级错误
3. 后续逐步规范化

## 🎯 推荐执行顺序
1. 修复 auth 和 i18n（已完成）
2. 修复 Schedule 相关视图（影响排班功能）
3. 修复 Expense 和 Insurance 视图（影响报销和保险）
4. 修复其他视图
5. 清理未使用的变量
