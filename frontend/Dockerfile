# ---- build ----
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# VITE_* вшиваются в бандл на сборке. Ключ проекта «Ариадны» — публичный (pub_…),
# в бандле он не секрет; при пустых значениях аналитика просто не подключается.
ARG VITE_ARIADNE_ENDPOINT=
ARG VITE_ARIADNE_PROJECT_KEY=
RUN VITE_ARIADNE_ENDPOINT="$VITE_ARIADNE_ENDPOINT" \
    VITE_ARIADNE_PROJECT_KEY="$VITE_ARIADNE_PROJECT_KEY" \
    npm run build

# ---- runtime: nginx раздаёт статику и проксирует /api на backend ----
FROM nginx:1.27-alpine
ENV API_UPSTREAM=api
ENV NGINX_ENVSUBST_FILTER=API_UPSTREAM
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
