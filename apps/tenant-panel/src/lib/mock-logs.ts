import type { CreateLogRequest, LogEntry } from '@/lib/api'

const now = Date.now()

function minutesAgo(minutes: number): string {
  return new Date(now - minutes * 60_000).toISOString()
}

/** Panel geliştirme / boş tenant için örnek log kayıtları */
export const MOCK_LOG_ENTRIES: LogEntry[] = [
  {
    id: -1,
    tenantId: 0,
    applicationName: 'BillingService',
    logLevel: 'Info',
    message: 'Payment processed successfully for invoice #INV-2048',
    timestamp: minutesAgo(2),
    httpMethod: 'POST',
    requestPath: '/api/payments/charge',
    statusCode: 200,
    correlationId: 'req_8f2a1b',
    actorIdentifier: 'customer@acme.com',
  },
  {
    id: -2,
    tenantId: 0,
    applicationName: 'AuthApi',
    logLevel: 'Warning',
    message: 'Login attempt with valid email but wrong password (rate limit: 3/5)',
    timestamp: minutesAgo(5),
    httpMethod: 'POST',
    requestPath: '/api/auth/login',
    statusCode: 401,
    actorIdentifier: 'consultant@shared.com',
  },
  {
    id: -3,
    tenantId: 0,
    applicationName: 'PaymentGateway',
    logLevel: 'Error',
    message: 'Stripe webhook signature verification failed — retry scheduled',
    timestamp: minutesAgo(8),
    httpMethod: 'POST',
    requestPath: '/webhooks/stripe',
    statusCode: 400,
    correlationId: 'wh_91c4d0',
    exceptionType: 'StripeSignatureException',
  },
  {
    id: -4,
    tenantId: 0,
    applicationName: 'NotificationWorker',
    logLevel: 'Info',
    message: 'Queued 142 digest emails for tenant batch delivery',
    timestamp: minutesAgo(12),
  },
  {
    id: -5,
    tenantId: 0,
    applicationName: 'InventoryApi',
    logLevel: 'Warning',
    message: 'Stock level below threshold for SKU-8821 (remaining: 4)',
    timestamp: minutesAgo(18),
    httpMethod: 'GET',
    requestPath: '/api/inventory/sku-8821',
    statusCode: 200,
  },
  {
    id: -6,
    tenantId: 0,
    applicationName: 'BillingService',
    logLevel: 'Error',
    message: 'Failed to charge saved card — insufficient funds',
    timestamp: minutesAgo(25),
    httpMethod: 'POST',
    requestPath: '/api/payments/charge',
    statusCode: 402,
    correlationId: 'req_7e19ff',
    actorIdentifier: 'customer@acme.com',
    exceptionType: 'PaymentDeclinedException',
  },
  {
    id: -7,
    tenantId: 0,
    applicationName: 'AuthApi',
    logLevel: 'Info',
    message: 'JWT issued for Admin role',
    timestamp: minutesAgo(31),
    httpMethod: 'POST',
    requestPath: '/api/auth/login',
    statusCode: 200,
    actorIdentifier: 'admin@acme.com',
  },
  {
    id: -8,
    tenantId: 0,
    applicationName: 'ReportScheduler',
    logLevel: 'Info',
    message: 'Daily log summary report generated (1,284 entries)',
    timestamp: minutesAgo(45),
  },
  {
    id: -9,
    tenantId: 0,
    applicationName: 'PaymentGateway',
    logLevel: 'Warning',
    message: 'Webhook delivery delayed — upstream latency 2.4s',
    timestamp: minutesAgo(52),
    httpMethod: 'POST',
    requestPath: '/webhooks/stripe',
    statusCode: 504,
  },
  {
    id: -10,
    tenantId: 0,
    applicationName: 'InventoryApi',
    logLevel: 'Error',
    message: 'Database connection pool exhausted — request rejected',
    timestamp: minutesAgo(67),
    httpMethod: 'GET',
    requestPath: '/api/inventory',
    statusCode: 503,
    exceptionType: 'NpgsqlException',
  },
  {
    id: -11,
    tenantId: 0,
    applicationName: 'NotificationWorker',
    logLevel: 'Info',
    message: 'SMS provider failover completed — using secondary route',
    timestamp: minutesAgo(90),
  },
  {
    id: -12,
    tenantId: 0,
    applicationName: 'BillingService',
    logLevel: 'Warning',
    message: 'Subscription renewal reminder sent — 7 days before expiry',
    timestamp: minutesAgo(120),
    httpMethod: 'POST',
    requestPath: '/api/subscriptions/remind',
    statusCode: 202,
  },
]

function toCreatePayload(log: LogEntry): CreateLogRequest {
  return {
    applicationName: log.applicationName,
    logLevel: log.logLevel,
    message: log.message,
    ...(log.httpMethod ? { httpMethod: log.httpMethod } : {}),
    ...(log.requestPath ? { requestPath: log.requestPath } : {}),
    ...(log.statusCode != null ? { statusCode: log.statusCode } : {}),
    ...(log.correlationId ? { correlationId: log.correlationId } : {}),
    ...(log.actorIdentifier ? { actorIdentifier: log.actorIdentifier } : {}),
    ...(log.exceptionType ? { exceptionType: log.exceptionType } : {}),
  }
}

export const DEMO_LOG_PAYLOADS: CreateLogRequest[] = MOCK_LOG_ENTRIES.map(toCreatePayload)

export function isMockLogEntry(log: LogEntry): boolean {
  return log.id < 0
}
