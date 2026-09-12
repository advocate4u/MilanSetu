export type VerificationType = 'Mobile' | 'Email' | 'Identity' | 'Education' | 'Employment'
export type VerificationStatus = 'NotStarted' | 'Pending' | 'Verified' | 'Rejected' | 'Expired'
export type VerificationItem = { type: VerificationType; status: VerificationStatus; requestedAt: string | null; verifiedAt: string | null }
export type VerificationChallengeResponse = { type: VerificationType; status: VerificationStatus; expiresAt: string; resendAfterSeconds: number; message: string }
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
function authHeaders(): Record<string, string> { const token = sessionStorage.getItem('milansetu_access_token'); return token ? { Authorization: `Bearer ${token}` } : {} }
async function request(path: string, options: RequestInit = {}) { const response = await fetch(`${apiBaseUrl}${path}`, { credentials: 'include', ...options, headers: { 'Content-Type': 'application/json', ...authHeaders(), ...(options.headers ?? {}) } }); const data = await response.json().catch(() => null); if (!response.ok) throw new Error(data?.message ?? 'Unable to complete the verification request.'); return data }
export function getVerificationStatus() { return request('/api/verification/me') as Promise<VerificationItem[]> }
export function requestVerification(type: VerificationType) { return request(`/api/verification/${type}/request`, { method: 'POST' }) as Promise<VerificationChallengeResponse | { message: string; type: VerificationType; status: VerificationStatus }> }
export function verifyVerificationCode(type: 'Mobile' | 'Email', code: string) { return request(`/api/verification/${type}/verify`, { method: 'POST', body: JSON.stringify({ code }) }) as Promise<{ type: VerificationType; status: 'Verified'; verifiedAt: string; message: string }> }
