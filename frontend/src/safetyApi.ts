export type NotificationItem = {
  id: string
  type: string
  title: string
  body: string
  relatedUserId: string | null
  relatedEntityId: string | null
  createdAt: string
  readAt: string | null
}

export type ReportReason = 'Abuse' | 'Harassment' | 'Scam' | 'Impersonation' | 'InappropriateContent' | 'Other'
export type MyReport = { id: string; reportedUserId: string; reason: ReportReason; status: string; createdAt: string; resolvedAt: string | null }

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()?.replace(/\/$/, '') ?? ''
function requireApiBaseUrl() { if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.') }
function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function getNotifications(unreadOnly = false): Promise<NotificationItem[]> {
  requireApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/api/notifications?unreadOnly=${unreadOnly}&limit=50`, { credentials: 'include', headers: authHeaders() })
  if (!response.ok) throw new Error('Unable to load notifications.')
  return response.json()
}
export async function markNotificationRead(id: string) {
  requireApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/api/notifications/${id}/read`, { method: 'POST', credentials: 'include', headers: authHeaders() })
  if (!response.ok) throw new Error('Unable to update notification.')
}
export async function markAllNotificationsRead() {
  requireApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/api/notifications/read-all`, { method: 'POST', credentials: 'include', headers: authHeaders() })
  if (!response.ok) throw new Error('Unable to update notifications.')
}
export async function submitReport(reportedUserId: string, reason: ReportReason, details?: string) {
  requireApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/api/reports`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json', ...authHeaders() }, body: JSON.stringify({ reportedUserId, reason, details: details?.trim() || null }) })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? 'Unable to submit the report.')
  return data as { id: string; status: string }
}
export async function getMyReports(): Promise<MyReport[]> {
  requireApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}/api/reports/mine`, { credentials: 'include', headers: authHeaders() })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Unable to load your reports.')
  return data as MyReport[]
}
