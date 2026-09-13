import { useCallback, useEffect, useState } from 'react'
import './reviewer.css'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'
type Item = { id: string; userId: string; type: string; status: string; requestedAt: string; claimedByUserId?: string | null }
type Detail = Item & { notes?: string | null; user: { id: string; email: string; phoneNumber?: string | null } }

function authHeaders() {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } : { 'Content-Type': 'application/json' }
}

export default function ReviewerPanel() {
  const [items, setItems] = useState<Item[]>([])
  const [selected, setSelected] = useState<Detail | null>(null)
  const [type, setType] = useState('')
  const [busy, setBusy] = useState(false)
  const [visible, setVisible] = useState(false)
  const [message, setMessage] = useState('')
  const [notes, setNotes] = useState('')

  const load = useCallback(async () => {
    const response = await fetch(`${apiBaseUrl}/api/reviewer/verifications?${type ? `type=${encodeURIComponent(type)}&` : ''}status=Pending&page=1&pageSize=50`, { headers: authHeaders() })
    if (response.status === 403 || response.status === 401) { setVisible(false); return }
    if (!response.ok) { setMessage('Unable to load the reviewer queue.'); return }
    const data = await response.json(); setItems(data.items ?? []); setVisible(true)
  }, [type])

  useEffect(() => { void load() }, [load])

  const open = async (id: string) => {
    setMessage(''); setBusy(true)
    try {
      const response = await fetch(`${apiBaseUrl}/api/reviewer/verifications/${id}`, { headers: authHeaders() })
      if (!response.ok) throw new Error('Unable to open verification.')
      const data = await response.json(); setSelected(data); setNotes(data.notes ?? '')
    } catch (e) { setMessage(e instanceof Error ? e.message : 'Unable to open verification.') }
    finally { setBusy(false) }
  }

  const decide = async (action: 'approve' | 'reject') => {
    if (!selected) return
    if (action === 'reject' && !notes.trim()) { setMessage('A rejection reason is required.'); return }
    setBusy(true); setMessage('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/reviewer/verifications/${selected.id}/${action}`, { method: 'POST', headers: authHeaders(), body: JSON.stringify({ notes: notes.trim() || null }) })
      const data = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(data.message ?? 'Unable to update verification.')
      setSelected(null); setNotes(''); setMessage(`Verification ${action}d successfully.`); await load()
    } catch (e) { setMessage(e instanceof Error ? e.message : 'Unable to update verification.') }
    finally { setBusy(false) }
  }

  if (!visible) return null
  return <section className="reviewer-panel" aria-label="Reviewer verification workspace">
    <div className="reviewer-header"><div><p className="eyebrow">TRUST & SAFETY</p><h2>Verification review</h2><p>Review identity, education and employment requests. Private documents are not exposed by this queue.</p></div><button className="secondary-button" onClick={() => void load()}>Refresh</button></div>
    <div className="reviewer-filters"><label>Type<select value={type} onChange={e => setType(e.target.value)}><option value="">All review types</option><option value="Identity">Identity</option><option value="Education">Education</option><option value="Employment">Employment</option></select></label></div>
    {message && <p className="reviewer-message" role="status">{message}</p>}
    {!selected ? <div className="reviewer-list">{items.length === 0 ? <div className="reviewer-empty">No pending verification requests.</div> : items.map(item => <button className="reviewer-item" key={item.id} onClick={() => void open(item.id)}><span><strong>{item.type}</strong><small>Requested {new Date(item.requestedAt).toLocaleString()}</small></span><span className="reviewer-status">{item.status}</span></button>)}</div> : <div className="reviewer-detail"><button className="text-button" onClick={() => setSelected(null)}>← Back to queue</button><h3>{selected.type} verification</h3><dl><dt>User</dt><dd>{selected.user.email}</dd><dt>User ID</dt><dd>{selected.user.id}</dd><dt>Requested</dt><dd>{new Date(selected.requestedAt).toLocaleString()}</dd></dl><label>Reviewer notes / decision reason<textarea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} rows={5} placeholder="Record the evidence reviewed and the reason for your decision…" /></label><div className="reviewer-actions"><button className="secondary-button" disabled={busy} onClick={() => void decide('reject')}>Reject</button><button className="primary-button" disabled={busy} onClick={() => void decide('approve')}>Approve</button></div><p className="reviewer-privacy">Reviewer actions are authenticated and should be audited. Never copy government ID numbers or other sensitive document contents into notes.</p></div>}
  </section>
}
