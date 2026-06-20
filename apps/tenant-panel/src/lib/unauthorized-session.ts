import { ApiError } from '@/lib/api'

type UnauthorizedHandler = () => void

let unauthorizedHandler: UnauthorizedHandler | null = null
let unauthorizedHandled = false

export function registerUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler
}

export function notifyUnauthorizedSession(): void {
  if (unauthorizedHandled || !unauthorizedHandler) return

  unauthorizedHandled = true
  unauthorizedHandler()

  window.setTimeout(() => {
    unauthorizedHandled = false
  }, 1_000)
}

export function isUnauthorizedError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}
