# Hrevolve 黑盒测试用例集（功能/边界/异常/E2E）
日期：2026-02-07  
用例规范：每条用例包含 前置条件 / 输入数据 / 执行步骤 / 预期结果 / 通过准则

## 0. 通用约定
- 基础地址（本地）：`https://localhost:5225`
- 租户：演示租户 `demo`
- 账号：
  - 普通员工：`demo_user / demo123`
  - HR 管理员：`demo_hr / demo123`
  - 系统管理员：`demo_admin / demo123`
- 认证：`Authorization: Bearer <accessToken>`
- HTTPS 证书：开发证书未必可信，工具侧需跳过证书校验

---

## 1. 认证与会话
### TC-AUTH-001 正确账号密码登录（员工）
- 前置条件：无
- 输入数据：username=demo_user；password=demo123；tenant=demo
- 执行步骤：
  1. 调用 `POST /api/Auth/login`，提交账号密码
- 预期结果：返回 200，包含 accessToken、refreshToken、expiresIn>0、requiresMfa=false（或明确的 MFA 标识）
- 通过准则：HTTP 状态码与返回字段满足预期，token 可用于后续受保护接口
- 设计技术：等价类（有效凭据）

### TC-AUTH-002 错误密码登录
- 前置条件：无
- 输入数据：username=demo_user；password=wrong
- 执行步骤：
  1. `POST /api/Auth/login`
- 预期结果：返回 400；错误码/错误消息可定位为“凭据无效”
- 通过准则：状态码为 400，错误结构字段存在且稳定（code/message/details）
- 设计技术：等价类（无效凭据）

### TC-AUTH-003 获取当前用户信息
- 前置条件：已完成 TC-AUTH-001 获取 accessToken
- 输入数据：Bearer token
- 执行步骤：
  1. `GET /api/Auth/me`
- 预期结果：返回 200，包含 tenantId、roles、permissions、employeeId（若绑定）
- 通过准则：字段存在且类型正确；tenantId 与演示租户一致；权限随角色变化
- 设计技术：决策表（角色×返回权限集）

### TC-AUTH-004 Refresh Token 轮换
- 前置条件：已登录并获取 refreshToken
- 输入数据：refreshToken
- 执行步骤：
  1. `POST /api/Auth/refresh`
  2. 使用新 accessToken 调用 `GET /api/Auth/me`
- 预期结果：返回新的 accessToken/refreshToken；旧 token 的行为符合预期（可用/不可用取决于设计）
- 通过准则：refresh 返回成功且 expiresIn>0；新 token 可访问受保护资源
- 设计技术：状态转换（token 生命周期）

### TC-AUTH-005 Logout 与访问撤销
- 前置条件：已登录
- 输入数据：Bearer token
- 执行步骤：
  1. `POST /api/Auth/logout`
  2. 立刻用相同 accessToken 调用 `GET /api/Auth/me`
- 预期结果：登出接口返回 200；撤销策略清晰（例如：后续访问返回 401）
- 通过准则：与系统定义一致且可重复验证
- 设计技术：状态转换（登录→登出）

---

## 2. 安全（认证/鉴权/多租户）
### TC-SEC-001 未登录访问受保护资源
- 前置条件：无
- 输入数据：无 Authorization header
- 执行步骤：
  1. `GET /api/Employees`
- 预期结果：401
- 通过准则：返回 401；不泄露内部异常堆栈
- 设计技术：因果图（未登录→401）

### TC-SEC-002 越权访问（员工访问 HR 资源）
- 前置条件：以 demo_user 登录
- 输入数据：demo_user 的 token
- 执行步骤：
  1. `GET /api/Employees`
- 预期结果：403（或 401/403 按系统规范）
- 通过准则：返回 403 且错误结构明确（code=FORBIDDEN 等）
- 设计技术：决策表（角色×资源×操作）

### TC-TEN-001 多租户隔离（租户上下文）
- 前置条件：系统存在多租户数据或至少租户标识贯穿链路
- 输入数据：demo_user token +（可选）显式 tenant header/query
- 执行步骤：
  1. 以 demo_user 登录并访问业务接口（如 `GET /api/Auth/me`、`GET /api/Company/tenant`）
  2.（可选）构造 tenant header 与 token tenant 不一致的请求
