FROM node:20-alpine AS build
WORKDIR /app
COPY Frontend/hrevolve-web/package.json Frontend/hrevolve-web/package-lock.json ./
RUN npm ci
COPY Frontend/hrevolve-web/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist/ /usr/share/nginx/html/
COPY Frontend/hrevolve-web/nginx/default.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
