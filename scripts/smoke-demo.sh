#!/usr/bin/env bash
set -euo pipefail

base_url="${1:?Usage: scripts/smoke-demo.sh https://demo.example.com}"
base_url="${base_url%/}"

paths=(
  "/"
  "/disponibilites"
  "/reserve"
  "/login"
  "/health/live"
  "/health/ready"
)

for path in "${paths[@]}"; do
  status="$(curl --silent --show-error --location --output /dev/null --write-out "%{http_code}" "${base_url}${path}")"
  if [[ "$status" != "200" ]]; then
    echo "Smoke check failed for ${path}: HTTP ${status}"
    exit 1
  fi
  echo "OK ${path}: HTTP ${status}"
done

ready_status="$(curl --silent --show-error --output /dev/null --write-out "%{http_code}" "${base_url}/health/ready")"
if [[ "$ready_status" != "200" ]]; then
  echo "Health gate failed: /health/ready returned HTTP ${ready_status}"
  exit 1
fi

echo "Demo smoke checks passed for ${base_url}."
