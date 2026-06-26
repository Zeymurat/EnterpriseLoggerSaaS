function readPositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

export const SESSION_IDLE_MINUTES = readPositiveInt(
  import.meta.env.VITE_SESSION_IDLE_MINUTES,
  15,
)

export const SESSION_WARN_MINUTES = readPositiveInt(
  import.meta.env.VITE_SESSION_WARN_MINUTES,
  2,
)

export const SESSION_IDLE_MS = SESSION_IDLE_MINUTES * 60 * 1000
export const SESSION_WARN_MS = SESSION_WARN_MINUTES * 60 * 1000

export function formatSessionDuration(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60

  if (minutes <= 0) return `${seconds} sn`
  if (seconds === 0) return `${minutes} dk`
  return `${minutes} dk ${seconds} sn`
}
