# Hrevolve 黑盒测试总结报告
日期：2026-02-07  
基线：当前工作区（d:\HomeCode\Hrevolve）  
执行方式：API 黑盒自动化 + 关键链路抽样验证  
参考：测试计划见 [BlackBoxTestPlan_2026-02-07.md](file:///d:/HomeCode/Hrevolve/TestReports/BlackBoxTestPlan_2026-02-07.md)

## 1. 测试范围与覆盖
- 范围口径：以运行时 OpenAPI 暴露的端点为主（L1），对 SRS 中未实现项仅做风险提示（L2）。
- 覆盖矩阵：[BlackBoxCoverageMatrix_2026-02-07.md](file:///d:/HomeCode/Hrevolve/TestReports/BlackBoxCoverageMatrix_2026-02-07.md)
- 覆盖率（按矩阵口径）：
  - 可测试需求条目：63（排除 NA 4 条）
  - 已执行：63（PASS/FAIL 计入覆盖）
  - 覆盖率：100%（≥90% 达标）

## 2. 测试环境与数据
- 后端：.NET（Development 环境启用 OpenAPI）
- 独立依赖：
  - DB：SQLite（`Data Source=hrevolve_test.sqlite`）
  - Cache：Redis disabled（使用内存缓存）
- 租户/账号：demo 租户；demo_user / demo_hr / demo_admin（见测试用例集）
- 自动化脚本：
  - 执行入口：[run-blackbox.ps1](file:///d:/HomeCode/Hrevolve/tests/blackbox/run-blackbox.ps1)
  - 原始结果：`TestReports/BlackBoxExecution_2026-02-07.*`

## 3. 执行统计
来源：[BlackBoxExecution_2026-02-07.md](file:///d:/HomeCode/Hrevolve/TestReports/BlackBoxExecution_2026-02-07.md)
- 总用例：64
- 通过：61
- 失败：3
- 阻断：0（以 FAIL 记录功能缺口/契约异常）

## 4. 缺陷分布
已提交缺陷（3）：
- S2/P0：2
  - [BB-20260207-001.md](file:///d:/HomeCode/Hrevolve/TestReports/Defects/BB-20260207-001.md)（组织员工查询接口 400，占位未实现）
  - [BB-20260207-003.md](file:///d:/HomeCode/Hrevolve/TestReports/Defects/BB-20260207-003.md)（报销允许负金额，缺少输入校验）
- S3/P1：1
  - [BB-20260207-002.md](file:///d:/HomeCode/Hrevolve/TestReports/Defects/BB-20260207-002.md)（部门考勤统计携带 date query 返回 404）

## 5. 性能指标（本地基线）
抽样接口（每接口 30 次）：
- OrganizationsTree：p50=18ms，p95=26ms
- EmployeesList：p50=16ms，p95=25ms
- LeaveBalancesMy：p50=15ms，p95=26ms

说明：该指标为本地环境回归基线，不等同于生产容量承诺。

## 6. 遗留风险（ISO 25010 视角）
- 功能适合性：组织单元员工查询端点对外暴露但返回 400，占位未实现（高风险）
- 安全性/数据完整性：报销负金额未校验，可能导致资损与账务异常（高风险）
- 可靠性/兼容性：部门考勤统计接口在携带 query 参数时出现 404，契约不稳定（中风险）
- 多租户：当前测试环境仅含 demo 单租户数据，跨租户隔离无法进行实证验证（风险保留，建议补充多租户测试数据与用例）

## 7. 改进建议
- 为所有写接口补齐输入校验（金额、日期范围、必填字段），统一返回 400/422 + 字段级错误结构。
- 清理“对外暴露但未实现”的占位端点：未实现则返回 501 或从 OpenAPI 移除/隐藏，避免联调阻断。
- 修复部门考勤统计接口 query 参数触发 404 的契约问题，并补充回归用例。
- 建议将 `tests/blackbox` 作为 CI 回归入口（至少覆盖 P0/P1），并建立稳定的可复位演示数据策略（seed/rollback）。
