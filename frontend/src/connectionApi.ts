const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    credentials: 'include',
    ...options,
    headers: { ...authHeaders(), ...(options.headers ?? {}) },
  })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Unable to complete the request.')
  return data as T
}

export type Connection = {
  id: string
  otherUserId: string
  createdAt: string
}

export type IncomingInterest = {
  id: string
  senderUserId: string
  createdAt: string
}

export type OutgoingInterest = {
  id: string
  receiverUserId: string
  status: 'Pending' | 'Accepted' | 'Declined' | 'Cancelled'
  createdAt: string
  respondedAt: string | null
}

export async function getConnections(): Promise<Connection[]> {
  return request<Connection[]>('/api/connections')
}

export async function getIncomingInterests(): Promise<IncomingInterest[]> {
  return request<IncomingInterest[]>('/api/interests/incoming')
}

export async function getOutgoingInterests(): Promise<OutgoingInterest[]> {
  return request<OutgoingInterest[]>('/api/interests/outgoing')
}

export async function sendInterest(profileId: string) {
  return request<{ id: string; status: string }>(`/api/interests/${profileId}`, { method: 'POST' })
}

export async function acceptInterest(id: string) {
  return request<{ status: string; connectionCreated: boolean }>(`/api/interests/${id}/accept`, { method: 'POST' })
}

export async function declineInterest(id: string) {
  return request<{ status: string }>(`/api/interests/${id}/decline`, { method: 'POST' })
}
