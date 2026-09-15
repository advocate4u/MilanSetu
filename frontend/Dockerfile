# Build the React application
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json ./
RUN npm install --ignore-scripts
COPY . ./
RUN npm run build

# Serve the SPA from an unprivileged nginx worker
FROM nginxinc/nginx-unprivileged:stable-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
