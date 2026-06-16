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
  },
  {
    id: -2,
    tenantId: 0,
    applicationName: 'AuthApi',
    logLevel: 'Warning',
    message: 'Login attempt with valid email but wrong password (rate limit: 3/5)',
    timestamp: minutesAgo(5),
  },
  {
    id: -3,
    tenantId: 0,
    applicationName: 'PaymentGateway',
    logLevel: 'Error',
    message: 'Stripe webhook signature verification failed — retry scheduled',
    timestamp: minutesAgo(8),
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
  },
  {
    id: -6,
    tenantId: 0,
    applicationName: 'BillingService',
    logLevel: 'Error',
    message: 'Failed to charge saved card — insufficient funds (customer_id: cus_9x2)',
    timestamp: minutesAgo(25),
  },
  {
    id: -7,
    tenantId: 0,
    applicationName: 'AuthApi',
    logLevel: 'Info',
    message: 'JWT issued for user role=Admin tenant=zeymurat',
    timestamp: minutesAgo(31),
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
  },
  {
    id: -10,
    tenantId: 0,
    applicationName: 'InventoryApi',
    logLevel: 'Error',
    message: 'Database connection pool exhausted — request rejected',
    timestamp: minutesAgo(67),
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
  },
]

export const DEMO_LOG_PAYLOADS: CreateLogRequest[] = MOCK_LOG_ENTRIES.map((log) => ({
  applicationName: log.applicationName,
  logLevel: log.logLevel,
  message: log.message,
}))

export function isMockLogEntry(log: LogEntry): boolean {
  return log.id < 0
}