- 预期结果：租户以 token/上下文为准；不允许跨租户读写
- 通过准则：不出现跨租户数据泄露；不一致时返回 400/403
- 设计技术：因果图（租户输入→隔离输出）

---

## 3. 多语言
### TC-I18N-001 获取 locales
- 前置条件：无
- 输入数据：无
- 执行步骤：
  1. `GET /api/Localization/locales`
- 预期结果：200 + locale 列表（至少包含 zh-CN/en-US）
- 通过准则：返回列表非空，值符合 locale 规范
- 设计技术：等价类（无参 GET）

### TC-I18N-002 获取消息包
- 前置条件：无
- 输入数据：locale=zh-CN
- 执行步骤：
  1. `GET /api/Localization/messages/zh-CN`
- 预期结果：200 + key/value（或结构化字典）
- 通过准则：返回非空且可被前端消费（类型一致）
- 设计技术：边界值（locale 合法/非法可扩展）

---

## 4. 组织与岗位
### TC-ORG-001 获取组织树
- 前置条件：以 demo_hr 或 demo_admin 登录
- 输入数据：HR/Admin token
- 执行步骤：
  1. `GET /api/Organizations/tree`
- 预期结果：200，树结构可解析（root + children）
- 通过准则：返回结构稳定；节点数>0
- 设计技术：等价类（有效 token）

### TC-ORG-002 获取组织单元详情
- 前置条件：已通过 TC-ORG-001 获取任意组织单元 id
- 输入数据：organizationUnitId
- 执行步骤：
  1. `GET /api/Organizations/{id}`
- 预期结果：200 + 详情字段（name/code/type/path/parentId 等）
- 通过准则：字段类型正确；id 回读一致
- 设计技术：状态迁移（先查树再查详情）

### TC-ORG-003 查询组织下员工与岗位
- 前置条件：已获取组织单元 id
- 输入数据：organizationUnitId
- 执行步骤：
  1. `GET /api/Organizations/{id}/employees`
  2. `GET /api/Organizations/{id}/positions`
- 预期结果：200 + 列表/分页结构
- 通过准则：返回可用；员工/岗位数量与演示数据合理
- 设计技术：等价类（有效 id）

### TC-ORG-004 岗位列表查询
- 前置条件：以 demo_hr 登录
- 输入数据：HR token
- 执行步骤：
  1. `GET /api/Organizations/positions`
- 预期结果：200 + 列表（含启用/停用岗位）
- 通过准则：返回非空；字段完整
- 设计技术：等价类

---

## 5. 员工
### TC-EMP-001 员工列表（分页）
- 前置条件：以 demo_hr 登录
- 输入数据：page=1，pageSize=20
- 执行步骤：
  1. `GET /api/Employees?page=1&pageSize=20`
- 预期结果：200 + items/total/page/pageSize（或等价结构）
- 通过准则：items 数量≤pageSize；total≥items.Count
- 设计技术：边界值（page/pageSize）

### TC-EMP-002 员工详情
- 前置条件：已通过 TC-EMP-001 获取 employeeId
- 输入数据：employeeId
- 执行步骤：
  1. `GET /api/Employees/{id}`
- 预期结果：200 + 详情字段
- 通过准则：关键字段（姓名、工号、部门/岗位、状态）可回读
- 设计技术：状态迁移

### TC-EMP-003 历史时点查询
- 前置条件：存在任职历史数据（演示数据默认具备）
- 输入数据：employeeId；date=过去某日
- 执行步骤：
  1. `GET /api/Employees/{id}/at-date?date=2024-01-01`
- 预期结果：200 + 对应历史快照
- 通过准则：返回数据与该时点一致（如职位/薪资字段存在且可解释）
- 设计技术：状态转换（SCD Type 2）

### TC-EMP-004 任职历史查询
- 前置条件：同 TC-EMP-003
- 输入数据：employeeId
- 执行步骤：
  1. `GET /api/Employees/{id}/job-history`
- 预期结果：200 + 时间线列表（按 effectiveStart/effectiveEnd 排序）
- 通过准则：时间范围连续或符合业务规则；当前记录 end=9999-12-31（若采用该策略）
- 设计技术：边界值（有效期边界）

