# Build Angular SSR
FROM node:22-bookworm AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG PUBLIC_BASE_URL=http://localhost:8088
ENV PUBLIC_BASE_URL=$PUBLIC_BASE_URL
RUN npm run build

# Runtime: nginx (port 80) + Angular SSR Node (port 4000)
FROM node:22-bookworm-slim AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/dist /app/dist
COPY --from=build /app/package.json /app/package.json
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN rm -f /etc/nginx/sites-enabled/default || true \
    && printf 'server_tokens off;\n' > /etc/nginx/conf.d/00-general.conf
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh \
    && sed -i 's/\r$//' /docker-entrypoint.sh

ENV PORT=4000
ENV NODE_ENV=production
ENV SSR_API_ORIGIN=http://backend:8080
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=3 \
  CMD curl -fsS http://127.0.0.1:4000/ >/dev/null && curl -fsS http://127.0.0.1/ >/dev/null || exit 1
ENTRYPOINT ["/docker-entrypoint.sh"]
