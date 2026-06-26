import { getAccessToken } from '@/lib/auth'
import { recordSessionActivity } from '@/lib/session-activity'
import { notifyUnauthorizedSession } from '@/lib/unauthorized-session'

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
  rootEmail: string | null
  rootPhone: string | null
  currentPackageName: string | null
  currentSubscriptionStatus: SubscriptionStatusValue | null
  currentSubscriptionStartDate: string | null
}

export interface PlatformTenantListFilters {
  name?: string
  rootEmail?: string
  rootPhone?: string
  packageCode?: string
  isActive?: boolean
  subscriptionStartFrom?: string
  subscriptionStartTo?: string
}

export type PlatformTenantSortField = 'createdAt' | 'name' | 'isActive' | 'userCount'

export interface PlatformTenantListOptions extends PlatformTenantListFilters {
  page?: number
  pageSize?: number
  sortBy?: PlatformTenantSortField
  sortDir?: 'asc' | 'desc'
}

export interface PlatformTenantListResponse {
  tenants: PlatformTenantListItem[]
  totalCount: number
  page: number
  pageSize: number
}

export interface PlatformTenantRootUser {
  id: number
  email: string
  phone: string
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
  paymentId: number | null
  paymentReferenceNumber: string | null
  cancelledAt: string | null
  cancellationReason: string | null
}

export interface PlatformTenantDetail {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  userCount: number
  logCount: number
  rootUser: PlatformTenantRootUser | null
  currentSubscription: PlatformSubscription | null
  subscriptionHistory: PlatformSubscription[]
  canDelete: boolean
  deleteBlockedReason: string | null
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
  activeTenantCount: number
}

export interface PlatformPackageFormData {
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
  isAvailable: boolean
  sortOrder: number
}

export const PACKAGE_LOG_LEVEL_OPTIONS = [
  { value: 'INFO', label: 'Info' },
  { value: 'WARNING', label: 'Warning' },
  { value: 'ERROR', label: 'Error' },
] as const

export type PackageLogLevelValue = (typeof PACKAGE_LOG_LEVEL_OPTIONS)[number]['value']

function isPackageLogLevel(level: string): level is PackageLogLevelValue {
  return PACKAGE_LOG_LEVEL_OPTIONS.some((option) => option.value === level)
}

export function parsePackageLogLevels(value: string): PackageLogLevelValue[] {
  return value
    .split(',')
    .map((level) => level.trim().toUpperCase())
    .filter(isPackageLogLevel)
}

export function serializePackageLogLevels(levels: string[]): string {
  const order: PackageLogLevelValue[] = ['INFO', 'WARNING', 'ERROR']
  return [...new Set(levels.map((level) => level.trim().toUpperCase()))]
    .filter(isPackageLogLevel)
    .sort((left, right) => order.indexOf(left) - order.indexOf(right))
    .join(',')
}

export function formatPackageLogLevels(value: string): string {
  const labels = new Map<string, string>(
    PACKAGE_LOG_LEVEL_OPTIONS.map((option) => [option.value, option.label]),
  )
  return parsePackageLogLevels(value)
    .map((level) => labels.get(level) ?? level)
    .join(', ')
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
  paymentId?: number | null
}

export const PaymentStatus = {
  Pending: 0,
  Confirmed: 1,
  Rejected: 2,
} as const

export type PaymentStatusValue = (typeof PaymentStatus)[keyof typeof PaymentStatus]

export interface PlatformPayment {
  id: number
  tenantId: number
  tenantName: string
  packageId: number
  packageName: string
  amount: number
  currency: string
  method: string
  referenceNumber: string
  status: PaymentStatusValue
  billingCycle: BillingCycleValue
  periodStart: string
  periodEnd: string
  notes: string
  createdAt: string
  confirmedAt: string | null
  linkedSubscriptionId: number | null
}

export interface PlatformPaymentListResponse {
  payments: PlatformPayment[]
}

export interface RecordPlatformPaymentRequest {
  tenantId: number
  packageId: number
  billingCycle: BillingCycleValue
  amount: number
  referenceNumber: string
  periodStart: string
  notes?: string
}

export interface ImpersonateTenantTicket {
  ticket: string
  expiresInSeconds: number
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

async function authenticatedFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = getAccessToken()
  if (!token) {
    notifyUnauthorizedSession()
    throw new ApiError('Oturum gerekli.', 401)
  }

  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await apiFetch(url, { ...options, headers })
  if (response.status === 401) notifyUnauthorizedSession()
  else if (response.ok) recordSessionActivity()

