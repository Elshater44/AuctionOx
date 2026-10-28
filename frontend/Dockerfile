# ----------------------------------------------------
# Stage 1: Build React SPA
# ----------------------------------------------------
FROM node:22-alpine AS build

WORKDIR /app

# Cache dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Copy source files
COPY . .

# Set default build argument for API URL (relative /api routes through Nginx reverse proxy)
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL

# Build production bundle
RUN npm run build

# ----------------------------------------------------
# Stage 2: Production Nginx Server
# ----------------------------------------------------
FROM nginx:alpine

# Copy built assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
