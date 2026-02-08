# Hrevolve 黑盒自动化执行结果
日期(UTC)：2026-02-07

## 统计
- 总用例：64
- 通过：61
- 失败：3

## 用例明细
| 用例ID | 名称 | 结果 | 期望 | 实际 | 耗时(ms) | 备注 |
|---|---|---|---|---|---:|---|
| TC-ENV-001 | 独立测试环境可运行 | PASS | 200 + Healthy | status=200 | 433 |  |
| TC-AUTH-001 | 正确账号密码登录（员工） | PASS | 200 + accessToken/refreshToken | status=200 | 335 |  |
| TC-AUTH-002 | 错误凭据登录 | PASS | 400 | status=400 | 144 |  |
| TC-AUTH-004 | Refresh Token 轮换 | PASS | 200 + new accessToken/refreshToken | status=200 | 250 |  |
| TC-AUTH-003 | 获取当前用户信息 | PASS | 200 + tenantId/roles/permissions | status=200 | 142 |  |
| TC-SEC-001 | 未登录访问受保护资源 | PASS | 401 | status=401 | 183 |  |
| TC-SEC-002 | 越权访问（员工访问员工列表） | PASS | 403 | status=403 | 105 |  |
| TC-I18N-001 | 获取 locales | PASS | 200 + locales | status=200 | 85 |  |
| TC-I18N-002 | 获取 zh-CN 消息包 | PASS | 200 + key/value | status=200 | 96 |  |
| TC-ORG-001 | 获取组织树 | PASS | 200 + tree | status=200 | 139 |  |
| TC-ORG-002 | 获取组织单元详情 | PASS | 200 + detail | status=200 | 106 |  |
| TC-ORG-003 | 查询组织下员工/岗位 | FAIL | 200 + list | emp=400,pos=200 | 247 |  |
| TC-ORG-004 | 岗位列表查询 | PASS | 200 + list | status=200 | 119 |  |
| TC-EMP-001 | 员工列表分页 | PASS | 200 + paging | status=200 | 136 |  |
| TC-EMP-002 | 员工详情 | PASS | 200 + detail | status=200 | 195 |  |
| TC-EMP-003 | 历史时点查询 | PASS | 200 | status=200 | 148 |  |
| TC-EMP-004 | 任职历史查询 | PASS | 200 + list | status=200 | 116 |  |
| TC-NFR-001 | 全局异常处理（不存在资源） | PASS | 404/400 且不泄露敏感堆栈 | status=404 | 124 |  |
| TC-ATT-008 | 班次列表 | PASS | 200 + list | status=200 | 110 |  |
| TC-ATT-001 | 签到 | PASS | 200 + today.checkInTime | check-in=200,today=200 | 208 |  |
| TC-ATT-002 | 签退 | PASS | 200 + today.checkOutTime | check-out=200,today=200 | 209 |  |
| TC-ATT-003 | 查询我的考勤记录 | PASS | 200 + paging | status=200 | 174 |  |
| TC-ATT-004 | 查询今日考勤摘要 | PASS | 200 | status=200 | 92 |  |
| TC-ATT-005 | 月度统计 | PASS | 200 + stats | status=200 | 181 |  |
| TC-ATT-006 | HR 查询全量记录与部门统计 | FAIL | 200 | records=200,dept=404 | 210 |  |
| TC-ATT-007 | 考勤纠正申请与审批（可达性） | PASS | 200 | apply=200,approve=200 | 206 |  |
| TC-LEAVE-001 | 假期类型列表 | PASS | 200 + list | status=200 | 135 |  |
| TC-LEAVE-002 | 查询我的假期余额 | PASS | 200 + balances | status=200 | 141 |  |
| TC-LEAVE-003 | HR 查询指定员工余额 | PASS | 200 | status=200 | 123 |  |
| TC-LEAVE-007 | 非法日期范围校验 | PASS | 400 | status=400 | 88 |  |
| TC-LEAVE-004C | 提交请假（跨月） | PASS | 201/200 + id | status=201 | 259 |  |
| TC-LEAVE-005 | 请假列表/详情 | PASS | 200 + contains id | list=200,detail=200 | 420 |  |
| TC-LEAVE-006 | 请假审批 | PASS | 200 + status=Approved | approve=200,after=200 | 358 |  |
| TC-EXP-001 | 报销类型列表/详情 | PASS | 200 | list=200,detail=200 | 204 |  |
| TC-EXP-002 | 提交报销申请（金额边界） | PASS | 200/201 + id | status=200 | 211 |  |
| TC-NFR-002 | 输入校验（负金额报销） | FAIL | 400 | status=200 | 176 |  |
| TC-EXP-003 | 报销列表查询 | PASS | 200 | status=200 | 195 |  |
| TC-EXP-004 | 报销审批 | PASS | 200 | status=200 | 199 |  |
| TC-PAY-001 | 周期列表 | PASS | 200 | status=200 | 116 |  |
| TC-PAY-002 | 薪资项列表 | PASS | 200 | status=200 | 106 |  |
| TC-PAY-006 | 周期计算/审批/锁定（接口可达） | PASS | 200 | calc=200,approve=200,lock=200 | 298 |  |
| TC-PAY-003 | 员工查询我的薪资单 | PASS | 200 | status=200 | 164 |  |
| TC-PAY-004 | HR 查询薪资记录列表与详情 | PASS | 200 | list=200,detail=200 | 269 |  |
| TC-PAY-005 | HR 查询指定员工薪资记录 | PASS | 200 | status=200 | 102 |  |
| TC-SCH-001 | 查询排班与统计 | PASS | 200 | list=200,stats=200 | 236 |  |
| TC-SCH-002 | 获取可排班员工与班次模板 | PASS | 200 | emp=200,tpl=200 | 206 |  |
| TC-SCH-003 | 分配排班写路径（可达性） | PASS | 200 | status=200 | 99 |  |
| TC-SET-001 | 权限清单 | PASS | 200 | status=200 | 96 |  |
| TC-SET-002 | 角色列表与详情 | PASS | 200 | list=200,detail=NA | 134 |  |
| TC-SET-003 | 角色权限查询/更新（临时角色） | PASS | 200 | create=200,update=200,delete=200 | 484 |  |
| TC-SET-004 | 用户列表查询 | PASS | 200 | status=200 | 155 |  |
| TC-SET-005 | 系统配置查询 | PASS | 200 | status=200 | 105 |  |
| TC-APPR-001 | 审批流配置 CRUD（可达性） | PASS | 200 | list=200,create=200,delete=200 | 397 |  |
| TC-AUD-001 | 审计日志查询与导出 | PASS | 200 | list=200,export=200 | 196 |  |
| TC-COMP-004 | 获取当前租户信息 | PASS | 200 | status=200 | 102 |  |
| TC-COMP-001 | 成本中心树查询 | PASS | 200 | tree=200,create=200 | 223 |  |
| TC-COMP-002 | 标签列表查询 | PASS | 200 | list=200,create=200 | 196 |  |
| TC-COMP-003 | 打卡设备列表查询 | PASS | 200 | list=200,create=200 | 214 |  |
| TC-INS-001 | 保险接口可达 | PASS | 200 | plans=200,benefits=200,stats=200 | 300 |  |
| TC-TAX-001 | 个税接口可达 | PASS | 200 | profiles=200,records=200,settings=200 | 306 |  |
| TC-AI-001 | AI 对话（Mock） | PASS | 200 + reply | status=200 | 633 |  |
| TC-AI-002 | AI 历史查询/清理 | PASS | 200 + cleared | get1=200,del=200,get2=200 | 307 |  |
| TC-AUTH-005 | Logout（撤销 RefreshToken） | PASS | logout 200 且 refresh 401 | logout=200,refresh=401 | 758 |  |
| TC-PERF-001 | 性能基准抽样 | PASS | 每接口 30 次采样，输出 p50/p95 | [
  {
    "name": "OrganizationsTree",
    "path": "/api/Organizations/tree",
    "samples": 30,
    "p50": 148.0,
    "p95": 175.0,
    "errorRate": 0.0
  },
  {
    "name": "EmployeesList",
    "path": "/api/Employees?page=1&pageSize=20",
    "samples": 30,
    "p50": 134.0,
    "p95": 166.0,
    "errorRate": 0.0
  },
  {
    "name": "LeaveBalancesMy",
    "path": "/api/Leave/balances/my",
    "samples": 30,
    "p50": 133.0,
    "p95": 172.0,
    "errorRate": 0.0
  }
] | 13031 |  |

## 性能基准（抽样）
| 指标 | 接口 | 样本数 | p50(ms) | p95(ms) | 错误率 |
|---|---|---:|---:|---:|---:|
| OrganizationsTree | /api/Organizations/tree | 30 | 148 | 175 | 0% |
| EmployeesList | /api/Employees?page=1&pageSize=20 | 30 | 134 | 166 | 0% |
| LeaveBalancesMy | /api/Leave/balances/my | 30 | 133 | 172 | 0% |