  return response
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

export function packagePriceForCycle(pkg: PlatformPackage, cycle: BillingCycleValue): number {
  switch (cycle) {
    case BillingCycle.Annual:
      return pkg.priceAnnual
    case BillingCycle.SemiAnnual:
      return pkg.priceSemiAnnual
    case BillingCycle.Quarterly:
      return pkg.priceQuarterly
    case BillingCycle.Monthly:
    default:
      return pkg.priceMonthly
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

export async function platformRefreshSession(): Promise<PlatformLoginResponse> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/auth/refresh`, {
    method: 'POST',
  })

  return parseResult<PlatformLoginResponse>(response)
}

export async function getPlatformTenants(
  options: PlatformTenantListOptions = {},
): Promise<PlatformTenantListResponse> {
  const params = new URLSearchParams()

  if (options.name?.trim()) params.set('name', options.name.trim())
  if (options.rootEmail?.trim()) params.set('rootEmail', options.rootEmail.trim())
  if (options.rootPhone?.trim()) params.set('rootPhone', options.rootPhone.trim())
  if (options.packageCode?.trim()) params.set('packageCode', options.packageCode.trim())
  if (options.isActive !== undefined) params.set('isActive', String(options.isActive))
  if (options.subscriptionStartFrom) params.set('subscriptionStartFrom', options.subscriptionStartFrom)
  if (options.subscriptionStartTo) params.set('subscriptionStartTo', options.subscriptionStartTo)
  if (options.page) params.set('page', String(options.page))
  if (options.pageSize) params.set('pageSize', String(options.pageSize))
  if (options.sortBy) params.set('sortBy', options.sortBy)
  if (options.sortDir) params.set('sortDir', options.sortDir)

  const query = params.toString()
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants${query ? `?${query}` : ''}`,
  )

  return parseResult<PlatformTenantListResponse>(response)
}

export async function exportPlatformTenants(
  options: Omit<PlatformTenantListOptions, 'page' | 'pageSize'> = {},
): Promise<Blob> {
  const params = new URLSearchParams()

  if (options.name?.trim()) params.set('name', options.name.trim())
  if (options.rootEmail?.trim()) params.set('rootEmail', options.rootEmail.trim())
  if (options.rootPhone?.trim()) params.set('rootPhone', options.rootPhone.trim())
  if (options.packageCode?.trim()) params.set('packageCode', options.packageCode.trim())
  if (options.isActive !== undefined) params.set('isActive', String(options.isActive))
  if (options.subscriptionStartFrom) params.set('subscriptionStartFrom', options.subscriptionStartFrom)
  if (options.subscriptionStartTo) params.set('subscriptionStartTo', options.subscriptionStartTo)
  if (options.sortBy) params.set('sortBy', options.sortBy)
  if (options.sortDir) params.set('sortDir', options.sortDir)

  const query = params.toString()
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants/export${query ? `?${query}` : ''}`,
  )

  if (!response.ok) {
    await parseResult<unknown>(response)
    throw new Error('CSV dışa aktarımı başarısız.')
  }

  return response.blob()
}

export async function setTenantStatus(tenantId: number, isActive: boolean): Promise<void> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/tenants/${tenantId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })

  await parseResult<boolean>(response)
}

export async function resetTenantRootPassword(
  tenantId: number,
): Promise<{ temporaryPassword: string }> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants/${tenantId}/root-password/reset`,
    { method: 'POST' },
  )

  return parseResult<{ temporaryPassword: string }>(response)
}

export async function getPlatformTenantDetail(
  tenantId: number,
): Promise<PlatformTenantDetail> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/tenants/${tenantId}`)

  return parseResult<PlatformTenantDetail>(response)
}

export async function getPlatformPackages(): Promise<PlatformPackageListResponse> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/packages`)

  return parseResult<PlatformPackageListResponse>(response)
}

export async function createPlatformPackage(
  request: PlatformPackageFormData,
): Promise<PlatformPackage> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/packages`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return parseResult<PlatformPackage>(response)
}

export async function updatePlatformPackage(
  packageId: number,
  request: Omit<PlatformPackageFormData, 'code'>,
): Promise<PlatformPackage> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/packages/${packageId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })

  return parseResult<PlatformPackage>(response)
}

export async function deletePlatformPackage(packageId: number): Promise<void> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/packages/${packageId}`, {
    method: 'DELETE',
  })

  await parseResult<boolean>(response)
}

