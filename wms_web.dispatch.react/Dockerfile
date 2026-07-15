## Multi-stage build: build React app then serve with Nginx

# 1) Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Install deps first for better caching
COPY wms_web.dispatch.react/client/package.json wms_web.dispatch.react/client/package-lock.json ./client/
RUN cd client && npm ci --no-audit --no-fund

# Copy source and build
COPY wms_web.dispatch.react/client ./client
WORKDIR /app/client
# Environment can be overridden at build-time with --build-arg
ARG REACT_APP_API_BASE_URL
ENV REACT_APP_API_BASE_URL=${REACT_APP_API_BASE_URL}
RUN npm run build

# 2) Runtime stage
FROM nginx:1.25-alpine AS runtime

# Copy custom nginx config
COPY wms_web.dispatch.react/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /app/client/build /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]


