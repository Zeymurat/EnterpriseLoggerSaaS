-- Bulk seed logs for benchmark tenant.
-- Usage: psql ... -v tenant_id=9 -v row_count=100000 -v batch_label=bench-trace-001

INSERT INTO "Logs" (
    "TenantId",
    "ApplicationName",
    "LogLevel",
    "Message",
    "Timestamp",
    "HttpMethod",
    "RequestPath",
    "StatusCode",
    "CorrelationId"
)
SELECT
    :tenant_id,
    CASE (g % 4)
        WHEN 0 THEN 'AuthService'
        WHEN 1 THEN 'BillingService'
        WHEN 2 THEN 'InventoryApi'
        ELSE 'NotificationWorker'
    END,
    CASE (g % 3)
        WHEN 0 THEN 'Info'
        WHEN 1 THEN 'Warning'
        ELSE 'Error'
    END,
    'Benchmark row ' || g || ' [' || :'batch_label' || ']',
    NOW() - ((g % 2592000) * INTERVAL '1 second'),
    CASE WHEN g % 5 = 0 THEN 'POST' ELSE 'GET' END,
    '/api/resource/' || (g % 200),
    CASE WHEN g % 7 = 0 THEN 500 ELSE 200 END,
    CASE
        WHEN g % 100 = 0 THEN :'batch_label'
        WHEN g % 1000 = 0 THEN 'bench-trace-secondary'
        ELSE NULL
    END
FROM generate_series(1, :row_count) AS g;

ANALYZE "Logs";