export async function assignTenantSubscription(
  tenantId: number,
  request: AssignTenantSubscriptionRequest,
): Promise<PlatformSubscription> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/tenants/${tenantId}/subscription`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  const data = await parseResult<{ subscription: PlatformSubscription }>(response)
  return data.subscription
}

export async function linkSubscriptionPayment(
  tenantId: number,
  paymentId: number,
  subscriptionId?: number,
): Promise<PlatformSubscription> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants/${tenantId}/subscription/link-payment`,
    {
      method: 'POST',
      body: JSON.stringify({ paymentId, subscriptionId: subscriptionId ?? null }),
    },
  )

  const data = await parseResult<{ subscription: PlatformSubscription }>(response)
  return data.subscription
}

export async function impersonateTenant(tenantId: number): Promise<ImpersonateTenantTicket> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/tenants/${tenantId}/impersonate`, {
    method: 'POST',
  })

  return parseResult<ImpersonateTenantTicket>(response)
}

export function paymentStatusLabel(status: PaymentStatusValue): string {
  switch (status) {
    case PaymentStatus.Pending:
      return 'Bekliyor'
    case PaymentStatus.Confirmed:
      return 'Onaylandı'
    case PaymentStatus.Rejected:
      return 'Reddedildi'
    default:
      return 'Bilinmiyor'
  }
}

export async function getPlatformPayments(
  status?: PaymentStatusValue,
): Promise<PlatformPaymentListResponse> {
  const query = status !== undefined ? `?status=${status}` : ''
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments${query}`)
  return parseResult<PlatformPaymentListResponse>(response)
}

export async function getTenantAvailablePayments(
  tenantId: number,
): Promise<PlatformPaymentListResponse> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/payments/tenant/${tenantId}/available`,
  )
  return parseResult<PlatformPaymentListResponse>(response)
}

export async function getTenantPayments(tenantId: number): Promise<PlatformPaymentListResponse> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/payments/tenant/${tenantId}/history`,
  )
  return parseResult<PlatformPaymentListResponse>(response)
}

export async function recordPlatformPayment(
  request: RecordPlatformPaymentRequest,
): Promise<PlatformPayment> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
  return parseResult<PlatformPayment>(response)
}

export async function confirmPlatformPayment(paymentId: number): Promise<PlatformPayment> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments/${paymentId}/confirm`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
  return parseResult<PlatformPayment>(response)
}

export async function rejectPlatformPayment(paymentId: number): Promise<PlatformPayment> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments/${paymentId}/reject`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
  return parseResult<PlatformPayment>(response)
}

export interface UpdatePlatformPaymentRequest {
  packageId: number
  billingCycle: BillingCycleValue
  amount: number
  referenceNumber: string
  periodStart: string
  notes?: string
}

export async function updatePlatformPayment(
  paymentId: number,
  request: UpdatePlatformPaymentRequest,
): Promise<PlatformPayment> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments/${paymentId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
  return parseResult<PlatformPayment>(response)
}

export async function deletePlatformPayment(paymentId: number): Promise<void> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/payments/${paymentId}`, {
    method: 'DELETE',
  })
  await parseResult<boolean>(response)
}

export async function cancelTenantSubscription(
  tenantId: number,
  reason: string,
): Promise<PlatformSubscription> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants/${tenantId}/subscription/cancel`,
    {
      method: 'POST',
      body: JSON.stringify({ reason }),
    },
  )

  const data = await parseResult<{ subscription: PlatformSubscription }>(response)
  return data.subscription
}

export async function removeTenantSubscription(
  tenantId: number,
  subscriptionId: number,
): Promise<PlatformSubscription | null> {
  const response = await authenticatedFetch(
    `${API_URL}/api/platform/tenants/${tenantId}/subscriptions/${subscriptionId}`,
    {
      method: 'DELETE',
    },
  )

  const data = await parseResult<{ currentSubscription: PlatformSubscription | null }>(response)
  return data.currentSubscription
}

export async function deletePlatformTenant(tenantId: number): Promise<void> {
  const response = await authenticatedFetch(`${API_URL}/api/platform/tenants/${tenantId}`, {
    method: 'DELETE',
  })
  await parseResult<boolean>(response)
}

export const PAYMENT_GRACE_DAY_OPTIONS = [7, 10, 15, 21, 30] as const