### TC-EMP-005 创建员工（写路径）
- 前置条件：以 demo_hr 登录
- 输入数据：新员工最小必填字段（工号唯一、姓名、入职日期等）
- 执行步骤：
  1. `POST /api/Employees`
  2. `GET /api/Employees/{newId}` 回读
- 预期结果：创建成功（200/201）；回读一致
- 通过准则：新员工可在列表中查询到；无数据损坏
- 设计技术：等价类（有效输入）+ 边界值（唯一性）

### TC-EMP-006 员工终止（离职）
- 前置条件：存在可终止员工；以 demo_hr 登录
- 输入数据：employeeId；terminationDate=今日或过去
- 执行步骤：
  1. `POST /api/Employees/{id}/terminate`
  2. `GET /api/Employees/{id}` 回读状态
- 预期结果：状态变更为离职/终止；终止日期可回读
- 通过准则：状态与日期一致；不影响其他员工数据
- 设计技术：状态转换

---

## 6. 考勤
### TC-ATT-001 签到
- 前置条件：以 demo_user 登录
- 输入数据：可选 location/method（若接口支持）
- 执行步骤：
  1. `POST /api/Attendance/check-in`
  2. `GET /api/Attendance/today`
- 预期结果：签到成功；today 返回 checkInTime 已填充
- 通过准则：返回 200；today 中的状态可解释
- 设计技术：状态转换（未签到→已签到）

### TC-ATT-002 签退
- 前置条件：先完成 TC-ATT-001
- 输入数据：无/可选
- 执行步骤：
  1. `POST /api/Attendance/check-out`
  2. `GET /api/Attendance/today`
- 预期结果：签退成功；today 返回 checkOutTime 与工时相关字段
- 通过准则：checkOutTime ≥ checkInTime；工时/加班字段类型正确
- 设计技术：状态转换（已签到→已签退）

### TC-ATT-003 查询我的考勤记录
- 前置条件：以 demo_user 登录
- 输入数据：page/pageSize
- 执行步骤：
  1. `GET /api/Attendance/records/my?page=1&pageSize=20`
- 预期结果：200 + 分页列表
- 通过准则：结构稳定；items 可解析
- 设计技术：等价类

### TC-ATT-004 查询今日考勤
- 前置条件：以 demo_user 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Attendance/today`
- 预期结果：200 + 今日摘要
- 通过准则：字段存在；无异常 5xx
- 设计技术：等价类

### TC-ATT-005 月度统计
- 前置条件：以 demo_user 登录
- 输入数据：year=当前年；month=当前月
- 执行步骤：
  1. `GET /api/Attendance/stats/monthly?year=2026&month=2`
- 预期结果：200，包含 workDays/totalHours/lateCount 等关键字段
- 通过准则：字段存在且类型正确；数值合理（非负）
- 设计技术：边界值（月=1/12 可扩展）

### TC-ATT-006 HR 查询全量记录与部门统计
- 前置条件：以 demo_hr 登录；已获取部门/组织 id
- 输入数据：departmentId
- 执行步骤：
  1. `GET /api/Attendance/records?page=1&pageSize=20`
  2. `GET /api/Attendance/statistics/department/{departmentId}`
- 预期结果：200 + 可解析结构
- 通过准则：无越权；返回结构稳定
- 设计技术：决策表（角色×资源）

### TC-ATT-007 考勤纠正申请与审批
- 前置条件：以 demo_user 登录创建纠正；以 demo_hr 登录审批
- 输入数据：纠正原因/目标日期/目标记录 id（按接口字段）
- 执行步骤：
  1. `POST /api/Attendance/correction`
  2. `POST /api/Attendance/correction/{id}/approve`
- 预期结果：纠正进入 Pending→Approved；记录回读一致
- 通过准则：状态流转正确；审批后数据可回读
- 设计技术：状态转换

### TC-ATT-008 班次列表
- 前置条件：以 demo_user 或 demo_hr 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Attendance/shifts`
- 预期结果：200 + 班次列表（含 start/end、flex 等）
- 通过准则：列表非空；字段类型正确
- 设计技术：等价类

---

## 7. 假期
### TC-LEAVE-001 假期类型列表
- 前置条件：以任意已登录用户（employee/hr）
- 输入数据：无
- 执行步骤：
  1. `GET /api/Leave/types`
