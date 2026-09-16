import { useEffect, useState } from 'react'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request(path: string, options: RequestInit = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...authHeaders(), ...(options.headers ?? {}) },
  })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Request failed.')
  return data
}

type IncomingInterest = { id: string; senderUserId: string; createdAt: string }

export default function IncomingInterestsPanel() {
  const [items, setItems] = useState<IncomingInterest[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)

  async function load() {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    setLoading(true)
    setError('')
    try { setItems(await request('/api/interests/incoming') as IncomingInterest[]) }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to load interests.') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load(); const id = window.setInterval(() => void load(), 10000); return () => window.clearInterval(id) }, [])

  async function respond(id: string, action: 'accept' | 'decline') {
    setBusyId(id); setError('')
    try {
      await request(`/api/interests/${id}/${action}`, { method: 'POST' })
      setItems(current => current.filter(item => item.id !== id))
      window.dispatchEvent(new Event('milansetu:connections-changed'))
      window.dispatchEvent(new Event('milansetu:notifications-changed'))
    } catch (e) { setError(e instanceof Error ? e.message : `Unable to ${action} interest.`) }
    finally { setBusyId(null) }
  }

  return <section style={{ marginTop: 16, padding: 16, border: '1px solid #ddd', borderRadius: 12 }}>
    <h2>Incoming Interests</h2>
    {loading && items.length === 0 ? <p>Loading…</p> : null}
    {error ? <p role="alert">{error}</p> : null}
    {!loading && items.length === 0 && !error ? <p>No pending interests.</p> : null}
    {items.map(item => <article key={item.id} style={{ padding: 12, marginTop: 10, border: '1px solid #eee', borderRadius: 8 }}>
      <div><strong>Member</strong>: {item.senderUserId}</div>
      <small>Received {new Date(item.createdAt).toLocaleString()}</small>
      <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
        <button disabled={busyId === item.id} onClick={() => void respond(item.id, 'accept')}>Accept</button>
        <button disabled={busyId === item.id} onClick={() => void respond(item.id, 'decline')}>Decline</button>
      </div>
    </article>)}
  </section>
}
