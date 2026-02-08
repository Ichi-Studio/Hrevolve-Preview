# Hrevolve 黑盒测试覆盖矩阵（需求条目 × 用例）
日期：2026-02-07  
覆盖口径：以“可测试需求条目”为单位统计覆盖率（目标 ≥ 90%）

状态定义：PASS / FAIL / BLOCK / NA（未实现或明确不在范围）

| 需求ID | 模块 | 需求条目（黑盒可观察） | 优先级 | 依据 | 关联端点（抽样） | 用例ID | 状态 |
|---|---|---|---|---|---|---|---|
| FR-AUTH-001 | 认证 | 账号密码登录成功返回 token | P0 | SRS+实现 | POST /api/Auth/login | TC-AUTH-001 | PASS |
| FR-AUTH-002 | 认证 | 错误凭据返回明确错误码 | P0 | SRS+实现 | POST /api/Auth/login | TC-AUTH-002 | PASS |
| FR-AUTH-003 | 会话 | 获取当前用户信息（含租户/角色/权限） | P0 | 实现 | GET /api/Auth/me | TC-AUTH-003 | PASS |
| FR-AUTH-004 | 会话 | Refresh Token 轮换与过期处理 | P0 | 实现 | POST /api/Auth/refresh | TC-AUTH-004 | PASS |
| FR-AUTH-005 | 会话 | Logout 行为可观察且可回归验证 | P0 | 实现 | POST /api/Auth/logout | TC-AUTH-005 | PASS |
| FR-SEC-001 | 安全 | 未登录访问受保护资源返回 401 | P0 | 实现 | GET /api/Employees | TC-SEC-001 | PASS |
| FR-SEC-002 | 安全 | 无权限访问返回 403（RBAC） | P0 | 实现 | GET /api/Employees（employee） | TC-SEC-002 | PASS |
| FR-TEN-001 | 多租户 | JWT/请求上下文识别租户并隔离数据 | P0 | SRS+实现 | 任意业务接口 | TC-TEN-001 | NA |
| FR-I18N-001 | 多语言 | 获取 locale 列表 | P1 | SRS+实现 | GET /api/Localization/locales | TC-I18N-001 | PASS |
| FR-I18N-002 | 多语言 | 获取指定 locale 消息包 | P1 | SRS+实现 | GET /api/Localization/messages/{locale} | TC-I18N-002 | PASS |
| FR-ORG-001 | 组织 | 获取组织树结构 | P0 | SRS+实现 | GET /api/Organizations/tree | TC-ORG-001 | PASS |
| FR-ORG-002 | 组织 | 获取组织单元详情 | P1 | 实现 | GET /api/Organizations/{id} | TC-ORG-002 | PASS |
| FR-ORG-003 | 组织 | 查询组织下员工/岗位 | P1 | 实现 | GET /api/Organizations/{id}/employees, /positions | TC-ORG-003 | FAIL |
| FR-ORG-004 | 岗位 | 获取岗位列表 | P1 | 实现 | GET /api/Organizations/positions | TC-ORG-004 | PASS |
| FR-EMP-001 | 员工 | 员工列表分页/过滤（最小校验） | P0 | SRS+实现 | GET /api/Employees | TC-EMP-001 | PASS |
| FR-EMP-002 | 员工 | 员工详情字段可回读 | P0 | SRS+实现 | GET /api/Employees/{id} | TC-EMP-002 | PASS |
| FR-EMP-003 | 员工 | 历史时点查询（Effective Dating） | P0 | SRS+实现 | GET /api/Employees/{id}/at-date | TC-EMP-003 | PASS |
| FR-EMP-004 | 员工 | 任职历史查询（时间线） | P0 | SRS+实现 | GET /api/Employees/{id}/job-history | TC-EMP-004 | PASS |
| FR-EMP-005 | 员工 | 创建员工（写路径与校验） | P1 | 实现 | POST /api/Employees | TC-EMP-005 | NA |
| FR-EMP-006 | 员工 | 终止员工（离职）可回读 | P1 | 实现 | POST /api/Employees/{id}/terminate | TC-EMP-006 | NA |
| FR-ATT-001 | 考勤 | 签到成功，状态可回读 | P0 | SRS+实现 | POST /api/Attendance/check-in | TC-ATT-001 | PASS |
| FR-ATT-002 | 考勤 | 签退成功，工时可计算 | P0 | SRS+实现 | POST /api/Attendance/check-out | TC-ATT-002 | PASS |
| FR-ATT-003 | 考勤 | 查询我的考勤记录分页返回结构一致 | P0 | 实现 | GET /api/Attendance/records/my | TC-ATT-003 | PASS |
| FR-ATT-004 | 考勤 | 查询今日考勤摘要 | P1 | 实现 | GET /api/Attendance/today | TC-ATT-004 | PASS |
| FR-ATT-005 | 考勤 | 月度统计返回关键字段 | P0 | SRS+实现 | GET /api/Attendance/stats/monthly | TC-ATT-005 | PASS |
| FR-ATT-006 | 考勤 | HR 查询全量记录/部门统计 | P1 | 实现 | GET /api/Attendance/records, /statistics/department/{id} | TC-ATT-006 | FAIL |
| FR-ATT-007 | 考勤 | 考勤纠正申请与审批（状态流转） | P1 | 实现 | POST /api/Attendance/correction, /approve | TC-ATT-007 | PASS |
| FR-ATT-008 | 排班关联 | 班次列表可查询 | P1 | 实现 | GET /api/Attendance/shifts | TC-ATT-008 | PASS |
| FR-LEAVE-001 | 假期 | 获取假期类型列表 | P1 | SRS+实现 | GET /api/Leave/types | TC-LEAVE-001 | PASS |
| FR-LEAVE-002 | 假期 | 查询我的假期余额 | P0 | SRS+实现 | GET /api/Leave/balances/my | TC-LEAVE-002 | PASS |
| FR-LEAVE-003 | 假期 | HR 查询指定员工余额 | P1 | 实现 | GET /api/Leave/balances/{employeeId} | TC-LEAVE-003 | PASS |
| FR-LEAVE-004 | 假期 | 提交请假申请成功（含边界组合抽样） | P0 | SRS+实现 | POST /api/Leave/requests | TC-LEAVE-004A~D | PASS |
| FR-LEAVE-005 | 假期 | 请假列表/详情可查询 | P0 | 实现 | GET /api/Leave/requests, /{id}, /my | TC-LEAVE-005 | PASS |
| FR-LEAVE-006 | 假期 | 审批/驳回/撤销状态机一致 | P0 | SRS+实现 | POST /api/Leave/requests/{id}/approve|reject|cancel | TC-LEAVE-006 | PASS |
| FR-LEAVE-007 | 假期 | 非法日期范围触发校验错误 | P0 | 实现 | POST /api/Leave/requests | TC-LEAVE-007 | PASS |
| FR-EXP-001 | 报销 | 获取报销类型列表/详情 | P1 | SRS+实现 | GET /api/Expenses/types, /{id} | TC-EXP-001 | PASS |
| FR-EXP-002 | 报销 | 提交报销申请（金额边界） | P0 | SRS+实现 | POST /api/Expenses/requests | TC-EXP-002 | PASS |
| FR-EXP-003 | 报销 | 报销列表查询 | P1 | 实现 | GET /api/Expenses/requests | TC-EXP-003 | PASS |
| FR-EXP-004 | 报销 | 审批流程（状态流转） | P0 | 实现 | POST /api/Expenses/requests/{id}/approve | TC-EXP-004 | PASS |
| FR-PAY-001 | 薪酬 | 周期列表可查询 | P0 | 实现 | GET /api/Payroll/periods | TC-PAY-001 | PASS |
| FR-PAY-002 | 薪酬 | 薪资项列表可查询 | P1 | 实现 | GET /api/Payroll/items | TC-PAY-002 | PASS |
| FR-PAY-003 | 薪酬 | 员工查询我的薪资单 | P0 | 实现 | GET /api/Payroll/records/my | TC-PAY-003 | PASS |
| FR-PAY-004 | 薪酬 | HR 查询薪资记录列表与详情 | P1 | 实现 | GET /api/Payroll/records, /{id} | TC-PAY-004 | PASS |
| FR-PAY-005 | 薪酬 | HR 查询指定员工薪资记录 | P1 | 实现 | GET /api/Payroll/records/employee/{employeeId} | TC-PAY-005 | PASS |
| FR-PAY-006 | 薪酬 | 计算/审批/锁定接口可达且返回一致 | P1 | 实现 | POST /api/Payroll/periods/{id}/calculate|approve|lock | TC-PAY-006 | PASS |
| FR-SCH-001 | 排班 | 查询排班（列表/统计） | P1 | 实现 | GET /api/Schedules, /stats | TC-SCH-001 | PASS |
| FR-SCH-002 | 排班 | 获取可排班员工/班次模板 | P1 | 实现 | GET /api/Schedules/schedulable-employees, /shift-templates | TC-SCH-002 | PASS |
| FR-SCH-003 | 排班 | 分配排班写路径与回读 | P1 | 实现 | POST /api/Schedules/assign | TC-SCH-003 | PASS |
| FR-COMP-001 | 公司 | 成本中心树/CRUD 可用 | P1 | 实现 | /api/Company/cost-centers* | TC-COMP-001 | PASS |
| FR-COMP-002 | 公司 | 标签 CRUD 可用 | P1 | 实现 | /api/Company/tags* | TC-COMP-002 | PASS |
| FR-COMP-003 | 公司 | 打卡设备 CRUD 可用 | P1 | 实现 | /api/Company/clock-devices* | TC-COMP-003 | PASS |
| FR-COMP-004 | 多租户 | 获取当前租户信息 | P1 | 实现 | GET /api/Company/tenant | TC-COMP-004 | PASS |
| FR-SET-001 | 设置 | 权限清单可查询 | P1 | 实现 | GET /api/Settings/permissions | TC-SET-001 | PASS |
| FR-SET-002 | 设置 | 角色列表/详情可查询 | P1 | 实现 | GET /api/Settings/roles, /{id} | TC-SET-002 | PASS |
| FR-SET-003 | 设置 | 角色权限查询/更新 | P1 | 实现 | GET/PUT /api/Settings/roles/{id}/permissions | TC-SET-003 | PASS |
| FR-SET-004 | 设置 | 用户列表/启用禁用/重置密码 | P1 | 实现 | /api/Settings/users* | TC-SET-004 | NA |
| FR-SET-005 | 设置 | 系统配置查询 | P2 | 实现 | GET /api/Settings/system-configs | TC-SET-005 | PASS |
| FR-SET-006 | 审计 | 审计日志查询与导出 | P1 | SRS+实现 | GET /api/Settings/audit-logs, /export | TC-AUD-001 | PASS |
| FR-SET-007 | 流程 | 审批流配置 CRUD 可用 | P2 | 实现 | /api/Settings/approval-flows* | TC-APPR-001 | PASS |
| FR-AI-001 | AI | 对话接口可用（Mock） | P1 | SRS+实现 | POST /api/Agent/chat | TC-AI-001 | PASS |
| FR-AI-002 | AI | 对话历史查询/清理 | P1 | 实现 | GET/DELETE /api/Agent/history | TC-AI-002 | PASS |
| FR-INS-001 | 保险 | 保险计划/参保/统计接口可达 | P2 | 实现 | /api/Insurance/* | TC-INS-001 | PASS |
| FR-TAX-001 | 个税 | 个税档案/记录/导出/设置接口可达 | P2 | 实现 | /api/Tax/* | TC-TAX-001 | PASS |
| NFR-ERR-001 | 可靠性 | 全局异常处理：返回结构一致且不泄露敏感栈信息 | P0 | 实现 | 任意异常触发 | TC-NFR-001 | PASS |
| NFR-PERF-001 | 性能 | 组织树/员工列表/假期余额关键查询 p50/p95 抽样 | P1 | SRS | GET /Organizations/tree 等 | TC-PERF-001 | PASS |
| NFR-SEC-003 | 安全 | 输入校验：常见非法输入返回 400/校验详情 | P1 | 实现 | POST /Leave/requests 等 | TC-NFR-002 | FAIL |
| NFR-PORT-001 | 可移植性 | 独立测试环境（SQLite + MemoryCache）可运行 | P1 | 本计划 | 环境搭建 | TC-ENV-001 | PASS |

说明：
- 用例明细见：[BlackBoxTestCases_2026-02-07.md](file:///d:/HomeCode/Hrevolve/TestReports/BlackBoxTestCases_2026-02-07.md)
- 执行结果与统计见：[BlackBoxTestReport_2026-02-07.md](file:///d:/HomeCode/Hrevolve/TestReports/BlackBoxTestReport_2026-02-07.md)