- 预期结果：200 + 类型列表
- 通过准则：列表非空；包含年假/病假等
- 设计技术：等价类

### TC-LEAVE-002 查询我的假期余额
- 前置条件：以 demo_user 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Leave/balances/my`
- 预期结果：200 + 余额项
- 通过准则：余额项非空；remaining/used/pending 字段一致性
- 设计技术：等价类

### TC-LEAVE-003 HR 查询指定员工余额
- 前置条件：以 demo_hr 登录；已取得 employeeId
- 输入数据：employeeId
- 执行步骤：
  1. `GET /api/Leave/balances/{employeeId}`
- 预期结果：200 + 指定员工余额
- 通过准则：返回 employeeId 匹配；结构一致
- 设计技术：等价类

### TC-LEAVE-004A 提交请假（正交实验组合 #1）
- 前置条件：以 demo_user 登录
- 输入数据：leaveType=年假；start/end 同日；半天；无附件；不跨月
- 执行步骤：
  1. `POST /api/Leave/requests`
- 预期结果：201/200，状态 Pending
- 通过准则：返回 id；totalDays 计算正确
- 设计技术：正交实验（多因子组合抽样）

### TC-LEAVE-004B 提交请假（正交实验组合 #2）
- 前置条件：以 demo_user 登录
- 输入数据：leaveType=病假；start=end；半天；有附件；不跨月
- 执行步骤：
  1. `POST /api/Leave/requests`（附件字段按接口）
- 预期结果：成功或明确校验错误（若附件必填）
- 通过准则：行为与规则一致（requiresAttachment 规则）
- 设计技术：正交实验

### TC-LEAVE-004C 提交请假（正交实验组合 #3：跨月）
- 前置条件：以 demo_user 登录
- 输入数据：leaveType=年假；start/end 跨月；全日；有/无附件
- 执行步骤：
  1. `POST /api/Leave/requests`
- 预期结果：成功，days 计算正确
- 通过准则：days 与日期跨度一致；状态 Pending
- 设计技术：边界值（跨月）

### TC-LEAVE-004D 提交请假（边界：最大天数）
- 前置条件：以 demo_user 登录
- 输入数据：leaveType=年假；天数=规则上限（如 MaxDaysPerRequest）
- 执行步骤：
  1. `POST /api/Leave/requests`
- 预期结果：成功或 400（若超过上限）
- 通过准则：与规则一致且错误提示清晰
- 设计技术：边界值

### TC-LEAVE-005 请假列表/详情
- 前置条件：已创建至少 1 条请假申请
- 输入数据：无
- 执行步骤：
  1. `GET /api/Leave/requests/my`
  2. `GET /api/Leave/requests/{id}`
- 预期结果：列表包含新申请；详情字段可回读
- 通过准则：id 一致；状态与创建一致
- 设计技术：状态迁移

### TC-LEAVE-006 审批/驳回/撤销状态机
- 前置条件：demo_user 创建 Pending；demo_hr 可审批
- 输入数据：requestId
- 执行步骤：
  1. HR：`POST /api/Leave/requests/{id}/approve`
  2. 新建另一条 Pending：HR `reject`；再新建一条 Pending：员工 `cancel`
  3. 分别 `GET /api/Leave/requests/{id}` 回读
- 预期结果：状态按 Pending→Approved/Rejected/Cancelled 变化；余额 pending/used 同步
- 通过准则：状态一致；余额一致性不破坏
- 设计技术：状态转换 + 决策表（状态×操作）

### TC-LEAVE-007 非法日期范围校验
- 前置条件：以 demo_user 登录
- 输入数据：endDate < startDate
- 执行步骤：
  1. `POST /api/Leave/requests`
- 预期结果：400，包含字段级校验信息
- 通过准则：不返回 5xx；错误结构可解析
- 设计技术：因果图（非法日期→400）

---

## 8. 报销
### TC-EXP-001 报销类型列表与详情
- 前置条件：已登录（employee）
- 输入数据：无
- 执行步骤：
  1. `GET /api/Expenses/types`
  2. 取任意 typeId，`GET /api/Expenses/types/{id}`
- 预期结果：200；字段包含 maxAmount/requiresReceipt 等
- 通过准则：列表非空；详情回读一致
- 设计技术：等价类

### TC-EXP-002 提交报销申请（金额边界）
- 前置条件：以 demo_user 登录；已获取 typeId（有 maxAmount 的类型）
- 输入数据：amount=上限；含/不含 receiptUrl（按 requiresReceipt）
- 执行步骤：
  1. `POST /api/Expenses/requests`
- 预期结果：创建成功；状态为 Draft/Pending（按实现）
- 通过准则：金额=上限时通过；超过上限返回 400（若实现校验）
- 设计技术：边界值

### TC-EXP-003 报销列表查询
- 前置条件：已存在报销数据
- 输入数据：page/pageSize
- 执行步骤：
  1. `GET /api/Expenses/requests?page=1&pageSize=20`
- 预期结果：200 + 分页结构
- 通过准则：结构稳定；items 可解析
- 设计技术：等价类

### TC-EXP-004 报销审批
- 前置条件：demo_user 创建一条待审批；demo_hr 登录
- 输入数据：requestId
- 执行步骤：
  1. HR：`POST /api/Expenses/requests/{id}/approve`
  2. 回读列表，确认状态变化
- 预期结果：状态变为 Approved（或等价）
- 通过准则：状态流转一致；无 5xx
- 设计技术：状态转换

---

## 9. 薪酬
### TC-PAY-001 周期列表
- 前置条件：以 demo_hr 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Payroll/periods`
- 预期结果：200 + 周期列表（至少近 12 个月）
- 通过准则：列表非空；year/month 字段正确
- 设计技术：等价类

