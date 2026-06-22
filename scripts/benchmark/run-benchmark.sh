#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
API_URL="${API_URL:-http://localhost:5247}"
PG_CONTAINER="${PG_CONTAINER:-enterprise-postgres}"
PG_USER="${POSTGRES_USER:-saas_admin}"
PG_DB="${POSTGRES_DB:-EnterpriseLoggerDb}"
TRACE_ID="${TRACE_ID:-bench-trace-001}"

BENCH_EMAIL="bench-$(date +%s)@benchmark.local"
BENCH_PASSWORD="BenchPass123!"
BENCH_PHONE="05550000001"
BENCH_TENANT_NAME="Benchmark-Fat-$(date +%s)"
SECONDARY_TENANT_NAME="Benchmark-Thin-$(date +%s)"

REPORT_FILE="${ROOT_DIR}/scripts/benchmark/results-$(date +%Y%m%d-%H%M%S).md"

log() { echo "[benchmark] $*"; }

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || { echo "Missing command: $1"; exit 1; }
}

curl_timing() {
  local label="$1"
  shift
  local outfile timing_http timing_total status

  outfile="$(mktemp)"
  timing_http="$(curl -s -o "$outfile" -w '%{http_code} %{time_starttransfer} %{time_total}' "$@")"
  status="$(echo "$timing_http" | awk '{print $1}')"
  local ttfb total
  ttfb="$(echo "$timing_http" | awk '{print $2}')"
  total="$(echo "$timing_http" | awk '{print $3}')"

  echo "| ${label} | ${status} | ${ttfb} | ${total} |" >> "$REPORT_FILE"
  rm -f "$outfile"
}

seed_rows() {
  local tenant_id="$1"
  local row_count="$2"
  local started ended elapsed

  log "Seeding tenant ${tenant_id} with ${row_count} rows..."
  started="$(date +%s)"

  docker exec -i "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" \
    -v tenant_id="$tenant_id" \
    -v row_count="$row_count" \
    -v batch_label="$TRACE_ID" \
    < "${ROOT_DIR}/scripts/benchmark/seed-logs.sql" >/dev/null

  ended="$(date +%s)"
  elapsed=$((ended - started))
  echo "| seed +${row_count} | ${elapsed}s | - | - |" >> "$REPORT_FILE"
  log "Seed done in ${elapsed}s"
}

count_logs() {
  local tenant_id="$1"
  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" -t -A \
    -c "SELECT COUNT(*) FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id};"
}

correlation_count() {
  local tenant_id="$1"
  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" -t -A \
    -c "SELECT COUNT(*) FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id} AND \"CorrelationId\" = '${TRACE_ID}';"
}

measure_sql() {
  local label="$2"
  local sql="$3"
  local start end ms

  start="$(python3 -c 'import time; print(time.perf_counter())')"
  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" -t -A -c "$sql" >/dev/null
  end="$(python3 -c 'import time; print(time.perf_counter())')"
  ms="$(python3 - <<PY
start = float("${start}")
end = float("${end}")
print(f"{(end - start) * 1000:.1f}")
PY
)"
  echo "| SQL: ${label} | 200 | ${ms}ms | ${ms}ms |" >> "$REPORT_FILE"
}

api_headers() {
  echo "X-Api-Key: $API_KEY"
}

run_api_suite() {
  local tenant_id="$1"
  local total
  total="$(count_logs "$tenant_id")"

  {
    echo ""
    echo "### Tenant ${tenant_id} — ${total} logs (API)"
    echo ""
    echo "| Endpoint | HTTP | TTFB (s) | Total (s) |"
    echo "|----------|------|----------|-----------|"
  } >> "$REPORT_FILE"

  curl_timing "GET /api/logs page=1" \
    -H "$(api_headers)" "${API_URL}/api/logs?page=1&pageSize=25"

  curl_timing "GET /api/logs page=40" \
    -H "$(api_headers)" "${API_URL}/api/logs?page=40&pageSize=25"

  curl_timing "GET /api/logs correlationId" \
    -H "$(api_headers)" "${API_URL}/api/logs?correlationId=${TRACE_ID}&page=1&pageSize=25"

  curl_timing "GET /api/logs export scope" \
    -H "$(api_headers)" "${API_URL}/api/logs/export?page=1&pageSize=25"
}

