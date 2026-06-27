FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_API_URL=http://localhost:5247
ARG VITE_TENANT_PANEL_URL=http://localhost:5173
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_TENANT_PANEL_URL=$VITE_TENANT_PANEL_URL

RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