### TC-PAY-002 薪资项列表
- 前置条件：以 demo_hr 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Payroll/items`
- 预期结果：200 + items
- 通过准则：items 非空；包含 BASE 等
- 设计技术：等价类

### TC-PAY-003 员工查询我的薪资单
- 前置条件：以 demo_user 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Payroll/records/my`
- 预期结果：200 + 列表
- 通过准则：列表可解析；字段不泄露他人信息
- 设计技术：决策表（角色×数据范围）

### TC-PAY-004 HR 查询薪资记录列表与详情
- 前置条件：以 demo_hr 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Payroll/records?page=1&pageSize=20`
  2. 取任意 recordId，`GET /api/Payroll/records/{id}`
- 预期结果：200；详情含明细 items
- 通过准则：详情结构稳定；总额字段一致性合理（非负/可解释）
- 设计技术：等价类

### TC-PAY-005 HR 查询指定员工薪资记录
- 前置条件：以 demo_hr 登录；已取得 employeeId
- 输入数据：employeeId
- 执行步骤：
  1. `GET /api/Payroll/records/employee/{employeeId}`
- 预期结果：200 + 记录列表
- 通过准则：employeeId 匹配；结构稳定
- 设计技术：等价类

### TC-PAY-006 周期计算/审批/锁定（接口可达）
- 前置条件：以 demo_hr 或 demo_admin 登录；已取得 periodId
- 输入数据：periodId
- 执行步骤：
  1. `POST /api/Payroll/periods/{periodId}/calculate`
  2. `POST /api/Payroll/periods/{periodId}/approve`
  3. `POST /api/Payroll/periods/{periodId}/lock`
- 预期结果：返回 200/204；周期状态随操作变化（按实现）
- 通过准则：接口可达；无 5xx；重复调用行为可解释（幂等/提示）
- 设计技术：状态转换

---

## 10. 排班
### TC-SCH-001 查询排班与统计
- 前置条件：以 demo_hr 登录
- 输入数据：可选范围参数
- 执行步骤：
  1. `GET /api/Schedules?page=1&pageSize=20`
  2. `GET /api/Schedules/stats`
- 预期结果：200；结构稳定
- 通过准则：无越权；数据可解析
- 设计技术：等价类

### TC-SCH-002 获取可排班员工与班次模板
- 前置条件：以 demo_hr 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Schedules/schedulable-employees`
  2. `GET /api/Schedules/shift-templates`
- 预期结果：200；列表非空
- 通过准则：字段类型正确；模板可用于分配
- 设计技术：等价类

