import { getAccessToken } from '@/lib/auth'
import type { CreateLogRequest, LogEntry } from '@/types/log-entry'

export type { CreateLogRequest, LogEntry } from '@/types/log-entry'

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
    throw new ApiError('Oturum gerekli. Lütfen tekrar giriş yapın.', 401)
  }

  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }

  return fetch(`${API_URL}${path}`, { ...options, headers })
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await fetch(`${API_URL}/api/auth/login`, {
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
  const response = await fetch(`${API_URL}/api/tenants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  return parseResult<TenantRegistration>(response)
}

export async function rotateTenantApiKey(): Promise<RotateApiKeyResponse> {
  const response = await authFetch('/api/tenants/me/api-key/rotate', {
    method: 'POST',
  })
  return parseResult<RotateApiKeyResponse>(response)
}

export async function getLogs(): Promise<LogEntry[]> {
  const response = await authFetch('/api/logs')
  return parseResult<LogEntry[]>(response)
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

export function useMockLogsOnly(): boolean {
  return import.meta.env.VITE_USE_MOCK_LOGS === 'true'
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

export function useMockUsersOnly(): boolean {
  return import.meta.env.VITE_USE_MOCK_USERS === 'true'
}
