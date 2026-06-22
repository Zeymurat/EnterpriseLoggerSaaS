import { getAccessToken } from '@/lib/auth'

export interface ApiResult<T> {
  data: T | null
  isSuccess: boolean
  errorMessage: string | null
  errorKind?: number
}

export interface PlatformAdminInfo {
  id: number
  email: string
}

export interface PlatformLoginResponse {
  accessToken: string
  expiresIn: number
  admin: PlatformAdminInfo
}

export interface PlatformLoginRequest {
  email: string
  password: string
}

export const SubscriptionStatus = {
  Active: 0,
  PendingPayment: 1,
  PastDue: 2,
  Cancelled: 3,
  Superseded: 4,
} as const

export type SubscriptionStatusValue =
  (typeof SubscriptionStatus)[keyof typeof SubscriptionStatus]

export const BillingCycle = {
  Monthly: 0,
  Quarterly: 1,
  SemiAnnual: 2,
  Annual: 3,
} as const

export type BillingCycleValue = (typeof BillingCycle)[keyof typeof BillingCycle]

export interface PlatformTenantListItem {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  userCount: number
  logCount: number
  currentPackageName: string | null
  currentSubscriptionStatus: SubscriptionStatusValue | null
}

export interface PlatformTenantListResponse {
  tenants: PlatformTenantListItem[]
}

export interface PlatformSubscription {
  id: number
  packageId: number
  packageCode: string
  packageName: string
  status: SubscriptionStatusValue
  billingCycle: BillingCycleValue
  startDate: string
  endDate: string
  gracePeriodEndDate: string | null
  isPaid: boolean
  autoRenew: boolean
  maxLogsPerMinute: number
  monthlyRequestLimit: number
}

export interface PlatformTenantDetail {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  userCount: number
  logCount: number
  currentSubscription: PlatformSubscription | null
  subscriptionHistory: PlatformSubscription[]
}

export interface PlatformPackage {
  id: number
  code: string
  name: string
  description: string
  allowedLogLevels: string
  isMailEnabled: boolean
  isSmsEnabled: boolean
  monthlyRequestLimit: number
  maxLogsPerMinute: number
  storageRetentionDays: number
  priceMonthly: number
  priceQuarterly: number
  priceSemiAnnual: number
  priceAnnual: number
  isDefault: boolean
  isAvailable: boolean
  sortOrder: number
}

export interface PlatformPackageListResponse {
  packages: PlatformPackage[]
}

export interface AssignTenantSubscriptionRequest {
  packageId: number
  billingCycle: BillingCycleValue
  autoRenew: boolean
  isPaid: boolean
  gracePeriodEndDate?: string | null
}

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5247'

export const NETWORK_ERROR_MESSAGE =
  'Sunucuya ulaşılamadı. Lütfen az sonra tekrar deneyin.'

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function apiFetch(url: string, options?: RequestInit): Promise<Response> {
  try {
    return await fetch(url, options)
  } catch {
    throw new ApiError(NETWORK_ERROR_MESSAGE, 0)
  }
}

async function parseResult<T>(response: Response): Promise<T> {
  const body = (await response.json()) as ApiResult<T>

  if (!response.ok || !body.isSuccess) {
    throw new ApiError(body.errorMessage ?? 'İstek başarısız oldu.', response.status)
  }

  return body.data as T
}

function authHeaders(): HeadersInit {
  const token = getAccessToken()
  if (!token) throw new ApiError('Oturum gerekli.', 401)
  return { Authorization: `Bearer ${token}` }
}

export function isUnauthorizedError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}

export function subscriptionStatusLabel(status: SubscriptionStatusValue): string {
  switch (status) {
    case SubscriptionStatus.Active:
      return 'Aktif'
    case SubscriptionStatus.PendingPayment:
      return 'Ödeme bekliyor'
    case SubscriptionStatus.PastDue:
      return 'Gecikmiş'
    case SubscriptionStatus.Cancelled:
      return 'İptal'
    case SubscriptionStatus.Superseded:
      return 'Kapatıldı'
    default:
      return 'Bilinmiyor'
  }
}

export function billingCycleLabel(cycle: BillingCycleValue): string {
  switch (cycle) {
    case BillingCycle.Monthly:
      return 'Aylık'
    case BillingCycle.Quarterly:
      return '3 Aylık'
    case BillingCycle.SemiAnnual:
      return '6 Aylık'
    case BillingCycle.Annual:
      return 'Yıllık'
    default:
      return 'Bilinmiyor'
  }
}

export async function platformLogin(
  request: PlatformLoginRequest,
): Promise<PlatformLoginResponse> {
  const response = await apiFetch(`${API_URL}/api/platform/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  return parseResult<PlatformLoginResponse>(response)
}

export async function getPlatformTenants(): Promise<PlatformTenantListResponse> {
  const response = await apiFetch(`${API_URL}/api/platform/tenants`, {
    headers: authHeaders(),
  })

  return parseResult<PlatformTenantListResponse>(response)
}

export async function getPlatformTenantDetail(
  tenantId: number,
): Promise<PlatformTenantDetail> {
  const response = await apiFetch(`${API_URL}/api/platform/tenants/${tenantId}`, {
    headers: authHeaders(),
  })

  return parseResult<PlatformTenantDetail>(response)
}

export async function getPlatformPackages(): Promise<PlatformPackageListResponse> {
  const response = await apiFetch(`${API_URL}/api/platform/packages`, {
    headers: authHeaders(),
  })

  return parseResult<PlatformPackageListResponse>(response)
}

export async function assignTenantSubscription(
  tenantId: number,
  request: AssignTenantSubscriptionRequest,
): Promise<PlatformSubscription> {
  const response = await apiFetch(`${API_URL}/api/platform/tenants/${tenantId}/subscription`, {
    method: 'POST',
    headers: {
      ...authHeaders(),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  const data = await parseResult<{ subscription: PlatformSubscription }>(response)
  return data.subscription
}
