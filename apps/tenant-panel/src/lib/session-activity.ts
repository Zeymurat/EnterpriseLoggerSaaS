type ActivityListener = () => void

const listeners = new Set<ActivityListener>()

export function registerSessionActivityListener(listener: ActivityListener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function recordSessionActivity(): void {
  listeners.forEach((listener) => listener())
}

const ACTIVITY_EVENTS = ['mousedown', 'keydown', 'scroll', 'touchstart'] as const

export function bindGlobalSessionActivityListeners(onActivity: () => void): () => void {
  const handler = () => onActivity()

  ACTIVITY_EVENTS.forEach((eventName) => {
    window.addEventListener(eventName, handler, { passive: true })
  })

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') onActivity()
  })

  return () => {
    ACTIVITY_EVENTS.forEach((eventName) => {
      window.removeEventListener(eventName, handler)
    })
  }
}
