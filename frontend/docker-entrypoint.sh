#!/bin/sh
set -eu

rm -f /etc/nginx/sites-enabled/default

SSR_PID=""
NGINX_PID=""

cleanup() {
  if [ -n "${NGINX_PID}" ] && kill -0 "${NGINX_PID}" 2>/dev/null; then
    kill -TERM "${NGINX_PID}" 2>/dev/null || true
  fi
  if [ -n "${SSR_PID}" ] && kill -0 "${SSR_PID}" 2>/dev/null; then
    kill -TERM "${SSR_PID}" 2>/dev/null || true
  fi
  wait "${NGINX_PID}" 2>/dev/null || true
  wait "${SSR_PID}" 2>/dev/null || true
}

trap cleanup TERM INT EXIT

node /app/dist/nezabuti/server/server.mjs &
SSR_PID=$!

# Wait until SSR answers before starting nginx
i=0
while [ "$i" -lt 60 ]; do
  if ! kill -0 "${SSR_PID}" 2>/dev/null; then
    echo "Angular SSR process exited during startup" >&2
    exit 1
  fi
  if curl -fsS "http://127.0.0.1:${PORT:-4000}/" >/dev/null 2>&1; then
    break
  fi
  i=$((i + 1))
  sleep 0.5
done

if ! curl -fsS "http://127.0.0.1:${PORT:-4000}/" >/dev/null 2>&1; then
  echo "Angular SSR failed to become ready" >&2
  exit 1
fi

nginx -g 'daemon off;' &
NGINX_PID=$!

# Exit container if either process dies (keeps health semantics honest)
while true; do
  if ! kill -0 "${SSR_PID}" 2>/dev/null; then
    echo "Angular SSR process died" >&2
    exit 1
  fi
  if ! kill -0 "${NGINX_PID}" 2>/dev/null; then
    echo "nginx process died" >&2
    exit 1
  fi
  sleep 2
done