run_sql_suite() {
  local tenant_id="$1"
  local total
  total="$(count_logs "$tenant_id")"
  local trace_count
  trace_count="$(correlation_count "$tenant_id")"

  {
    echo ""
    echo "### Tenant ${tenant_id} — ${total} logs (${trace_count} trace rows) — direct SQL"
    echo ""
    echo "| Query | HTTP | TTFB (s) | Total (s) |"
    echo "|-------|------|----------|-----------|"
  } >> "$REPORT_FILE"

  measure_sql "$tenant_id" "COUNT all tenant logs" \
    "SELECT COUNT(*) FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id};"

  measure_sql "$tenant_id" "Summary by level" \
    "SELECT \"LogLevel\", COUNT(*) FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id} GROUP BY \"LogLevel\";"

  measure_sql "$tenant_id" "Page 1 ORDER BY Timestamp DESC LIMIT 25" \
    "SELECT \"Id\" FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id} ORDER BY \"Timestamp\" DESC LIMIT 25;"

  measure_sql "$tenant_id" "Deep page OFFSET 975 LIMIT 25" \
    "SELECT \"Id\" FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id} ORDER BY \"Timestamp\" DESC OFFSET 975 LIMIT 25;"

  measure_sql "$tenant_id" "CorrelationId filter" \
    "SELECT COUNT(*) FROM \"Logs\" WHERE \"TenantId\" = ${tenant_id} AND \"CorrelationId\" = '${TRACE_ID}';"
}

create_benchmark_tenant() {
  log "Creating benchmark tenant via API..."
  curl -sf -X POST "${API_URL}/api/tenants" \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"${BENCH_TENANT_NAME}\",\"ownerEmail\":\"${BENCH_EMAIL}\",\"ownerPhone\":\"${BENCH_PHONE}\",\"ownerPassword\":\"${BENCH_PASSWORD}\"}" >/dev/null

  local token
  token="$(curl -sf -X POST "${API_URL}/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${BENCH_EMAIL}\",\"password\":\"${BENCH_PASSWORD}\"}" \
    | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")"

  API_KEY="$(curl -sf -X POST "${API_URL}/api/tenants/me/api-key/rotate" \
    -H "Authorization: Bearer ${token}" \
    | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['apiKey'])")"

  FAT_TENANT_ID="$(docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" -t -A \
    -c "SELECT \"Id\" FROM \"Tenants\" WHERE \"Name\" = '${BENCH_TENANT_NAME}' LIMIT 1;")"

  log "Benchmark tenant id=${FAT_TENANT_ID}"
}

create_secondary_tenant() {
  local email="thin-$(date +%s)@benchmark.local"
  curl -sf -X POST "${API_URL}/api/tenants" \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"${SECONDARY_TENANT_NAME}\",\"ownerEmail\":\"${email}\",\"ownerPhone\":\"05550000002\",\"ownerPassword\":\"${BENCH_PASSWORD}\"}" >/dev/null

  THIN_TENANT_ID="$(docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$PG_DB" -t -A \
    -c "SELECT \"Id\" FROM \"Tenants\" WHERE \"Name\" = '${SECONDARY_TENANT_NAME}' LIMIT 1;")"

  log "Secondary tenant id=${THIN_TENANT_ID}"
}

main() {
  require_cmd curl
  require_cmd docker
  require_cmd python3

  {
    echo "# EnterpriseLogger Benchmark Report"
    echo ""
    echo "- Date: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
    echo "- API: ${API_URL}"
    echo "- Trace id: ${TRACE_ID}"
    echo "- Method: SQL bulk seed (generate_series) + curl API timing"
    echo ""
    echo "## Seed timings"
    echo ""
    echo "| Phase | Seconds | TTFB | Total |"
    echo "|-------|---------|------|-------|"
  } > "$REPORT_FILE"

  create_benchmark_tenant
  create_secondary_tenant

  # Secondary tenant: fixed 50k for multi-tenant noise
  seed_rows "$THIN_TENANT_ID" 50000

  for target in 100000 500000 1000000; do
    current="$(count_logs "$FAT_TENANT_ID")"
    to_add=$((target - current))
    if [ "$to_add" -le 0 ]; then
      log "Tenant ${FAT_TENANT_ID} already at ${current} logs (target ${target})"
    else
      seed_rows "$FAT_TENANT_ID" "$to_add"
    fi

    {
      echo ""
      echo "## Tier: ${target} logs on fat tenant"
      echo ""
      echo "- Fat tenant id: ${FAT_TENANT_ID}"
      echo "- Thin tenant id: ${THIN_TENANT_ID} ($(count_logs "$THIN_TENANT_ID") logs)"
      echo "- Correlation rows (${TRACE_ID}): $(correlation_count "$FAT_TENANT_ID")"
    } >> "$REPORT_FILE"

    run_sql_suite "$FAT_TENANT_ID"
    run_api_suite "$FAT_TENANT_ID"
  done

  log "Report written to ${REPORT_FILE}"
  cat "$REPORT_FILE"
}

main "$@"