### TC-SCH-003 分配排班写路径与回读
- 前置条件：以 demo_hr 登录；获取 employeeId/shiftId/date
- 输入数据：employeeId + shiftId + date
- 执行步骤：
  1. `POST /api/Schedules/assign`
  2. `GET /api/Schedules`（按日期过滤若支持）回读
- 预期结果：分配成功；回读能看到排班变更
- 通过准则：写后可读；无 5xx
- 设计技术：状态迁移

---

## 11. 公司基础资料
### TC-COMP-001 成本中心树/CRUD
- 前置条件：以 demo_admin 登录
- 输入数据：新成本中心 name/code；parentId（可选）
- 执行步骤：
  1. `GET /api/Company/cost-centers/tree`
  2. `POST /api/Company/cost-centers`（若存在）
  3. `GET /api/Company/cost-centers/{id}`
- 预期结果：读写可用；树结构更新
- 通过准则：创建后可回读；字段一致
- 设计技术：等价类 + 边界值（层级）

### TC-COMP-002 标签 CRUD
- 前置条件：以 demo_admin 登录
- 输入数据：name/code/color（按接口）
- 执行步骤：
  1. `GET /api/Company/tags`
  2. `POST /api/Company/tags`
  3. `GET /api/Company/tags/{id}`
- 预期结果：读写可用
- 通过准则：创建后可回读；列表包含新标签
- 设计技术：等价类

### TC-COMP-003 打卡设备 CRUD
- 前置条件：以 demo_admin 登录
- 输入数据：deviceName/serial/location（按接口）
- 执行步骤：
  1. `GET /api/Company/clock-devices`
  2. `POST /api/Company/clock-devices`
  3. `GET /api/Company/clock-devices/{id}`
- 预期结果：读写可用
- 通过准则：创建后可回读
- 设计技术：等价类

### TC-COMP-004 获取当前租户信息
- 前置条件：以任意已登录用户
- 输入数据：token
- 执行步骤：
  1. `GET /api/Company/tenant`
- 预期结果：200 + tenant 信息
- 通过准则：tenantCode/name 等可回读且与演示租户一致
- 设计技术：等价类

---

## 12. 系统设置与审计
### TC-SET-001 权限清单
- 前置条件：以 demo_admin 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Settings/permissions`
- 预期结果：200 + 权限列表
- 通过准则：列表非空；包含 payroll/employee 等权限点
- 设计技术：等价类

### TC-SET-002 角色列表与详情
- 前置条件：以 demo_admin 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Settings/roles`
  2. 取 roleId：`GET /api/Settings/roles/{id}`
- 预期结果：200
- 通过准则：角色字段与权限点可回读
- 设计技术：等价类

### TC-SET-003 角色权限查询/更新
- 前置条件：以 demo_admin 登录；已取得 roleId
- 输入数据：权限集（最小集/全量集）
- 执行步骤：
  1. `GET /api/Settings/roles/{id}/permissions`
  2. `PUT /api/Settings/roles/{id}/permissions`（若存在）
  3. 再次 GET 回读
- 预期结果：权限更新生效
- 通过准则：回读权限集与更新一致；RBAC 行为随之变化（抽样验证）
- 设计技术：决策表（角色×权限×行为）

### TC-SET-004 用户管理（启用/禁用/重置密码）
- 前置条件：以 demo_admin 登录；已取得 userId
- 输入数据：userId
- 执行步骤：
  1. `GET /api/Settings/users`
  2. `POST /api/Settings/users/{id}/disable`，再 `enable`
  3. `POST /api/Settings/users/{id}/reset-password`
- 预期结果：状态变更可回读；重置密码返回明确结果
- 通过准则：禁用后登录失败；启用后恢复（抽样）
- 设计技术：状态转换

