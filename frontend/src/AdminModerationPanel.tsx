import { useEffect, useState } from 'react'
import './admin-moderation.css'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/$/, '') ?? ''
type Case = { id: string; reportId: string; targetUserId: string; severity: string; status: string; action: string; createdAt: string; reason: string; details?: string | null }

async function api(path: string, options: RequestInit = {}) {
  if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.')
  const token = sessionStorage.getItem('milansetu_access_token')
  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(options.headers ?? {}) } })
  const data = await response.json().catch(() => ({}))
  if (response.status === 401) { sessionStorage.removeItem('milansetu_access_token'); window.dispatchEvent(new Event('milansetu:auth-changed')); throw new Error('Your session has expired. Please sign in again.') }
  if (!response.ok) throw new Error(data.message ?? 'Request failed.')
  return data
}

export default function AdminModerationPanel() {
  const [allowed, setAllowed] = useState(false), [cases, setCases] = useState<Case[]>([]), [selected, setSelected] = useState<Case | null>(null)
  const [status, setStatus] = useState('Resolved'), [action, setAction] = useState('Warning'), [notes, setNotes] = useState(''), [message, setMessage] = useState(''), [busy, setBusy] = useState(false), [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const me = await api('/api/auth/me')
      if (me.role !== 'Admin') { setAllowed(false); return }
      setAllowed(true)
      const result = await api('/api/admin/moderation/cases?status=Open&page=1&pageSize=50')
      setCases(result.items ?? [])
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load moderation queue.') }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])
  if (!allowed) return null

  const decide = async () => {
    if (!selected) return
    setBusy(true); setMessage('')
    try {
      await api(`/api/admin/moderation/cases/${selected.id}/decide`, { method: 'POST', body: JSON.stringify({ status, action, notes }) })
      setSelected(null); setNotes(''); await load()
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to update moderation case.') }
    finally { setBusy(false) }
  }

  return <section className="admin-moderation-panel" aria-label="Admin moderation workspace">
    <div className="admin-moderation-head"><div><p className="eyebrow">ADMIN MODERATION</p><h2>Safety cases</h2><p>Review user reports and apply documented moderation actions.</p></div><button className="secondary-button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : 'Refresh'}</button></div>
    {message && <p className="admin-moderation-message" role="status">{message}</p>}
    <div className="admin-moderation-grid"><div className="admin-moderation-list">{cases.length === 0 ? <p>{loading ? 'Loading cases…' : 'No open moderation cases.'}</p> : cases.map(item => <button key={item.id} className={`admin-case ${selected?.id === item.id ? 'selected' : ''}`} onClick={() => setSelected(item)}><strong>{item.reason}</strong><span>{item.severity} · {item.status}</span><small>{new Date(item.createdAt).toLocaleString()}</small></button>)}</div>
    {selected && <div className="admin-moderation-detail"><p className="eyebrow">CASE</p><h3>{selected.reason}</h3><p>Target: <code>{selected.targetUserId}</code></p><p>{selected.details || 'No additional report details.'}</p><label>Status<select value={status} onChange={e => setStatus(e.target.value)}><option value="Resolved">Resolved</option><option value="Dismissed">Dismissed</option></select></label><label>Action<select value={action} onChange={e => setAction(e.target.value)}><option>None</option><option>Warning</option><option>MessagingRestriction</option><option>TemporarySuspension</option><option>PermanentBan</option></select></label><textarea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} placeholder="Moderation notes" aria-label="Moderation notes"/><button className="primary-button" disabled={busy} onClick={() => void decide()}>{busy ? 'Saving…' : 'Apply decision'}</button></div>}</div>
  </section>
}
