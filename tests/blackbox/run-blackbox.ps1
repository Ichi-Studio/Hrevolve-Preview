param(
  [string]$BaseUrl = "https://localhost:5225",
  [string]$TenantCode = "demo",
  [string]$OutDir = "d:\HomeCode\Hrevolve\TestReports"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function New-CaseResult {
  param(
    [string]$Id,
    [string]$Name
  )

  [pscustomobject]@{
    id = $Id
    name = $Name
    startedAtUtc = [DateTime]::UtcNow.ToString("o")
    durationMs = $null
    expected = $null
    actual = $null
    pass = $false
    notes = @()
  }
}

function Add-Note([object]$r, [string]$note) {
  $r.notes += $note
}

function Invoke-Api {
  param(
    [ValidateSet("GET","POST","PUT","DELETE")]
    [string]$Method,
    [string]$Path,
    [string]$Token = $null,
    [object]$Body = $null,
    [hashtable]$Headers = $null
  )

  $uri = ($BaseUrl.TrimEnd("/") + $Path)
  $allHeaders = @{}
  if ($Headers) {
    foreach ($k in $Headers.Keys) { $allHeaders[$k] = $Headers[$k] }
  }
  if ($Token) {
    $allHeaders["Authorization"] = "Bearer $Token"
  }

  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  try {
    $params = @{
      Method = $Method
      Uri = $uri
      Headers = $allHeaders
      SkipCertificateCheck = $true
    }
    if ($Body -ne $null) {
      $params["ContentType"] = "application/json"
      $params["Body"] = ($Body | ConvertTo-Json -Depth 20)
    }

    $resp = Invoke-WebRequest @params
    $sw.Stop()

    $content = $null
    $rawText = $null
    if ($null -ne $resp.Content) {
      if ($resp.Content -is [byte[]]) {
        $rawText = [Text.Encoding]::UTF8.GetString($resp.Content)
      } else {
        $rawText = [string]$resp.Content
      }
    }

    if ($rawText) {
      try { $content = $rawText | ConvertFrom-Json -Depth 50 } catch { $content = $rawText }
    }

    return [pscustomobject]@{
      ok = $true
      status = [int]$resp.StatusCode
      durationMs = [int]$sw.ElapsedMilliseconds
      content = $content
      raw = $rawText
      headers = $resp.Headers
    }
  } catch {
    $sw.Stop()

    $status = $null
    $raw = $null
    $headersOut = $null
    try {
      $ex = $_.Exception
      if ($ex -and $ex.Response) {
        $respObj = $ex.Response
        if ($respObj -is [System.Net.Http.HttpResponseMessage]) {
          $status = [int]$respObj.StatusCode
          $headersOut = $respObj.Headers
          if ($respObj.Content) {
            $raw = $respObj.Content.ReadAsStringAsync().GetAwaiter().GetResult()
          }
        } else {
          $status = [int]$respObj.StatusCode.value__
          $headersOut = $respObj.Headers
          $sr = New-Object System.IO.StreamReader($respObj.GetResponseStream())
          $raw = $sr.ReadToEnd()
        }
      }
    } catch {
    }

    $content = $null
    if ($raw) {
      try { $content = $raw | ConvertFrom-Json -Depth 50 } catch { $content = $raw }
    }

    return [pscustomobject]@{
      ok = $false
      status = $status
      durationMs = [int]$sw.ElapsedMilliseconds
      content = $content
      raw = $raw
      headers = $headersOut
      error = $_.ToString()
    }
  }
}

function Assert-True([bool]$cond, [string]$message) {
  if (-not $cond) { throw $message }
}

function Assert-Has([object]$obj, [string]$prop) {
  Assert-True ($null -ne $obj) "响应体为空，无法检查字段 $prop"
  $p = $obj.PSObject.Properties.Name -contains $prop
  Assert-True $p "缺少字段：$prop"
}

function Login {
  param([string]$Username, [string]$Password)
  $body = @{
    username = $Username
    password = $Password
    tenant = $TenantCode
  }
  $r = Invoke-Api -Method POST -Path "/api/Auth/login" -Body $body
  if ($r.status -ne 200) { throw "登录失败：$Username，status=$($r.status)，raw=$($r.raw)" }
  Assert-Has $r.content "accessToken"
  Assert-Has $r.content "refreshToken"
  return $r.content
}

function Percentile([double[]]$values, [double]$p) {
  if (-not $values -or $values.Count -eq 0) { return $null }
  $sorted = $values | Sort-Object
  $n = $sorted.Count
  $idx = [math]::Ceiling(($p / 100.0) * $n) - 1
  $idx = [math]::Max(0, [math]::Min($idx, $n - 1))
  return [double]$sorted[$idx]
}

$runId = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
$results = New-Object System.Collections.Generic.List[object]
$perf = New-Object System.Collections.Generic.List[object]

try {
  $envCase = New-CaseResult -Id "TC-ENV-001" -Name "独立测试环境可运行"
  $health = Invoke-Api -Method GET -Path "/health"
  $envCase.durationMs = $health.durationMs
  $envCase.expected = "200 + Healthy"
  $envCase.actual = "status=$($health.status)"
  $envCase.pass = ($health.status -eq 200 -and $health.raw -match "Healthy")
  $results.Add($envCase)

  $case = New-CaseResult -Id "TC-AUTH-001" -Name "正确账号密码登录（员工）"
  $loginEmp = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{ username = "demo_user"; password = "demo123"; tenant = $TenantCode }
  $case.durationMs = $loginEmp.durationMs
  $case.expected = "200 + accessToken/refreshToken"
  $case.actual = "status=$($loginEmp.status)"
  $tokensEmp = $null
  try {
    Assert-True ($loginEmp.status -eq 200) "期望 200"
    Assert-Has $loginEmp.content "accessToken"
    Assert-Has $loginEmp.content "refreshToken"
    $tokensEmp = $loginEmp.content
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AUTH-002" -Name "错误凭据登录"
  $badLogin = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{ username = "demo_user"; password = "wrong"; tenant = $TenantCode }
  $case.durationMs = $badLogin.durationMs
  $case.expected = "400"
  $case.actual = "status=$($badLogin.status)"
  $case.pass = ($badLogin.status -eq 400)
  $results.Add($case)

  $tokensHr = Login -Username "demo_hr" -Password "demo123"
  $tokensAdmin = Login -Username "demo_admin" -Password "demo123"

  $empToken = $tokensEmp.accessToken
  $hrToken = $tokensHr.accessToken
  $adminToken = $tokensAdmin.accessToken

  $case = New-CaseResult -Id "TC-AUTH-004" -Name "Refresh Token 轮换"
  $refresh = Invoke-Api -Method POST -Path "/api/Auth/refresh" -Body @{ refreshToken = $tokensEmp.refreshToken }
  $case.durationMs = $refresh.durationMs
  $case.expected = "200 + new accessToken/refreshToken"
  $case.actual = "status=$($refresh.status)"
  try {
    Assert-True ($refresh.status -eq 200) "期望 200"
    Assert-Has $refresh.content "accessToken"
    Assert-Has $refresh.content "refreshToken"
    $me2 = Invoke-Api -Method GET -Path "/api/Auth/me" -Token $refresh.content.accessToken
    Assert-True ($me2.status -eq 200) "新 token 无法访问 /me"
    $empToken = $refresh.content.accessToken
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AUTH-003" -Name "获取当前用户信息"
  $me = Invoke-Api -Method GET -Path "/api/Auth/me" -Token $empToken
  $case.durationMs = $me.durationMs
  $case.expected = "200 + tenantId/roles/permissions"
  $case.actual = "status=$($me.status)"
  try {
    Assert-True ($me.status -eq 200) "期望 200"
    Assert-Has $me.content "tenantId"
    Assert-Has $me.content "roles"
    Assert-Has $me.content "permissions"
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SEC-001" -Name "未登录访问受保护资源"
  $anon = Invoke-Api -Method GET -Path "/api/Employees"
  $case.durationMs = $anon.durationMs
  $case.expected = "401"
  $case.actual = "status=$($anon.status)"
  $case.pass = ($anon.status -eq 401)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SEC-002" -Name "越权访问（员工访问员工列表）"
  $forbidden = Invoke-Api -Method GET -Path "/api/Employees?page=1&pageSize=10" -Token $empToken
  $case.durationMs = $forbidden.durationMs
  $case.expected = "403"
  $case.actual = "status=$($forbidden.status)"
  $case.pass = ($forbidden.status -eq 403)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-I18N-001" -Name "获取 locales"
  $locales = Invoke-Api -Method GET -Path "/api/Localization/locales"
  $case.durationMs = $locales.durationMs
  $case.expected = "200 + locales"
  $case.actual = "status=$($locales.status)"
  $case.pass = ($locales.status -eq 200 -and $locales.raw -match "zh-CN")
  $results.Add($case)

  $case = New-CaseResult -Id "TC-I18N-002" -Name "获取 zh-CN 消息包"
  $msgs = Invoke-Api -Method GET -Path "/api/Localization/messages/zh-CN"
  $case.durationMs = $msgs.durationMs
  $case.expected = "200 + key/value"
  $case.actual = "status=$($msgs.status)"
  $case.pass = ($msgs.status -eq 200 -and $msgs.raw.Length -gt 10)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ORG-001" -Name "获取组织树"
  $tree = Invoke-Api -Method GET -Path "/api/Organizations/tree" -Token $hrToken
  $case.durationMs = $tree.durationMs
  $case.expected = "200 + tree"
  $case.actual = "status=$($tree.status)"
  $deptId = $null
  $orgId = $null
  try {
    Assert-True ($tree.status -eq 200) "期望 200"
    $orgId = $tree.content.id
    if ($tree.content.children -and $tree.content.children.Count -gt 0) {
      $deptId = $tree.content.children[0].id
    }
    Assert-True ($null -ne $orgId) "无法从组织树提取 id"
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  if (-not $deptId) { $deptId = $orgId }

  $case = New-CaseResult -Id "TC-ORG-002" -Name "获取组织单元详情"
  $orgDetail = Invoke-Api -Method GET -Path "/api/Organizations/$deptId" -Token $hrToken
  $case.durationMs = $orgDetail.durationMs
  $case.expected = "200 + detail"
  $case.actual = "status=$($orgDetail.status)"
  $case.pass = ($orgDetail.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ORG-003" -Name "查询组织下员工/岗位"
  $orgEmp = Invoke-Api -Method GET -Path "/api/Organizations/$deptId/employees" -Token $hrToken
  $orgPos = Invoke-Api -Method GET -Path "/api/Organizations/$deptId/positions" -Token $hrToken
  $case.durationMs = ($orgEmp.durationMs + $orgPos.durationMs)
  $case.expected = "200 + list"
  $case.actual = "emp=$($orgEmp.status),pos=$($orgPos.status)"
  $case.pass = ($orgEmp.status -eq 200 -and $orgPos.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ORG-004" -Name "岗位列表查询"
  $positions = Invoke-Api -Method GET -Path "/api/Organizations/positions" -Token $hrToken
  $case.durationMs = $positions.durationMs
  $case.expected = "200 + list"
  $case.actual = "status=$($positions.status)"
  $case.pass = ($positions.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EMP-001" -Name "员工列表分页"
  $empList = Invoke-Api -Method GET -Path "/api/Employees?page=1&pageSize=20" -Token $hrToken
  $case.durationMs = $empList.durationMs
  $case.expected = "200 + paging"
  $case.actual = "status=$($empList.status)"
  $employeeId = $null
  $shiftId = $null
  try {
    Assert-True ($empList.status -eq 200) "期望 200"
    if ($empList.content.items -and $empList.content.items.Count -gt 0) {
      $employeeId = $empList.content.items[0].id
    }
    Assert-True ($null -ne $employeeId) "无法从员工列表提取 employeeId"
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EMP-002" -Name "员工详情"
  $empDetail = Invoke-Api -Method GET -Path "/api/Employees/$employeeId" -Token $hrToken
  $case.durationMs = $empDetail.durationMs
  $case.expected = "200 + detail"
  $case.actual = "status=$($empDetail.status)"
  $case.pass = ($empDetail.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EMP-003" -Name "历史时点查询"
  $atDate = Invoke-Api -Method GET -Path "/api/Employees/$employeeId/at-date?date=2024-01-01" -Token $hrToken
  $case.durationMs = $atDate.durationMs
  $case.expected = "200"
  $case.actual = "status=$($atDate.status)"
  $case.pass = ($atDate.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EMP-004" -Name "任职历史查询"
  $jobHist = Invoke-Api -Method GET -Path "/api/Employees/$employeeId/job-history" -Token $hrToken
  $case.durationMs = $jobHist.durationMs
  $case.expected = "200 + list"
  $case.actual = "status=$($jobHist.status)"
  $case.pass = ($jobHist.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-NFR-001" -Name "全局异常处理（不存在资源）"
  $missing = Invoke-Api -Method GET -Path "/api/Employees/00000000-0000-0000-0000-000000000000" -Token $hrToken
  $case.durationMs = $missing.durationMs
  $case.expected = "404/400 且不泄露敏感堆栈"
  $case.actual = "status=$($missing.status)"
  $case.pass = ($missing.status -in 400,404 -and ($missing.raw -notmatch "System\\." ) -and ($missing.raw -notmatch "Exception"))
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-008" -Name "班次列表"
  $shifts = Invoke-Api -Method GET -Path "/api/Attendance/shifts" -Token $empToken
  $case.durationMs = $shifts.durationMs
  $case.expected = "200 + list"
  $case.actual = "status=$($shifts.status)"
  $case.pass = ($shifts.status -eq 200)
  if ($shifts.status -eq 200 -and $shifts.content -is [array] -and $shifts.content.Count -gt 0) {
    $shiftId = $shifts.content[0].id
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-001" -Name "签到"
  $checkIn = Invoke-Api -Method POST -Path "/api/Attendance/check-in" -Token $empToken -Body @{}
  $today1 = Invoke-Api -Method GET -Path "/api/Attendance/today" -Token $empToken
  $case.durationMs = ($checkIn.durationMs + $today1.durationMs)
  $case.expected = "200 + today.checkInTime"
  $case.actual = "check-in=$($checkIn.status),today=$($today1.status)"
  $case.pass = ($checkIn.status -eq 200 -and $today1.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-002" -Name "签退"
  $checkOut = Invoke-Api -Method POST -Path "/api/Attendance/check-out" -Token $empToken -Body @{}
  $today2 = Invoke-Api -Method GET -Path "/api/Attendance/today" -Token $empToken
  $case.durationMs = ($checkOut.durationMs + $today2.durationMs)
  $case.expected = "200 + today.checkOutTime"
  $case.actual = "check-out=$($checkOut.status),today=$($today2.status)"
  $case.pass = ($checkOut.status -eq 200 -and $today2.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-003" -Name "查询我的考勤记录"
  $myRecords = Invoke-Api -Method GET -Path "/api/Attendance/records/my?page=1&pageSize=20" -Token $empToken
  $case.durationMs = $myRecords.durationMs
  $case.expected = "200 + paging"
  $case.actual = "status=$($myRecords.status)"
  $case.pass = ($myRecords.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-004" -Name "查询今日考勤摘要"
  $today = Invoke-Api -Method GET -Path "/api/Attendance/today" -Token $empToken
  $case.durationMs = $today.durationMs
  $case.expected = "200"
  $case.actual = "status=$($today.status)"
  $case.pass = ($today.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-005" -Name "月度统计"
  $monthly = Invoke-Api -Method GET -Path "/api/Attendance/stats/monthly?year=2026&month=2" -Token $empToken
  $case.durationMs = $monthly.durationMs
  $case.expected = "200 + stats"
  $case.actual = "status=$($monthly.status)"
  $case.pass = ($monthly.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-006" -Name "HR 查询全量记录与部门统计"
  $attAll = Invoke-Api -Method GET -Path "/api/Attendance/records?page=1&pageSize=10" -Token $hrToken
  $dateStr = (Get-Date).ToString("yyyy-MM-dd")
  $deptStat = Invoke-Api -Method GET -Path "/api/Attendance/statistics/department/$deptId?date=$dateStr" -Token $hrToken
  $case.durationMs = $attAll.durationMs + $deptStat.durationMs
  $case.expected = "200"
  $case.actual = "records=$($attAll.status),dept=$($deptStat.status)"
  $case.pass = ($attAll.status -eq 200 -and $deptStat.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-ATT-007" -Name "考勤纠正申请与审批（可达性）"
  $corr = Invoke-Api -Method POST -Path "/api/Attendance/correction" -Token $empToken -Body @{
    date = $dateStr
    checkInTime = $null
    checkOutTime = $null
    reason = "自动化补卡申请"
  }
  $corrApprove = Invoke-Api -Method POST -Path "/api/Attendance/correction/$([Guid]::NewGuid())/approve" -Token $adminToken -Body @{}
  $case.durationMs = $corr.durationMs + $corrApprove.durationMs
  $case.expected = "200"
  $case.actual = "apply=$($corr.status),approve=$($corrApprove.status)"
  $case.pass = ($corr.status -eq 200 -and $corrApprove.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-LEAVE-001" -Name "假期类型列表"
  $leaveTypes = Invoke-Api -Method GET -Path "/api/Leave/types" -Token $empToken
  $case.durationMs = $leaveTypes.durationMs
  $case.expected = "200 + list"
  $case.actual = "status=$($leaveTypes.status)"
  $annualTypeId = $null
  try {
    Assert-True ($leaveTypes.status -eq 200) "期望 200"
    foreach ($t in $leaveTypes.content) {
      $code = ($t.code ?? $t.Code)
      if ($code -eq "ANNUAL") { $annualTypeId = $t.id }
    }
    Assert-True ($null -ne $annualTypeId) "未找到 ANNUAL 假期类型"
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-LEAVE-002" -Name "查询我的假期余额"
  $balances = Invoke-Api -Method GET -Path "/api/Leave/balances/my" -Token $empToken
  $case.durationMs = $balances.durationMs
  $case.expected = "200 + balances"
  $case.actual = "status=$($balances.status)"
  $case.pass = ($balances.status -eq 200)
  $results.Add($case)

  $leaveTypeIdForRequest = $annualTypeId
  if ($balances.status -eq 200 -and $balances.content) {
    $candidate = @($balances.content | Where-Object { $_.remainingDays -ge 2 } | Select-Object -First 1)
    if ($candidate.Count -gt 0) { $leaveTypeIdForRequest = $candidate[0].leaveTypeId }
  }

  $case = New-CaseResult -Id "TC-LEAVE-003" -Name "HR 查询指定员工余额"
  $empBalances = Invoke-Api -Method GET -Path "/api/Leave/balances/$employeeId" -Token $hrToken
  $case.durationMs = $empBalances.durationMs
  $case.expected = "200"
  $case.actual = "status=$($empBalances.status)"
  $case.pass = ($empBalances.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-LEAVE-007" -Name "非法日期范围校验"
  $badLeave = Invoke-Api -Method POST -Path "/api/Leave/requests" -Token $empToken -Body @{
    leaveTypeId = $leaveTypeIdForRequest
    startDate = "2026-02-10"
    endDate = "2026-02-09"
    startDayPart = 0
    endDayPart = 0
    reason = "invalid date range"
  }
  $case.durationMs = $badLeave.durationMs
  $case.expected = "400"
  $case.actual = "status=$($badLeave.status)"
  $case.pass = ($badLeave.status -eq 400)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-LEAVE-004C" -Name "提交请假（跨月）"
  $existingLeavesResp = Invoke-Api -Method GET -Path "/api/Leave/requests/my?page=1&pageSize=200" -Token $empToken
  $existingItems = @()
  if ($existingLeavesResp.status -eq 200 -and $existingLeavesResp.content) {
    if ($existingLeavesResp.content.items) { $existingItems = @($existingLeavesResp.content.items) }
    elseif ($existingLeavesResp.content -is [array]) { $existingItems = @($existingLeavesResp.content) }
  }

  $picked = $null
  for ($i = 1; $i -le 12; $i++) {
    $m = (Get-Date).AddMonths($i)
    $start = [DateTime]::new($m.Year, $m.Month, [DateTime]::DaysInMonth($m.Year, $m.Month))
    $end = $start.AddDays(1)

    $overlap = $false
    foreach ($it in $existingItems) {
      $s = $null
      $e = $null
      try { $s = [DateTime]::Parse(($it.startDate ?? $it.StartDate)) } catch {}
      try { $e = [DateTime]::Parse(($it.endDate ?? $it.EndDate)) } catch {}
      if ($s -and $e) {
        if (-not ($end.Date -lt $s.Date -or $start.Date -gt $e.Date)) { $overlap = $true; break }
      }
    }

    if (-not $overlap) { $picked = @{ start = $start; end = $end }; break }
  }

  $newLeave = $null
  if (-not $picked) {
    $case.durationMs = $existingLeavesResp.durationMs
    $case.expected = "201/200 + id"
    $case.actual = "BLOCK(no non-conflicting cross-month window)"
    $case.pass = $false
    Add-Note $case "BLOCK：无法找到不冲突的跨月日期窗口（未来 12 个月）"
    $results.Add($case)
    $leaveRequestId = $null
  } else {
    $newLeave = Invoke-Api -Method POST -Path "/api/Leave/requests" -Token $empToken -Body @{
      leaveTypeId = $leaveTypeIdForRequest
      startDate = $picked.start.ToString("yyyy-MM-dd")
      endDate = $picked.end.ToString("yyyy-MM-dd")
      startDayPart = 0
      endDayPart = 0
      reason = "cross-month leave"
      attachments = @()
    }
    $case.durationMs = $newLeave.durationMs
    $case.expected = "201/200 + id"
    $case.actual = "status=$($newLeave.status)"
    $leaveRequestId = $null
    try {
      Assert-True ($newLeave.status -in 200,201) "期望 200/201"
      $leaveRequestId = $newLeave.content.id
      Assert-True ($null -ne $leaveRequestId) "缺少 id"
      $case.pass = $true
    } catch {
      Add-Note $case $_.Exception.Message
      if ($newLeave.raw) { Add-Note $case ("raw=" + ($newLeave.raw -replace "`r|`n"," ")) }
    }
    $results.Add($case)
  }

  $case = New-CaseResult -Id "TC-LEAVE-005" -Name "请假列表/详情"
  if (-not $leaveRequestId) {
    $case.durationMs = 0
    $case.expected = "200 + contains id"
    $case.actual = "BLOCK(missing leaveRequestId)"
    $case.pass = $false
    Add-Note $case "BLOCK：依赖的请假创建失败，未获得 leaveRequestId"
  } else {
    $myLeaves = Invoke-Api -Method GET -Path "/api/Leave/requests/my?page=1&pageSize=20" -Token $empToken
    $leaf = Invoke-Api -Method GET -Path "/api/Leave/requests/$leaveRequestId" -Token $empToken
    $case.durationMs = ($myLeaves.durationMs + $leaf.durationMs)
    $case.expected = "200 + contains id"
    $case.actual = "list=$($myLeaves.status),detail=$($leaf.status)"
    $case.pass = ($myLeaves.status -eq 200 -and $leaf.status -eq 200)
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-LEAVE-006" -Name "请假审批"
  if (-not $leaveRequestId) {
    $case.durationMs = 0
    $case.expected = "200 + status=Approved"
    $case.actual = "BLOCK(missing leaveRequestId)"
    $case.pass = $false
    Add-Note $case "BLOCK：依赖的请假创建失败，未获得 leaveRequestId"
  } else {
    $approve = Invoke-Api -Method POST -Path "/api/Leave/requests/$leaveRequestId/approve" -Token $hrToken -Body @{ approved = $true; comment = "ok" }
    $after = Invoke-Api -Method GET -Path "/api/Leave/requests/$leaveRequestId" -Token $hrToken
    $case.durationMs = ($approve.durationMs + $after.durationMs)
    $case.expected = "200 + status=Approved"
    $case.actual = "approve=$($approve.status),after=$($after.status)"
    $case.pass = ($approve.status -eq 200 -and $after.status -eq 200)
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EXP-001" -Name "报销类型列表/详情"
  $expTypes = Invoke-Api -Method GET -Path "/api/Expenses/types" -Token $empToken
  $expTypeId = $null
  if ($expTypes.status -eq 200 -and $expTypes.content.Count -gt 0) { $expTypeId = $expTypes.content[0].id }
  $expTypeDetail = $null
  if ($expTypeId) { $expTypeDetail = Invoke-Api -Method GET -Path "/api/Expenses/types/$expTypeId" -Token $empToken }
  $case.durationMs = $expTypes.durationMs + ($(if ($expTypeDetail) { $expTypeDetail.durationMs } else { 0 }))
  $case.expected = "200"
  $case.actual = "list=$($expTypes.status),detail=$(if($expTypeDetail){$expTypeDetail.status}else{'NA'})"
  $case.pass = ($expTypes.status -eq 200 -and ($null -eq $expTypeDetail -or $expTypeDetail.status -eq 200))
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EXP-002" -Name "提交报销申请（金额边界）"
  $newExp = Invoke-Api -Method POST -Path "/api/Expenses/requests" -Token $empToken -Body @{
    expenseTypeId = $expTypeId
    amount = 500.0
    expenseDate = "2026-02-01"
    description = "test"
    currency = "CNY"
    items = @(@{ receiptUrl = "/demo-assets/receipts/receipt-001.svg" })
  }
  $case.durationMs = $newExp.durationMs
  $case.expected = "200/201 + id"
  $case.actual = "status=$($newExp.status)"
  $expenseRequestId = $null
  try {
    Assert-True ($newExp.status -in 200,201) "期望 200/201"
    $expenseRequestId = $newExp.content.id
    Assert-True ($null -ne $expenseRequestId) "缺少 id"
    $case.pass = $true
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-NFR-002" -Name "输入校验（负金额报销）"
  $badExp = Invoke-Api -Method POST -Path "/api/Expenses/requests" -Token $empToken -Body @{
    expenseTypeId = $expTypeId
    amount = -1
    expenseDate = "2026-02-01"
    description = "negative amount"
    currency = "CNY"
    items = @(@{ receiptUrl = "/demo-assets/receipts/receipt-001.svg" })
  }
  $case.durationMs = $badExp.durationMs
  $case.expected = "400"
  $case.actual = "status=$($badExp.status)"
  $case.pass = ($badExp.status -eq 400)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EXP-003" -Name "报销列表查询"
  $expList = Invoke-Api -Method GET -Path "/api/Expenses/requests?page=1&pageSize=20" -Token $empToken
  $case.durationMs = $expList.durationMs
  $case.expected = "200"
  $case.actual = "status=$($expList.status)"
  $case.pass = ($expList.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-EXP-004" -Name "报销审批"
  $expApprove = Invoke-Api -Method POST -Path "/api/Expenses/requests/$expenseRequestId/approve" -Token $hrToken -Body @{ approved = $true; comment = "ok" }
  $case.durationMs = $expApprove.durationMs
  $case.expected = "200"
  $case.actual = "status=$($expApprove.status)"
  $case.pass = ($expApprove.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-PAY-001" -Name "周期列表"
  $periods = Invoke-Api -Method GET -Path "/api/Payroll/periods" -Token $hrToken
  $case.durationMs = $periods.durationMs
  $case.expected = "200"
  $case.actual = "status=$($periods.status)"
  $periodId = $null
  if ($periods.status -eq 200 -and $periods.content.Count -gt 0) { $periodId = $periods.content[0].id }
  $case.pass = ($periods.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-PAY-002" -Name "薪资项列表"
  $items = Invoke-Api -Method GET -Path "/api/Payroll/items" -Token $hrToken
  $case.durationMs = $items.durationMs
  $case.expected = "200"
  $case.actual = "status=$($items.status)"
  $case.pass = ($items.status -eq 200)
  $results.Add($case)

  if ($periodId) {
    $case = New-CaseResult -Id "TC-PAY-006" -Name "周期计算/审批/锁定（接口可达）"
    $calc = Invoke-Api -Method POST -Path "/api/Payroll/periods/$periodId/calculate?isDryRun=true" -Token $adminToken -Body @{}
    $appr = Invoke-Api -Method POST -Path "/api/Payroll/periods/$periodId/approve" -Token $adminToken -Body @{}
    $lock = Invoke-Api -Method POST -Path "/api/Payroll/periods/$periodId/lock" -Token $adminToken -Body @{}
    $case.durationMs = $calc.durationMs + $appr.durationMs + $lock.durationMs
    $case.expected = "200"
    $case.actual = "calc=$($calc.status),approve=$($appr.status),lock=$($lock.status)"
    $case.pass = ($calc.status -eq 200 -and $appr.status -eq 200 -and $lock.status -eq 200)
    $results.Add($case)
  }

  $case = New-CaseResult -Id "TC-PAY-003" -Name "员工查询我的薪资单"
  $myPay = Invoke-Api -Method GET -Path "/api/Payroll/records/my?page=1&pageSize=10" -Token $empToken
  $case.durationMs = $myPay.durationMs
  $case.expected = "200"
  $case.actual = "status=$($myPay.status)"
  $case.pass = ($myPay.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-PAY-004" -Name "HR 查询薪资记录列表与详情"
  $payList = Invoke-Api -Method GET -Path "/api/Payroll/records?page=1&pageSize=10" -Token $hrToken
  $rid = $null
  if ($payList.status -eq 200 -and $payList.content.items.Count -gt 0) { $rid = $payList.content.items[0].id }
  $payDetail = $null
  if ($rid) { $payDetail = Invoke-Api -Method GET -Path "/api/Payroll/records/$rid" -Token $hrToken }
  $case.durationMs = $payList.durationMs + ($(if ($payDetail) { $payDetail.durationMs } else { 0 }))
  $case.expected = "200"
  $case.actual = "list=$($payList.status),detail=$(if($payDetail){$payDetail.status}else{'NA'})"
  $case.pass = ($payList.status -eq 200 -and ($null -eq $payDetail -or $payDetail.status -eq 200))
  $results.Add($case)

  $case = New-CaseResult -Id "TC-PAY-005" -Name "HR 查询指定员工薪资记录"
  $empPay = Invoke-Api -Method GET -Path "/api/Payroll/records/employee/$employeeId" -Token $hrToken
  $case.durationMs = $empPay.durationMs
  $case.expected = "200"
  $case.actual = "status=$($empPay.status)"
  $case.pass = ($empPay.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SCH-001" -Name "查询排班与统计"
  $sch = Invoke-Api -Method GET -Path "/api/Schedules?page=1&pageSize=10" -Token $hrToken
  $schStats = Invoke-Api -Method GET -Path "/api/Schedules/stats" -Token $hrToken
  $case.durationMs = $sch.durationMs + $schStats.durationMs
  $case.expected = "200"
  $case.actual = "list=$($sch.status),stats=$($schStats.status)"
  $case.pass = ($sch.status -eq 200 -and $schStats.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SCH-002" -Name "获取可排班员工与班次模板"
  $schEmp = Invoke-Api -Method GET -Path "/api/Schedules/schedulable-employees?page=1&pageSize=10" -Token $hrToken
  $templates = Invoke-Api -Method GET -Path "/api/Schedules/shift-templates?page=1&pageSize=10" -Token $hrToken
  $case.durationMs = $schEmp.durationMs + $templates.durationMs
  $case.expected = "200"
  $case.actual = "emp=$($schEmp.status),tpl=$($templates.status)"
  $case.pass = ($schEmp.status -eq 200 -and $templates.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SCH-003" -Name "分配排班写路径（可达性）"
  if (-not $employeeId -or -not $shiftId) {
    $case.durationMs = 0
    $case.expected = "200"
    $case.actual = "BLOCK(missing employeeId/shiftId)"
    $case.pass = $false
    Add-Note $case "BLOCK：缺少 employeeId 或 shiftId"
  } else {
    $assign = Invoke-Api -Method POST -Path "/api/Schedules/assign" -Token $hrToken -Body @{
      employeeId = $employeeId
      shiftId = $shiftId
      scheduleDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
    }
    $case.durationMs = $assign.durationMs
    $case.expected = "200"
    $case.actual = "status=$($assign.status)"
    $case.pass = ($assign.status -eq 200)
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SET-001" -Name "权限清单"
  $permissionCodes = @()
  $perms = Invoke-Api -Method GET -Path "/api/Settings/permissions" -Token $adminToken
  $case.durationMs = $perms.durationMs
  $case.expected = "200"
  $case.actual = "status=$($perms.status)"
  $case.pass = ($perms.status -eq 200)
  if ($perms.status -eq 200 -and $perms.content) {
    foreach ($p in @($perms.content)) {
      $code = ($p.code ?? $p.Code ?? $p.permissionCode ?? $p.PermissionCode)
      if ($code) { $permissionCodes += $code }
    }
    $permissionCodes = $permissionCodes | Select-Object -Unique
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SET-002" -Name "角色列表与详情"
  $roles = Invoke-Api -Method GET -Path "/api/Settings/roles?page=1&pageSize=20" -Token $adminToken
  $roleId = $null
  if ($roles.status -eq 200 -and $roles.content) {
    if ($roles.content.items -and $roles.content.items.Count -gt 0) { $roleId = $roles.content.items[0].id }
    elseif ($roles.content -is [array] -and $roles.content.Count -gt 0) { $roleId = $roles.content[0].id }
  }
  $roleDetail = $null
  if ($roleId) { $roleDetail = Invoke-Api -Method GET -Path "/api/Settings/roles/$roleId" -Token $adminToken }
  $case.durationMs = $roles.durationMs + ($(if ($roleDetail) { $roleDetail.durationMs } else { 0 }))
  $case.expected = "200"
  $case.actual = "list=$($roles.status),detail=$(if($roleDetail){$roleDetail.status}else{'NA'})"
  $case.pass = ($roles.status -eq 200 -and ($null -eq $roleDetail -or $roleDetail.status -eq 200))
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SET-003" -Name "角色权限查询/更新（临时角色）"
  if (-not $permissionCodes -or $permissionCodes.Count -lt 1) {
    $case.durationMs = 0
    $case.expected = "200"
    $case.actual = "BLOCK(empty permission list)"
    $case.pass = $false
    Add-Note $case "BLOCK：权限清单为空，无法验证角色权限更新"
  } else {
    $roleCode = ("AUTO_ROLE_" + ([Guid]::NewGuid().ToString("N").Substring(0, 8))).ToUpperInvariant()
    $createRole = Invoke-Api -Method POST -Path "/api/Settings/roles" -Token $adminToken -Body @{
      name = "自动化测试角色"
      code = $roleCode
      description = "created by blackbox runner"
      permissions = @($permissionCodes[0])
    }
    $newRoleId = $createRole.content.id

    $updatePerms = $null
    $deleteRole = $null
    if ($newRoleId) {
      $updatePerms = Invoke-Api -Method PUT -Path "/api/Settings/roles/$newRoleId/permissions" -Token $adminToken -Body @{
        permissions = @($permissionCodes[0])
      }
      $deleteRole = Invoke-Api -Method DELETE -Path "/api/Settings/roles/$newRoleId" -Token $adminToken
    }

    $case.durationMs =
      $createRole.durationMs +
      ($(if ($updatePerms) { $updatePerms.durationMs } else { 0 })) +
      ($(if ($deleteRole) { $deleteRole.durationMs } else { 0 }))
    $case.expected = "200"
    $case.actual = "create=$($createRole.status),update=$(if($updatePerms){$updatePerms.status}else{'NA'}),delete=$(if($deleteRole){$deleteRole.status}else{'NA'})"
    $case.pass = ($createRole.status -eq 200 -and $updatePerms -and $updatePerms.status -eq 200 -and $deleteRole -and $deleteRole.status -eq 200)
  }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SET-004" -Name "用户列表查询"
  $users = Invoke-Api -Method GET -Path "/api/Settings/users?page=1&pageSize=20" -Token $adminToken
  $case.durationMs = $users.durationMs
  $case.expected = "200"
  $case.actual = "status=$($users.status)"
  $case.pass = ($users.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-SET-005" -Name "系统配置查询"
  $cfg = Invoke-Api -Method GET -Path "/api/Settings/system-configs" -Token $adminToken
  $case.durationMs = $cfg.durationMs
  $case.expected = "200"
  $case.actual = "status=$($cfg.status)"
  $case.pass = ($cfg.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-APPR-001" -Name "审批流配置 CRUD（可达性）"
  $flows = Invoke-Api -Method GET -Path "/api/Settings/approval-flows" -Token $adminToken
  $flowCreate = Invoke-Api -Method POST -Path "/api/Settings/approval-flows" -Token $adminToken -Body @{ name = "自动化审批流"; type = "leave"; steps = @() }
  $flowDelete = Invoke-Api -Method DELETE -Path "/api/Settings/approval-flows/$([Guid]::NewGuid())" -Token $adminToken
  $case.durationMs = $flows.durationMs + $flowCreate.durationMs + $flowDelete.durationMs
  $case.expected = "200"
  $case.actual = "list=$($flows.status),create=$($flowCreate.status),delete=$($flowDelete.status)"
  $case.pass = ($flows.status -eq 200 -and $flowCreate.status -eq 200 -and $flowDelete.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AUD-001" -Name "审计日志查询与导出"
  $aud = Invoke-Api -Method GET -Path "/api/Settings/audit-logs?page=1&pageSize=20" -Token $adminToken
  $audExport = Invoke-Api -Method GET -Path "/api/Settings/audit-logs/export" -Token $adminToken
  $case.durationMs = $aud.durationMs + $audExport.durationMs
  $case.expected = "200"
  $case.actual = "list=$($aud.status),export=$($audExport.status)"
  $case.pass = ($aud.status -eq 200 -and $audExport.status -in 200,204)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-COMP-004" -Name "获取当前租户信息"
  $tenant = Invoke-Api -Method GET -Path "/api/Company/tenant" -Token $empToken
  $case.durationMs = $tenant.durationMs
  $case.expected = "200"
  $case.actual = "status=$($tenant.status)"
  $case.pass = ($tenant.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-COMP-001" -Name "成本中心树查询"
  $cc = Invoke-Api -Method GET -Path "/api/Company/cost-centers/tree" -Token $adminToken
  $ccCreate = Invoke-Api -Method POST -Path "/api/Company/cost-centers" -Token $adminToken -Body @{
    name = "自动化成本中心"
    code = ("AUTO_CC_" + ([Guid]::NewGuid().ToString("N").Substring(0, 6))).ToUpperInvariant()
  }
  $case.durationMs = $cc.durationMs + $ccCreate.durationMs
  $case.expected = "200"
  $case.actual = "tree=$($cc.status),create=$($ccCreate.status)"
  $case.pass = ($cc.status -eq 200 -and $ccCreate.status -eq 200 -and $ccCreate.raw -match '"id"')
  $results.Add($case)

  $case = New-CaseResult -Id "TC-COMP-002" -Name "标签列表查询"
  $tags = Invoke-Api -Method GET -Path "/api/Company/tags" -Token $adminToken
  $tagCreate = Invoke-Api -Method POST -Path "/api/Company/tags" -Token $adminToken -Body @{
    name = "自动化标签"
    code = ("AUTO_TAG_" + ([Guid]::NewGuid().ToString("N").Substring(0, 6))).ToUpperInvariant()
    color = "#FFAA00"
  }
  $case.durationMs = $tags.durationMs + $tagCreate.durationMs
  $case.expected = "200"
  $case.actual = "list=$($tags.status),create=$($tagCreate.status)"
  $case.pass = ($tags.status -eq 200 -and $tagCreate.status -eq 200 -and $tagCreate.raw -match '"id"')
  $results.Add($case)

  $case = New-CaseResult -Id "TC-COMP-003" -Name "打卡设备列表查询"
  $devices = Invoke-Api -Method GET -Path "/api/Company/clock-devices" -Token $adminToken
  $devCreate = Invoke-Api -Method POST -Path "/api/Company/clock-devices" -Token $adminToken -Body @{
    name = "自动化打卡设备"
    serialNumber = ("SN-" + ([Guid]::NewGuid().ToString("N").Substring(0, 8))).ToUpperInvariant()
  }
  $case.durationMs = $devices.durationMs + $devCreate.durationMs
  $case.expected = "200"
  $case.actual = "list=$($devices.status),create=$($devCreate.status)"
  $case.pass = ($devices.status -eq 200 -and $devCreate.status -eq 200 -and $devCreate.raw -match '"id"')
  $results.Add($case)

  $case = New-CaseResult -Id "TC-INS-001" -Name "保险接口可达"
  $insPlans = Invoke-Api -Method GET -Path "/api/Insurance/plans" -Token $hrToken
  $insBenefits = Invoke-Api -Method GET -Path "/api/Insurance/benefits-simple" -Token $hrToken
  $insStats = Invoke-Api -Method GET -Path "/api/Insurance/stats" -Token $hrToken
  $case.durationMs = $insPlans.durationMs + $insBenefits.durationMs + $insStats.durationMs
  $case.expected = "200"
  $case.actual = "plans=$($insPlans.status),benefits=$($insBenefits.status),stats=$($insStats.status)"
  $case.pass = ($insPlans.status -eq 200 -and $insBenefits.status -eq 200 -and $insStats.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-TAX-001" -Name "个税接口可达"
  $taxProfiles = Invoke-Api -Method GET -Path "/api/Tax/profiles?page=1&pageSize=20" -Token $hrToken
  $taxRecords = Invoke-Api -Method GET -Path "/api/Tax/records?page=1&pageSize=20" -Token $hrToken
  $taxSettings = Invoke-Api -Method GET -Path "/api/Tax/settings" -Token $hrToken
  $case.durationMs = $taxProfiles.durationMs + $taxRecords.durationMs + $taxSettings.durationMs
  $case.expected = "200"
  $case.actual = "profiles=$($taxProfiles.status),records=$($taxRecords.status),settings=$($taxSettings.status)"
  $case.pass = ($taxProfiles.status -eq 200 -and $taxRecords.status -eq 200 -and $taxSettings.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AI-001" -Name "AI 对话（Mock）"
  $chat = Invoke-Api -Method POST -Path "/api/Agent/chat" -Token $empToken -Body @{ message = "查询我的假期余额" }
  $case.durationMs = $chat.durationMs
  $case.expected = "200 + reply"
  $case.actual = "status=$($chat.status)"
  $case.pass = ($chat.status -eq 200 -and $chat.raw.Length -gt 0)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AI-002" -Name "AI 历史查询/清理"
  $hist1 = Invoke-Api -Method GET -Path "/api/Agent/history" -Token $empToken
  $del = Invoke-Api -Method DELETE -Path "/api/Agent/history" -Token $empToken
  $hist2 = Invoke-Api -Method GET -Path "/api/Agent/history" -Token $empToken
  $case.durationMs = $hist1.durationMs + $del.durationMs + $hist2.durationMs
  $case.expected = "200 + cleared"
  $case.actual = "get1=$($hist1.status),del=$($del.status),get2=$($hist2.status)"
  $case.pass = ($hist1.status -eq 200 -and $del.status -in 200,204 -and $hist2.status -eq 200)
  $results.Add($case)

  $case = New-CaseResult -Id "TC-AUTH-005" -Name "Logout（撤销 RefreshToken）"
  $tmpLogin = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{ username = "demo_user"; password = "demo123"; tenant = $TenantCode }
  $logoutOk = $false
  $case.durationMs = $tmpLogin.durationMs
  $case.expected = "logout 200 且 refresh 401"
  $case.actual = "login=$($tmpLogin.status)"
  try {
    Assert-True ($tmpLogin.status -eq 200) "临时登录失败"
    $tmpAccess = $tmpLogin.content.accessToken
    $tmpRefresh = $tmpLogin.content.refreshToken
    $logout = Invoke-Api -Method POST -Path "/api/Auth/logout" -Token $tmpAccess -Body @{}
    $refreshAfterLogout = Invoke-Api -Method POST -Path "/api/Auth/refresh" -Body @{ refreshToken = $tmpRefresh }
    $case.durationMs = $tmpLogin.durationMs + $logout.durationMs + $refreshAfterLogout.durationMs
    $case.actual = "logout=$($logout.status),refresh=$($refreshAfterLogout.status)"
    $logoutOk = ($logout.status -eq 200 -and $refreshAfterLogout.status -eq 401)
    $case.pass = $logoutOk
  } catch { Add-Note $case $_.Exception.Message }
  $results.Add($case)

  $case = New-CaseResult -Id "TC-PERF-001" -Name "性能基准抽样"
  $targets = @(
    @{ name = "OrganizationsTree"; path = "/api/Organizations/tree"; token = $hrToken },
    @{ name = "EmployeesList"; path = "/api/Employees?page=1&pageSize=20"; token = $hrToken },
    @{ name = "LeaveBalancesMy"; path = "/api/Leave/balances/my"; token = $empToken }
  )
  $case.expected = "每接口 30 次采样，输出 p50/p95"
  $t0 = [System.Diagnostics.Stopwatch]::StartNew()
  $perfItems = @()
  foreach ($t in $targets) {
    $durations = New-Object System.Collections.Generic.List[double]
    $errors = 0
    for ($i = 0; $i -lt 30; $i++) {
      $r = Invoke-Api -Method GET -Path $t.path -Token $t.token
      if ($r.status -ne 200) { $errors++ }
      $durations.Add([double]$r.durationMs)
    }
    $perfItems += [pscustomobject]@{
      name = $t.name
      path = $t.path
      samples = 30
      p50 = [math]::Round((Percentile -values $durations.ToArray() -p 50), 1)
      p95 = [math]::Round((Percentile -values $durations.ToArray() -p 95), 1)
      errorRate = [math]::Round(($errors / 30.0) * 100, 2)
    }
  }
  $t0.Stop()
  $case.durationMs = [int]$t0.ElapsedMilliseconds
  $case.actual = ($perfItems | ConvertTo-Json -Depth 10)
  $case.pass = ($perfItems | Where-Object { $_.errorRate -gt 0 }).Count -eq 0
  $results.Add($case)
  foreach ($pi in $perfItems) { $perf.Add($pi) }
}
finally {
}

$summary = [pscustomobject]@{
  runDateUtc = $runId
  baseUrl = $BaseUrl
  tenant = $TenantCode
  total = $results.Count
  passed = ($results | Where-Object { $_.pass }).Count
  failed = ($results | Where-Object { -not $_.pass }).Count
}

$outJson = Join-Path $OutDir "BlackBoxExecution_$runId.json"
$outMd = Join-Path $OutDir "BlackBoxExecution_$runId.md"
$outPerfJson = Join-Path $OutDir "BlackBoxPerf_$runId.json"

$payload = [pscustomobject]@{
  summary = $summary
  cases = $results
  perf = $perf
}
$payload | ConvertTo-Json -Depth 50 | Set-Content -Encoding UTF8 $outJson
$perf | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $outPerfJson

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Hrevolve 黑盒自动化执行结果")
$lines.Add("日期(UTC)：$runId")
$lines.Add("")
$lines.Add("## 统计")
$lines.Add("- 总用例：$($summary.total)")
$lines.Add("- 通过：$($summary.passed)")
$lines.Add("- 失败：$($summary.failed)")
$lines.Add("")
$lines.Add("## 用例明细")
$lines.Add("| 用例ID | 名称 | 结果 | 期望 | 实际 | 耗时(ms) | 备注 |")
$lines.Add("|---|---|---|---|---|---:|---|")
foreach ($c in $results) {
  $ok = if ($c.pass) { "PASS" } else { "FAIL" }
  $notes = ($c.notes -join " ; ").Replace("`r"," ").Replace("`n"," ")
  $lines.Add("| $($c.id) | $($c.name) | $ok | $($c.expected) | $($c.actual) | $($c.durationMs) | $notes |")
}
$lines.Add("")
$lines.Add("## 性能基准（抽样）")
$lines.Add("| 指标 | 接口 | 样本数 | p50(ms) | p95(ms) | 错误率 |")
$lines.Add("|---|---|---:|---:|---:|---:|")
foreach ($pi in $perf) {
  $lines.Add("| $($pi.name) | $($pi.path) | $($pi.samples) | $($pi.p50) | $($pi.p95) | $($pi.errorRate)% |")
}

$lines | Set-Content -Encoding UTF8 $outMd

Write-Output ("Done. Results: " + $outMd)
