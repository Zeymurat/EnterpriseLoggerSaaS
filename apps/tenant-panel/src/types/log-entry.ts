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

export interface LogLevelSummary {
  total: number
  info: number
  warning: number
  error: number
}

export interface LogListResponse {
  items: LogEntry[]
  totalCount: number
  page: number
  pageSize: number
  summary: LogLevelSummary
  availableFilters: LogFilterOptions
  overallSummary?: LogLevelSummary | null
  isDateFiltered?: boolean
}

export interface LogFilterOptions {
  applicationNames: string[]
  httpMethods: string[]
  statusCodes: number[]
}

export interface GetLogsParams {
  page?: number
  pageSize?: number
  logLevels?: string[]
  search?: string
  from?: string
  to?: string
  applicationNames?: string[]
  httpMethods?: string[]
  statusCodes?: number[]
}
