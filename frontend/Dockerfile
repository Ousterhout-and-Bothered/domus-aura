# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /src
ARG BUILD_CONFIG=docker

# Restore dependencies as a separate layer
COPY package*.json ./
RUN npm ci

# Build the production bundle
COPY . .
RUN npm run build -- --configuration ${BUILD_CONFIG}

# Stage 2: Serve
FROM nginx:1.27-alpine AS final

# Replace default nginx config with one that handles Angular's client-side routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Angular 21 with @angular/build:application outputs to dist/<project>/browser
COPY --from=build /src/dist/smart-home/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