### TC-SET-005 系统配置查询
- 前置条件：以 demo_admin 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Settings/system-configs`
- 预期结果：200 + 配置项
- 通过准则：结构稳定；字段类型正确
- 设计技术：等价类

### TC-AUD-001 审计日志查询与导出
- 前置条件：以 demo_admin 登录
- 输入数据：过滤条件（可选）
- 执行步骤：
  1. `GET /api/Settings/audit-logs?page=1&pageSize=20`
  2. `GET /api/Settings/audit-logs/export`（若为文件流，验证 Content-Type/下载）
- 预期结果：查询返回分页；导出返回文件或下载链接
- 通过准则：无 5xx；导出内容可下载/可解析
- 设计技术：等价类

### TC-APPR-001 审批流配置 CRUD（可达性）
- 前置条件：以 demo_admin 登录
- 输入数据：最小审批流配置（按接口）
- 执行步骤：
  1. `GET /api/Settings/approval-flows`
  2. `POST /api/Settings/approval-flows`（若存在）
  3. `GET /api/Settings/approval-flows/{id}`
- 预期结果：接口可达；结构稳定
- 通过准则：创建后可回读（如支持）
- 设计技术：等价类

---

## 13. AI 助手
### TC-AI-001 对话（Mock）
- 前置条件：以任意已登录用户
- 输入数据：message="查询我的假期余额"
- 执行步骤：
  1. `POST /api/Agent/chat`
- 预期结果：200 + assistant 回复非空
- 通过准则：回复字段存在且可展示；无 5xx
- 设计技术：等价类

### TC-AI-002 历史查询与清理
- 前置条件：先执行 TC-AI-001 产生历史
- 输入数据：无
- 执行步骤：
  1. `GET /api/Agent/history`
  2. `DELETE /api/Agent/history`
  3. 再次 `GET /api/Agent/history`
- 预期结果：清理前 historyCount>0；清理后为空
- 通过准则：行为可重复验证
- 设计技术：状态转换

---

## 14. 保险与个税（可达性/契约一致性）
### TC-INS-001 保险接口可达
- 前置条件：以 demo_hr 或 demo_admin 登录
- 输入数据：无/任意 id（从列表获取）
- 执行步骤：
  1. `GET /api/Insurance/plans`
  2. `GET /api/Insurance/benefits-simple`
  3. `GET /api/Insurance/stats`
- 预期结果：200；结构可解析
- 通过准则：接口可达且无 5xx
- 设计技术：等价类

### TC-TAX-001 个税接口可达
- 前置条件：以 demo_hr 或 demo_admin 登录
- 输入数据：无
- 执行步骤：
  1. `GET /api/Tax/profiles`
  2. `GET /api/Tax/records?page=1&pageSize=20`
  3. `GET /api/Tax/settings`
- 预期结果：200；结构可解析
- 通过准则：接口可达且无 5xx
- 设计技术：等价类

---

## 15. 非功能（可靠性/性能/可移植性）
### TC-NFR-001 全局异常处理（不泄露敏感信息）
- 前置条件：无
- 输入数据：构造明显无效的资源 id（如 Guid.Empty）
- 执行步骤：
  1. `GET /api/Employees/00000000-0000-0000-0000-000000000000`
- 预期结果：404 或 400；错误结构稳定；不返回内部堆栈
- 通过准则：无 500；响应体不包含敏感堆栈/连接串等
- 设计技术：等价类（不存在资源）

### TC-NFR-002 输入校验（非法输入返回 400）
- 前置条件：以 demo_user 登录
- 输入数据：例如金额为负数、日期为空、必填缺失
- 执行步骤：
  1. `POST /api/Expenses/requests` 传入 amount=-1
- 预期结果：400 + 字段级错误
- 通过准则：返回 400；错误信息可定位字段
- 设计技术：因果图

### TC-PERF-001 性能基准抽样（本地回归基线）
- 前置条件：服务已启动；数据已准备
- 输入数据：固定接口与固定参数
- 执行步骤：
  1. 对以下接口各执行 30 次采样并计算 p50/p95：
     - `GET /api/Organizations/tree`
     - `GET /api/Employees?page=1&pageSize=20`
     - `GET /api/Leave/balances/my`
- 预期结果：错误率 0%；p95 在本地环境可接受（作为基线，不作为容量承诺）
- 通过准则：输出 p50/p95 与错误率；后续回归可对比
- 设计技术：抽样统计

### TC-ENV-001 独立测试环境可运行
- 前置条件：无
- 输入数据：SQLite connection string + Redis disabled
- 执行步骤：
  1. 启动后端并通过 `/health` 与 `/openapi/v1.json` 验证
- 预期结果：服务可启动；接口可访问
- 通过准则：健康检查通过；OpenAPI 可访问
- 设计技术：可移植性验证
