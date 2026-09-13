export type VerificationType = 'Identity' | 'Education' | 'Employment'
export type VerificationStatus = 'NotStarted' | 'Pending' | 'Verified' | 'Rejected' | 'Expired'
export type ReviewItem = { id: string; userId: string; type: VerificationType; status: VerificationStatus; requestedAt: string; reviewedAt: string | null; reviewerNotes: string | null }
const base = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
function headers() { const token = sessionStorage.getItem('milansetu_access_token'); return token ? { Authorization: `Bearer ${token}` } : {} }
async function request<T>(path: string, init?: RequestInit): Promise<T> { const response = await fetch(`${base}${path}`, { ...init, headers: { 'Content-Type': 'application/json', ...headers(), ...(init?.headers ?? {}) } }); if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.message ?? `Request failed (${response.status})`) } return response.status === 204 ? undefined as T : response.json() }
export function getReviewQueue(type?: VerificationType) { return request<{ page: number; pageSize: number; total: number; items: ReviewItem[] }>(`/api/reviewer/verifications${type ? `?type=${type}` : ''}`) }
export function getReview(id: string) { return request<any>(`/api/reviewer/verifications/${id}`) }
export function approveReview(id: string, notes: string) { return request(`/api/reviewer/verifications/${id}/approve`, { method: 'POST', body: JSON.stringify({ notes }) }) }
export function rejectReview(id: string, notes: string) { return request(`/api/reviewer/verifications/${id}/reject`, { method: 'POST', body: JSON.stringify({ notes }) }) }
