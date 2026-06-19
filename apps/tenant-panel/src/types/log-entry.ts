export interface LogEntry {
  id: number
  tenantId: number
  applicationName: string
  logLevel: string
  message: string
  timestamp: string
  httpMethod?: string | null
  requestPath?: string | null
  statusCode?: number | null
  correlationId?: string | null
  actorIdentifier?: string | null
  exceptionType?: string | null
}

export interface CreateLogRequest {
  applicationName: string
  logLevel: string
  message: string
  httpMethod?: string
  requestPath?: string
  statusCode?: number
  correlationId?: string
  actorIdentifier?: string
  exceptionType?: string
}
