# Home System UI image: the Vite build served by unprivileged nginx on 8080. Build context is src/ui:
#   docker build -t home-system-ui src/ui
# Nothing environment-specific is baked in: mount config.js over /usr/share/nginx/html/config.js
# (see public/config.js). Runs with a read-only root filesystem when /tmp is writable.

FROM node:24-alpine AS build
WORKDIR /ui
COPY package.json package-lock.json ./
# --ignore-scripts: `prepare` points git hooks at the repo root, which is not in this context.
RUN npm ci --ignore-scripts
COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:stable-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /ui/dist /usr/share/nginx/html
EXPOSE 8080
