import { getAccessToken } from '@/lib/auth'
import { notifyUnauthorizedSession } from '@/lib/unauthorized-session'
import { recordSessionActivity } from '@/lib/session-activity'
import type { CreateLogRequest, GetLogsParams, LogEntry, LogListResponse } from '@/types/log-entry'

export type { CreateLogRequest, GetLogsParams, LogEntry, LogFilterOptions, LogLevelSummary, LogListResponse } from '@/types/log-entry'

export interface TenantLoginOption {
  tenantName: string
  role: string
  displayName?: string | null
}

export interface ApiResult<T> {
  data: T | null
  isSuccess: boolean
  errorMessage: string | null
  errorKind?: number
  errorCode?: string | null
  tenantOptions?: TenantLoginOption[] | null
}

export const AUTH_ERROR_CODES = {
  ambiguousTenant: 'AmbiguousTenantContext',
} as const

export interface UserInfo {
  id: number
  email: string
  phone: string
  role: string
  tenantId: number
  tenantName: string
  permissions: string[]
}

export interface LoginResponse {
  accessToken: string
  expiresIn: number
  user: UserInfo
}

export interface LoginRequest {
  email: string
  password: string
  tenantName?: string
}

export interface TenantUser {
  id: number
  email: string
  phone: string
  role: string
  isActive: boolean
  permissions: string[]
}

export interface InviteUserRequest {
  email: string
  phone: string
  role: string
  permissions?: string[]
}

export interface InviteUserResponse {
  user: TenantUser
  temporaryPassword: string
}

export interface RegisterTenantRequest {
  name: string
  ownerEmail: string
  ownerPhone: string
  ownerPassword: string
}

export interface TenantRegistration {
  id: number
  name: string
  ownerEmail: string
  ownerPhone: string
  isActive: boolean
  createdAt: string
}

export interface RotateApiKeyResponse {
  apiKey: string
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

export class AmbiguousTenantError extends ApiError {
  constructor(
    message: string,
    public tenantOptions: TenantLoginOption[],
  ) {
    super(message, 400)
    this.name = 'AmbiguousTenantError'
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

  if (!response.ok || !body.isSuccess || body.data === null) {
    throw new ApiError(body.errorMessage ?? 'İstek başarısız.', response.status)
  }

  return body.data
}

async function authFetch(path: string, options: RequestInit = {}): Promise<Response> {
  const token = getAccessToken()
  if (!token) {
    notifyUnauthorizedSession()
    throw new ApiError('Oturum gerekli. Lütfen tekrar giriş yapın.', 401)
  }

  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await apiFetch(`${API_URL}${path}`, { ...options, headers })
  if (response.status === 401) notifyUnauthorizedSession()
  else if (response.ok) recordSessionActivity()

  return response
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiFetch(`${API_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  const body = (await response.json()) as ApiResult<LoginResponse>

  if (
    !response.ok &&
    body.errorCode === AUTH_ERROR_CODES.ambiguousTenant &&
    body.tenantOptions &&
    body.tenantOptions.length > 0
  ) {
    throw new AmbiguousTenantError(
      body.errorMessage ?? 'Birden fazla şirket bulundu. Lütfen seçim yapın.',
      body.tenantOptions,
    )
  }

  if (!response.ok || !body.isSuccess || body.data === null) {
    throw new ApiError(body.errorMessage ?? 'İstek başarısız.', response.status)
  }

  return body.data
}

export async function registerTenant(request: RegisterTenantRequest): Promise<TenantRegistration> {
  const response = await apiFetch(`${API_URL}/api/tenants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  return parseResult<TenantRegistration>(response)
}

export async function consumeImpersonationTicket(ticket: string): Promise<LoginResponse> {
  const response = await apiFetch(`${API_URL}/api/auth/impersonate/${encodeURIComponent(ticket)}`, {
    method: 'POST',
  })

  return parseResult<LoginResponse>(response)
}

export async function refreshSession(): Promise<LoginResponse> {
  const response = await authFetch('/api/auth/refresh', { method: 'POST' })
  return parseResult<LoginResponse>(response)
}

export async function rotateTenantApiKey(): Promise<RotateApiKeyResponse> {
  const response = await authFetch('/api/tenants/me/api-key/rotate', {
    method: 'POST',
  })
  return parseResult<RotateApiKeyResponse>(response)
}

function appendMany(searchParams: URLSearchParams, key: string, values?: string[]) {
  values?.forEach((value) => {
    if (value.trim()) searchParams.append(key, value.trim())
  })
}

function appendManyNumbers(searchParams: URLSearchParams, key: string, values?: number[]) {
  values?.forEach((value) => searchParams.append(key, String(value)))
}

function buildLogsSearchParams(params: GetLogsParams): URLSearchParams {
  const searchParams = new URLSearchParams()

  if (params.page) searchParams.set('page', String(params.page))
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize))
  appendMany(searchParams, 'logLevels', params.logLevels)
  if (params.search?.trim()) searchParams.set('search', params.search.trim())
  if (params.correlationId?.trim()) searchParams.set('correlationId', params.correlationId.trim())
  if (params.from) searchParams.set('from', params.from)
  if (params.to) searchParams.set('to', params.to)
  appendMany(searchParams, 'applicationNames', params.applicationNames)
  appendMany(searchParams, 'httpMethods', params.httpMethods)
  appendManyNumbers(searchParams, 'statusCodes', params.statusCodes)

