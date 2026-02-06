# Hrevolve 黑盒端到端功能测试报告（E2E）
日期：2026-02-06  
测试类型：黑盒（UI/API 端到端 + 安全/兼容/性能基准抽样）  
版本基线：本地开发环境（Backend: https://localhost:5225，Frontend: http://localhost:5173）

## 1. 目标与结论
目标：覆盖所有用户可见输入/输出/业务流程与边界条件，包含正常路径、异常路径、错误处理、数据完整性、安全性、兼容性与性能基准，形成可上线质量评估结论。

质量评估结论（本轮）：
- 结论：有条件可上线（需先处理“会话/刷新”相关高风险项）
- 阻断项：0
- 高风险项：1
- 中风险项：2
- 低风险项：若干（见缺陷与风险章节）

## 2. 测试范围与依据
依据文档：
- [RequirementsDocument.md](file:///c:/Code/Hrevolve/Design/RequirementsDocument.md)
- [README.md](file:///c:/Code/Hrevolve/README.md)
- [Backend README.md](file:///c:/Code/Hrevolve/Backend/README.md)
- 前端路由与权限点： [router/index.ts](file:///c:/Code/Hrevolve/Frontend/hrevolve-web/src/router/index.ts)

范围（按用户可见模块）：
- 认证与会话：登录、登出、当前用户信息、异常提示
- 组织架构：组织树、组织单元详情、岗位列表
- 员工：列表、详情、历史时点查询、任职历史
- 考勤：签到/签退、我的考勤、统计
- 假期：假期类型、余额、请假申请、审批/驳回/撤销
- 薪酬：周期、薪资单查询、计算/审批/锁定（以接口可达性为准）
- 报销：报销类型、报销申请、审批
- 系统设置：权限/角色/用户、系统配置、审计日志导出（接口可达性为准）
- AI 助手：对话、历史查询/清除（Mock Provider）
- 多语言：locale 列表与消息包

不在本轮强制执行范围（仅做风险提示）：
- 跨浏览器手工兼容（Safari/移动端 WebView）、真实 SSO/MFA/扫码登录全链路、RAG 向量库与知识库内容治理、Webhook/SCIM 集成

## 3. 测试环境与测试数据
环境：
- OS：Windows（本机）
- Backend：ASP.NET Core Web API（HTTPS 5225）
- Frontend：Vue3 + Vite（5173），/api 代理到 backend
- 数据库：PostgreSQL（本地），已启用演示数据（demo 租户）
- AI Provider：Mock

测试账号（演示租户）：
- 普通员工：demo_user / demo123
- HR 管理员：demo_hr / demo123
- 系统管理员：demo_admin / demo123

## 4. 测试方法与覆盖维度
覆盖维度：
- 正常路径：核心业务主链路可完成、数据可回读
- 异常路径：参数缺失、越权访问、未登录访问、错误数据格式
- 错误处理：HTTP 状态码、错误码/错误消息结构
- 数据完整性：创建后可查询、审批后状态/余额/统计一致
- 安全性：认证鉴权、权限边界（401/403）、多租户隔离（JWT tenant_id 主导）
- 兼容性：前端依赖现代浏览器；本轮以 Chromium 系内核为目标（风险提示）
- 性能基准：关键查询接口在本地环境下的响应时间抽样（p50/p95）

## 5. 端到端测试用例与执行结果
说明：用例以“黑盒视角”描述（输入/输出/可观察行为）。实际执行以 API 驱动为主，UI 操作给出步骤与检查点。

状态定义：
- PASS：实际结果与预期一致
- FAIL：不一致（需提缺陷）
- BLOCK：被阻断（环境/依赖/前置缺失）
- NA：不适用/本轮未覆盖

### 5.1 认证与会话
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| AUTH-001 | 正确账号密码登录（员工） | 无 | POST /api/auth/login | 返回 accessToken、expiresIn>0 | 已验证返回 token、requiresMfa=false | PASS |  |
| AUTH-002 | 获取当前用户信息 | 已登录 | GET /api/auth/me | 返回 roles/permissions/tenantId/employeeId | demo_user 返回“普通员工”、tenantId/employeeId 非空 | PASS |  |
| AUTH-003 | 错误密码登录 | 无 | password 错误 | 400，code=INVALID_CREDENTIALS | 400，code=INVALID_CREDENTIALS | PASS |  |
| AUTH-004 | 未登录访问受保护资源 | 无 | GET /api/auth/me | 401 | 401 | PASS |  |
| AUTH-005 | refresh token | 无 | POST /api/auth/refresh | 返回新 token 或明确未实现 | 返回“Token刷新功能待实现” | PASS |  |
| AUTH-006 | logout | 已登录 | POST /api/auth/logout | 200，登出成功或明确未实现 | 200，message=Logout successful | PASS |  |

### 5.2 权限与越权（RBAC）
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| RBAC-001 | 员工访问员工列表（应禁止） | demo_user 登录 | GET /api/employees | 403 | 403，code=FORBIDDEN | PASS |  |
| RBAC-002 | HR 访问员工列表（应允许） | demo_hr 登录 | GET /api/employees | 200 + 列表 | 200，总数 total=123 | PASS |  |
| RBAC-003 | 员工提交请假（应允许） | demo_user 登录 | POST /api/leave/requests | 200/201 + 新申请 | 201，创建成功（跨月申请，id=5593c604-517d-4d8e-9ee8-d65a11e15549） | PASS |  |
| RBAC-004 | 员工审批请假（应禁止） | demo_user 登录 | POST /api/leave/requests/{id}/approve | 403 | 403，code=FORBIDDEN | PASS |  |

### 5.3 组织与员工（核心基础）
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| ORG-001 | 获取组织树 | 已登录（HR/管理员） | GET /api/organizations/tree | 200 + 树结构 | 200，root=Hrevolve 演示公司，节点数=11 | PASS |  |
| EMP-001 | 员工列表分页/关键字查询 | demo_hr 登录 | GET /api/employees?page=1&pageSize=20&keyword=赵 | 200 + 分页结构 | 200（本轮验证分页：pageSize=5，total=123；关键字过滤未抽样） | PASS |  |
| EMP-002 | 员工详情 | demo_hr 登录 | GET /api/employees/{id} | 200 + 详情字段 | 200（抽样 employeeId=0334fd52-5fab-4db0-bb40-c6f88347b2a0） | PASS |  |
| EMP-003 | 历史时点查询 | demo_hr 登录 | GET /api/employees/{id}/at-date?date=2024-01-01 | 200 + 对应历史记录 | 200 | PASS |  |
| EMP-004 | 任职历史查询 | demo_hr 登录 | GET /api/employees/{id}/job-history | 200 + 时间线 | 200，jobHistoryCount=1 | PASS |  |

### 5.4 考勤
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| ATT-001 | 签到 | demo_user 登录 | POST /api/attendance/check-in | 200 + 今日记录更新 | 200，message=check-in ok（方法=Web） | PASS |  |
| ATT-002 | 签退 | demo_user 登录 | POST /api/attendance/check-out | 200 + 今日记录更新 | 200，message=check-out ok（方法=Web） | PASS |  |
| ATT-003 | 查询我的考勤记录 | demo_user 登录 | GET /api/attendance/records/my | 200 + 列表 | 200，返回分页 items/total | PASS |  |
| ATT-004 | 月度统计 | demo_user 登录 | GET /api/attendance/stats/monthly?year=2026&month=2 | 200 + 统计 | 200，返回 workDays/totalHours/lateCount 等 | PASS |  |

### 5.5 假期
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| LEAVE-001 | 查询我的假期余额 | demo_user 登录 | GET /api/leave/balances/my | 200 + 余额项 | 200，balanceCount>0（年假 remainingDays=8） | PASS |  |
| LEAVE-002 | 提交请假申请（边界：跨月） | demo_user 登录 | POST /api/leave/requests（start/end 跨月） | 200/201 + 状态 Pending | 201，id=5593c604-517d-4d8e-9ee8-d65a11e15549，days=2 | PASS |  |
| LEAVE-003 | HR 审批请假 | demo_hr 登录 | POST /api/leave/requests/{id}/approve | 200 + 状态 Approved | 200，status=Approved；余额 pendingDays→0，usedDays+=2 | PASS | BB-001（已修复并回归） |
| LEAVE-004 | 撤销已提交申请 | demo_user 登录 | POST /api/leave/requests/{id}/cancel | 200 + 状态 Cancelled | 200，status=Cancelled（id=b024bb69-1cb0-4ad4-b86f-f2d1ca581af5） | PASS |  |
| LEAVE-005 | 非法日期范围（end < start） | demo_user 登录 | POST /api/leave/requests | 400 + 错误信息 | 400，code=VALIDATION_ERROR（EndDate 早于 StartDate） | PASS |  |

### 5.6 薪酬
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| PAY-001 | 查询我的薪资单 | demo_user 登录 | GET /api/payroll/records/my | 200 + 列表 | 200，itemCount=1 | PASS |  |
| PAY-002 | 周期列表 | demo_hr 登录 | GET /api/payroll/periods | 200 + 列表 | 200，periodCount=12，latest=2026-02 | PASS |  |

### 5.7 报销
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| EXP-001 | 查询报销类型 | demo_user 登录 | GET /api/expenses/types | 200 + 列表 | 200，返回默认类型集合 | PASS |  |
| EXP-002 | 提交报销申请（金额上限边界） | demo_user 登录 | POST /api/expenses/requests（amount=limit） | 200 + Pending | 200，status=Pending（id=a8a31518-a7af-4654-94c1-30a1dbf43301） | PASS |  |
| EXP-003 | HR/财务审批报销 | demo_hr 登录 | POST /api/expenses/requests/{id}/approve | 200 + Approved | 200，statusAfter=Approved | PASS | BB-002（已修复并回归） |

### 5.8 AI 助手与多语言
| 用例ID | 场景 | 前置 | 步骤（简） | 预期结果 | 实际结果 | 状态 | 缺陷 |
|---|---|---|---|---|---|---|---|
| AI-001 | 对话（Mock Provider） | 已登录 | POST /api/agent/chat | 200 + assistant 回复 | 200，reply 非空（Mock 回复） | PASS |  |
| AI-002 | 查询对话历史 | 已登录 | GET /api/agent/history | 200 + 列表 | 200，historyCount=2 | PASS |  |
| I18N-001 | 获取 locales | 无 | GET /api/localization/locales | 200 + locale 列表 | 200，返回 zh-CN/zh-TW/en-US | PASS |  |
| I18N-002 | 获取 zh-CN 消息包 | 无 | GET /api/localization/messages/zh-CN | 200 + key/value | 200，返回约 3.3KB 消息包 | PASS |  |

## 6. 性能基准（抽样）
说明：本地单机环境抽样，仅作为回归基线，不代表线上容量。

| 指标ID | 目标接口 | 样本数 | p50(ms) | p95(ms) | 错误率 | 备注 |
|---|---|---:|---:|---:|---:|---|
| PERF-001 | GET /api/organizations/tree | 30 | 264.1 | 911.1 | 0% | 本地环境抽样 |
| PERF-002 | GET /api/employees?page=1&pageSize=20 | 30 | 160.8 | 301.0 | 0% | 本地环境抽样 |
| PERF-003 | GET /api/leave/balances/my | 30 | 163.8 | 391.9 | 0% | 本地环境抽样 |

## 7. 缺陷统计与风险评级
缺陷严重级别定义：
- S1 阻断：核心链路不可用/数据严重错误/安全漏洞可被利用
- S2 高：主要功能受影响/越权风险/数据一致性问题
- S3 中：次要功能错误/提示不清晰/边界处理欠佳
- S4 低：UI/文案/体验问题

缺陷清单（本轮）：
| 缺陷ID | 标题 | 严重级别 | 影响范围 | 复现步骤摘要 | 当前状态 |
|---|---|---|---|---|---|
| BB-001 | 请假审批返回 500（并发更新 LeaveApprovals，影响主链路） | S2 | 假期审批 | 员工创建请假后，HR 调用 /leave/requests/{id}/approve，返回 500 INTERNAL_ERROR | 已修复并回归通过 |
| BB-002 | 报销审批返回 500（并发更新 ExpenseApprovals，影响主链路） | S2 | 报销审批 | 员工创建报销后，审批接口 /expenses/requests/{id}/approve 返回 500 INTERNAL_ERROR | 已修复并回归通过 |
| BB-003 | 报销模块缺少后端权限控制（可越权创建/修改/审批） | S1 | 安全/RBAC | 对 /api/expenses/* 接口未做细粒度权限校验，存在越权风险 | 已修复（补齐权限校验） |

风险评级（本轮）：
- 高风险：refresh token 为占位实现，logout 不会使 accessToken 立即失效（若发布到生产需明确会话/续期/撤销策略）
- 中风险：权限校验在前端 DEV 环境跳过（可能掩盖越权问题，需要确保生产配置与后端授权一致）
- 中风险：兼容性/性能未做多浏览器与压测（需上线前补齐最低保障）

## 8. 覆盖率分析（黑盒视角）
覆盖口径：按“用户可见模块 × 覆盖维度（正常/异常/边界/安全/性能）”统计。

| 模块 | 正常 | 异常 | 边界 | 安全 | 性能 |
|---|---:|---:|---:|---:|---:|
| 认证与会话 | 已覆盖 | 已覆盖 | NA | 已覆盖 | NA |
| 权限与越权 | NA | NA | NA | 已覆盖 | NA |
| 组织与员工 | 已覆盖 | NA | NA | 部分 | 部分 |
| 考勤 | 已覆盖 | NA | NA | 部分 | NA |
| 假期 | 已覆盖 | 已覆盖 | 已覆盖 | 已覆盖 | 部分 |
| 薪酬 | 已覆盖 | NA | NA | 部分 | NA |
| 报销 | 已覆盖 | NA | 已覆盖 | 已覆盖 | NA |
| AI 助手 | 已覆盖 | NA | NA | 部分 | NA |
| 多语言 | 已覆盖 | NA | NA | NA | NA |

后续建议：
- 将“NA/待执行”补齐为 PASS/FAIL，并在 CI 中固化为 API 冒烟回归
- 上线前补齐：生产环境权限拦截回归、关键链路性能（并发/长尾）、安全基线（OWASP Top 10）
