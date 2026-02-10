# Docker 一键部署

## 前置条件

- 已安装 Docker Desktop（含 Docker Compose v2）

## 启动

1. 复制环境变量文件：

   - Windows PowerShell：
     - `Copy-Item .env.example .env`

2. 修改 `.env` 里的 `JWT_KEY`（至少 32 字符）

3. 启动：

   - `docker compose up -d --build`

## 访问

- Web：`http://localhost:${WEB_PORT}`（默认 `http://localhost:8080`）
- 后端健康检查：`http://localhost:${WEB_PORT}/api/health`
- Ollama：`http://localhost:${OLLAMA_PORT}`（默认 `http://localhost:11434`）

## CI/CD（GitHub Actions）

### 镜像构建与发布

- 工作流：`.github/workflows/docker.yml`
- 默认推送到：`ghcr.io/<owner>/hrevolve-api` 与 `ghcr.io/<owner>/hrevolve-web`

### 自动部署（可选）

设置以下 Secrets 后，push main 会自动 SSH 到服务器执行 `docker compose -f docker-compose.prod.yml pull/up`：

- `DEPLOY_HOST`
- `DEPLOY_USER`
- `DEPLOY_KEY`（私钥）
- `DEPLOY_PATH`（服务器上包含 `docker-compose.prod.yml` 与 `.env` 的目录）

服务器侧 `.env` 需要至少包含：

- `IMAGE_NAMESPACE`（例如 `my-org`，与镜像仓库路径一致）
- `JWT_KEY`（至少 32 字符）

## 停止与清理

- 停止：`docker compose down`
- 连同数据卷一起清理：`docker compose down -v`