  return searchParams
}

function parseContentDispositionFileName(header: string | null): string | null {
  if (!header) return null

  const utf8Match = header.match(/filename\*=UTF-8''([^;]+)/i)
  if (utf8Match?.[1]) return decodeURIComponent(utf8Match[1])

  const basicMatch = header.match(/filename="?([^";]+)"?/i)
  return basicMatch?.[1] ?? null
}

export async function getLogs(params: GetLogsParams = {}): Promise<LogListResponse> {
  const query = buildLogsSearchParams(params).toString()
  const path = query ? `/api/logs?${query}` : '/api/logs'
  const response = await authFetch(path)
  const data = await parseResult<LogListResponse>(response)

  return {
    ...data,
    availableFilters: data.availableFilters ?? {
      applicationNames: [],
      httpMethods: [],
      statusCodes: [],
    },
  }
}

export interface LogExportResult {
  blob: Blob
  fileName: string
  exportedCount: number
  totalMatching: number
  truncated: boolean
}

export async function exportLogs(params: GetLogsParams = {}): Promise<LogExportResult> {
  const query = buildLogsSearchParams(params).toString()
  const path = query ? `/api/logs/export?${query}` : '/api/logs/export'
  const response = await authFetch(path)

  if (!response.ok) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('application/json')) {
      const body = (await response.json()) as ApiResult<unknown>
      throw new ApiError(body.errorMessage ?? 'Dışa aktarma başarısız.', response.status)
    }
    throw new ApiError('Dışa aktarma başarısız.', response.status)
  }

  const blob = await response.blob()
  const fileName =
    parseContentDispositionFileName(response.headers.get('Content-Disposition')) ??
    `logs-export-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.csv`

  return {
    blob,
    fileName,
    exportedCount: Number(response.headers.get('X-Export-Count') ?? 0),
    totalMatching: Number(response.headers.get('X-Export-Total-Matching') ?? 0),
    truncated: response.headers.get('X-Export-Truncated') === 'true',
  }
}

export async function createLog(request: CreateLogRequest): Promise<LogEntry> {
  const response = await authFetch('/api/logs', {
    method: 'POST',
    body: JSON.stringify(request),
  })
  return parseResult<LogEntry>(response)
}

export function getApiUrl(): string {
  return API_URL
}

export function isUnauthorizedError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}

export async function getUsers(): Promise<TenantUser[]> {
  const response = await authFetch('/api/users')
  return parseResult<TenantUser[]>(response)
}

export async function inviteUser(request: InviteUserRequest): Promise<InviteUserResponse> {
  const response = await authFetch('/api/users/invite', {
    method: 'POST',
    body: JSON.stringify(request),
  })
  return parseResult<InviteUserResponse>(response)
}

export async function updateUserPermissions(
  userId: number,
  permissions: string[],
): Promise<TenantUser> {
  const response = await authFetch(`/api/users/${userId}/permissions`, {
    method: 'PATCH',
    body: JSON.stringify({ permissions }),
  })
  return parseResult<TenantUser>(response)
}

export async function updateUserRole(userId: number, role: string): Promise<TenantUser> {
  const response = await authFetch(`/api/users/${userId}/role`, {
    method: 'PATCH',
    body: JSON.stringify({ role }),
  })
  return parseResult<TenantUser>(response)
}

export async function deactivateUser(userId: number): Promise<TenantUser> {
  const response = await authFetch(`/api/users/${userId}/deactivate`, {
    method: 'PATCH',
  })
  return parseResult<TenantUser>(response)
}

export interface TenantBillingNotice {
  showNotice: boolean
  packageName: string | null
  paymentDueBy: string | null
  periodEnd: string | null
  message: string | null
  billingCycle: number | null
}

export async function getBillingNotice(): Promise<TenantBillingNotice> {
  const response = await authFetch('/api/billing/notice')
  return parseResult<TenantBillingNotice>(response)
}

export interface TenantUsage {
  packageName: string
  monthlyLogCount: number
  monthlyRequestLimit: number
  logsLastMinute: number
  maxLogsPerMinute: number
}

export async function getTenantUsage(): Promise<TenantUsage | null> {
  const response = await authFetch('/api/billing/usage')

  if (response.status === 400 || response.status === 403) {
    return null
  }

  if (!response.ok) {
    throw new Error('Kota bilgisi alınamadı.')
  }

  return parseResult<TenantUsage>(response)
}

export const PaymentStatus = {
  Pending: 0,
  Confirmed: 1,
  Rejected: 2,
} as const

export type PaymentStatusValue = (typeof PaymentStatus)[keyof typeof PaymentStatus]

export interface TenantPaymentSummary {
  id: number
  amount: number
  currency: string
  status: PaymentStatusValue
  referenceNumber: string
  periodStart: string
  periodEnd: string
  createdAt: string
  confirmedAt: string | null
}

export interface TenantBillingOverview {
  hasActiveSubscription: boolean
  packageName: string | null
  packageCode: string | null
  status: number | null
  billingCycle: number | null
  startDate: string | null
  endDate: string | null
  gracePeriodEndDate: string | null
  isPaid: boolean
  autoRenew: boolean
  storageRetentionDays: number
  monthlyRequestLimit: number
  maxLogsPerMinute: number
  showPaymentNotice: boolean
  paymentNoticeMessage: string | null
  recentPayments: TenantPaymentSummary[]
}

export async function getTenantBillingOverview(): Promise<TenantBillingOverview> {
  const response = await authFetch('/api/billing/overview')
  return parseResult<TenantBillingOverview>(response)
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
