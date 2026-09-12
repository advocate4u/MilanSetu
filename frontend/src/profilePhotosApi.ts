export type ProfilePhoto = { fileName: string; url: string }

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request(path: string, options: RequestInit = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    credentials: 'include',
    ...options,
    headers: { ...authHeaders(), ...(options.headers ?? {}) },
  })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Unable to complete the photo request.')
  return data
}

export function getProfilePhotos() {
  return request('/api/profile/photos') as Promise<ProfilePhoto[]>
}

export function uploadProfilePhoto(file: File) {
  const form = new FormData()
  form.append('file', file)
  return request('/api/profile/photos', { method: 'POST', body: form }) as Promise<ProfilePhoto>
}

export function deleteProfilePhoto(fileName: string) {
  return request(`/api/profile/photos/${encodeURIComponent(fileName)}`, { method: 'DELETE' })
}

export async function loadProfilePhoto(photo: ProfilePhoto) {
  const response = await fetch(`${apiBaseUrl}${photo.url}`, { credentials: 'include', headers: authHeaders() })
  if (!response.ok) throw new Error('Unable to load the private photo.')
  return URL.createObjectURL(await response.blob())
}
