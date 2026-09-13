import { useEffect, useState } from 'react'
import './reviewer.css'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
type Item = { id: string; userId: string; type: string; status: string; requestedAt: string; claimedByUserId?: string; claimedAt?: string }

async function api(path: string, options: RequestInit = {}) {
  const token = sessionStorage.getItem('milansetu_access_token')
  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(options.headers ?? {}) } })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? 'Request failed.')
  return data
}

export default function ReviewerPanel() {
  const [allowed, setAllowed] = useState(false)
  const [items, setItems] = useState<Item[]>([])
  const [selected, setSelected] = useState<Item | null>(null)
  const [notes, setNotes] = useState('')
  const [filter, setFilter] = useState('Pending')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  const load = async () => {
    try {
      const me = await api('/api/auth/me')
      if (me.role !== 'Reviewer' && me.role !== 'Admin') return setAllowed(false)
      setAllowed(true)
      const query = filter ? `?status=${encodeURIComponent(filter)}&page=1&pageSize=50` : '?page=1&pageSize=50'
      const result = await api(`/api/reviewer/verifications${query}`)
      setItems(result.items ?? [])
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load reviewer queue.') }
  }

  useEffect(() => { void load() }, [filter])
  if (!allowed) return null

  const review = async (action: 'approve' | 'reject') => {
    if (!selected) return
    if (action === 'reject' && !notes.trim()) { setMessage('A rejection reason is required.'); return }
    setBusy(true); setMessage('')
    try { await api(`/api/reviewer/verifications/${selected.id}/${action}`, { method: 'POST', body: JSON.stringify({ notes }) }); setSelected(null); setNotes(''); await load() }
    catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to complete review.') }
    finally { setBusy(false) }
  }

  return <section className="reviewer-panel" aria-label="Verification review queue"><div className="reviewer-head"><div><p className="eyebrow">REVIEWER WORKSPACE</p><h2>Verification review</h2><p>Identity, education and employment requests. Private documents are not exposed by this queue.</p></div><select value={filter} onChange={e => setFilter(e.target.value)}><option>Pending</option><option>Verified</option><option>Rejected</option></select></div>{message && <p className="reviewer-message" role="status">{message}</p>}<div className="reviewer-grid"><div className="reviewer-list">{items.length === 0 ? <p>No verification requests in this queue.</p> : items.map(item => <button key={item.id} className={`reviewer-item ${selected?.id === item.id ? 'selected' : ''}`} onClick={() => { setSelected(item); setNotes('') }}><strong>{item.type}</strong><span>{item.status}</span><small>{new Date(item.requestedAt).toLocaleString()}</small></button>)}</div>{selected && <div className="reviewer-detail"><p className="eyebrow">REQUEST</p><h3>{selected.type} verification</h3><p>Status: <strong>{selected.status}</strong></p><p className="reviewer-id">Request: {selected.id}</p><p className="reviewer-id">User: {selected.userId}</p><textarea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} placeholder="Review notes / rejection reason" aria-label="Reviewer notes"/><div className="reviewer-actions"><button className="secondary-button" disabled={busy} onClick={() => review('reject')}>Reject</button><button className="primary-button" disabled={busy} onClick={() => review('approve')}>Approve</button></div></div>}</div></section>
}
