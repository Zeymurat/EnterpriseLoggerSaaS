export interface ApiResult<T> {
  data: T | null
  isSuccess: boolean
  errorMessage: string | null
  errorKind?: number
}

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

async function parseResult<T>(response: Response): Promise<T> {
  const body = (await response.json()) as ApiResult<T>

  if (!response.ok || !body.isSuccess || body.data === null) {
    throw new ApiError(body.errorMessage ?? 'İstek başarısız.', response.status)
  }

  return body.data
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  return parseResult<LoginResponse>(response)
}

export function getApiUrl(): string {
  return API_URL
}
