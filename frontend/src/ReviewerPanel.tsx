import { useEffect, useState } from 'react'
import './reviewer.css'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const apiBaseUrl = configuredApiBaseUrl ? configuredApiBaseUrl.replace(/\/$/, '') : ''
type Item = { id: string; userId: string; type: string; status: string; requestedAt: string; reviewedAt?: string; reviewedByUserId?: string; claimedByUserId?: string; claimedAt?: string }
type Detail = Item & { verifiedAt?: string; reviewerNotes?: string }

async function api(path: string, options: RequestInit = {}) {
  if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.')
  const token = sessionStorage.getItem('milansetu_access_token')
  if (!token) throw new Error('Your session has expired. Please sign in again.')
  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(options.headers ?? {}) } })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? data.error ?? 'Request failed.')
  return data
}

export default function ReviewerPanel() {
  const [allowed, setAllowed] = useState(false)
  const [items, setItems] = useState<Item[]>([])
  const [selected, setSelected] = useState<Detail | null>(null)
  const [notes, setNotes] = useState('')
  const [filter, setFilter] = useState('Pending')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const me = await api('/api/auth/me')
      if (me.role !== 'Reviewer' && me.role !== 'Admin') { setAllowed(false); return }
      setAllowed(true)
      const query = filter ? `?status=${encodeURIComponent(filter)}&page=1&pageSize=50` : '?page=1&pageSize=50'
      const result = await api(`/api/reviewer/verifications${query}`)
      setItems(result.items ?? [])
      if (selected && !(result.items ?? []).some((item: Item) => item.id === selected.id)) setSelected(null)
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load reviewer queue.') }
    finally { setLoading(false) }
  }

  useEffect(() => {
    const sync = () => { if (sessionStorage.getItem('milansetu_access_token')) void load(); else { setAllowed(false); setSelected(null) } }
    sync()
    window.addEventListener('milansetu:auth-changed', sync)
    return () => window.removeEventListener('milansetu:auth-changed', sync)
  }, [filter])

  if (!allowed) return null

  const selectItem = async (item: Item) => {
    setSelected(item); setNotes(item.reviewedAt ? '' : '')
    setMessage('')
    try {
      const detail = await api(`/api/reviewer/verifications/${item.id}`)
      setSelected(detail)
      setNotes(detail.reviewerNotes ?? '')
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load verification details.') }
  }

  const claim = async () => {
    if (!selected || selected.status !== 'Pending') return
    setBusy(true); setMessage('')
    try {
      const result = await api(`/api/reviewer/verifications/${selected.id}/claim`, { method: 'POST' })
      setSelected(current => current ? { ...current, claimedByUserId: result.claimedByUserId, claimedAt: result.claimedAt } : current)
      await load()
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to claim verification.') }
    finally { setBusy(false) }
  }

  const review = async (action: 'approve' | 'reject') => {
    if (!selected || selected.status !== 'Pending') return
    if (action === 'reject' && !notes.trim()) { setMessage('A rejection reason is required.'); return }
    setBusy(true); setMessage('')
    try {
      await api(`/api/reviewer/verifications/${selected.id}/${action}`, { method: 'POST', body: JSON.stringify({ notes: notes.trim() || null }) })
      setSelected(null); setNotes(''); await load()
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to complete review.') }
    finally { setBusy(false) }
  }

  return <section className="reviewer-panel" aria-label="Verification review queue"><div className="reviewer-head"><div><p className="eyebrow">REVIEWER WORKSPACE</p><h2>Verification review</h2><p>Identity, education and employment requests. Private documents are not exposed by this queue.</p></div><select value={filter} onChange={e => setFilter(e.target.value)} aria-label="Verification status filter"><option>Pending</option><option>Verified</option><option>Rejected</option></select></div>{message && <p className="reviewer-message" role="status">{message}</p>}<div className="reviewer-grid"><div className="reviewer-list">{loading ? <p role="status">Loading verification queue…</p> : items.length === 0 ? <p>No verification requests in this queue.</p> : items.map(item => <button key={item.id} className={`reviewer-item ${selected?.id === item.id ? 'selected' : ''}`} onClick={() => void selectItem(item)}><strong>{item.type}</strong><span>{item.status}</span><small>{new Date(item.requestedAt).toLocaleString()}</small>{item.claimedByUserId && <small>Claimed by reviewer</small>}</button>)}</div>{selected && <div className="reviewer-detail"><p className="eyebrow">REQUEST</p><h3>{selected.type} verification</h3><p>Status: <strong>{selected.status}</strong></p><p className="reviewer-id">Request: {selected.id}</p><p className="reviewer-id">User: {selected.userId}</p>{selected.status === 'Pending' && selected.claimedByUserId && <p className="reviewer-claim">Claimed for review</p>}<textarea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} placeholder="Review notes / rejection reason" aria-label="Reviewer notes" disabled={selected.status !== 'Pending'}/><div className="reviewer-actions">{selected.status === 'Pending' && !selected.claimedByUserId && <button className="secondary-button" disabled={busy} onClick={() => void claim()}>Claim</button>}{selected.status === 'Pending' && <><button className="secondary-button" disabled={busy} onClick={() => void review('reject')}>Reject</button><button className="primary-button" disabled={busy} onClick={() => void review('approve')}>Approve</button></>}</div></div>}</div></section>
}
