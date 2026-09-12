export type Conversation = { id: string; otherUserId: string; createdAt: string; lastMessageAt: string | null }
export type Message = { id: string; senderUserId: string; body: string; createdAt: string }
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
function authHeaders(): Record<string, string> { const token = sessionStorage.getItem('milansetu_access_token'); return token ? { Authorization: `Bearer ${token}` } : {} }
async function request<T>(path: string, options: RequestInit = {}): Promise<T> { const response = await fetch(`${apiBaseUrl}${path}`, { credentials: 'include', ...options, headers: { ...authHeaders(), ...(options.headers ?? {}) } }); const data = await response.json().catch(() => null); if (!response.ok) throw new Error(data?.message ?? data?.warning ?? 'Unable to complete the request.'); return data as T }
export function getConversations() { return request<Conversation[]>('/api/messages/conversations') }
export function openConversation(otherUserId: string) { return request<{ id: string; otherUserId: string }>(`/api/messages/conversations/${otherUserId}`, { method: 'POST' }) }
export function getMessages(conversationId: string, before?: string) { const params = new URLSearchParams({ limit: '50' }); if (before) params.set('before', before); return request<Message[]>(`/api/messages/conversations/${conversationId}?${params.toString()}`) }
export function sendMessage(conversationId: string, body: string) { return request<Message>(`/api/messages/conversations/${conversationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ body }) }) }
export function deleteMessage(conversationId: string, messageId: string) { return request<void>(`/api/messages/conversations/${conversationId}/${messageId}`, { method: 'DELETE' }) }
