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

export interface PlatformTenantListItem {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  userCount: number
  logCount: number
}

export interface PlatformTenantListResponse {
  tenants: PlatformTenantListItem[]
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

export function isUnauthorizedError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
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
  const token = getAccessToken()
  if (!token) throw new ApiError('Oturum gerekli.', 401)

  const response = await apiFetch(`${API_URL}/api/platform/tenants`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  })

  return parseResult<PlatformTenantListResponse>(response)
}
