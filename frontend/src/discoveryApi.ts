export type DiscoveryProfile = {
  id: string
  userId: string
  displayName: string
  dateOfBirth: string
  gender: string
  maritalStatus: string | null
  motherTongue: string | null
  bio: string | null
  city: string | null
  education: string | null
  profession: string | null
}

export type DiscoveryResponse = { page: number; pageSize: number; total: number; items: DiscoveryProfile[] }

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}
async function request(path: string, options: RequestInit = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, { credentials: 'include', ...options, headers: { ...authHeaders(), ...(options.headers ?? {}) } })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Unable to complete the request.')
  return data
}
export function isSignedIn() { return Boolean(sessionStorage.getItem('milansetu_access_token')) }
export async function discover(filters: { minAge?: number; maxAge?: number; city?: string; page?: number; pageSize?: number }) {
  const params = new URLSearchParams()
  if (filters.minAge !== undefined) params.set('minAge', String(filters.minAge))
  if (filters.maxAge !== undefined) params.set('maxAge', String(filters.maxAge))
  if (filters.city?.trim()) params.set('city', filters.city.trim())
  params.set('page', String(filters.page ?? 1)); params.set('pageSize', String(filters.pageSize ?? 12))
  return request(`/api/discovery?${params.toString()}`) as Promise<DiscoveryResponse>
}
export async function expressInterest(profileId: string) { return request(`/api/interests/${profileId}`, { method: 'POST' }) }
export async function blockUser(userId: string) { await request(`/api/block/${userId}`, { method: 'POST' }) }
export async function unblockUser(userId: string) { await request(`/api/block/${userId}`, { method: 'DELETE' }) }
