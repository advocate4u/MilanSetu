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

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

function authHeaders() {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function getNotifications(unreadOnly = false): Promise<NotificationItem[]> {
  const response = await fetch(`${apiBaseUrl}/api/notifications?unreadOnly=${unreadOnly}&limit=50`, {
    credentials: 'include',
    headers: authHeaders(),
  })
  if (!response.ok) throw new Error('Unable to load notifications.')
  return response.json()
}

export async function markNotificationRead(id: string) {
  const response = await fetch(`${apiBaseUrl}/api/notifications/${id}/read`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeaders(),
  })
  if (!response.ok) throw new Error('Unable to update notification.')
}

export async function markAllNotificationsRead() {
  const response = await fetch(`${apiBaseUrl}/api/notifications/read-all`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeaders(),
  })
  if (!response.ok) throw new Error('Unable to update notifications.')
}

export async function submitReport(reportedUserId: string, reason: ReportReason, details?: string) {
  const response = await fetch(`${apiBaseUrl}/api/reports`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ reportedUserId, reason, details: details?.trim() || null }),
  })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? 'Unable to submit the report.')
  return data as { id: string; status: string }
}
