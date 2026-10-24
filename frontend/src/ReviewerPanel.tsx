import { useEffect, useState } from 'react'
import './reviewer.css'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const apiBaseUrl = configuredApiBaseUrl ? configuredApiBaseUrl.replace(/\/$/, '') : ''
type Item = { id: string; userId: string; type: string; status: string; requestedAt: string; reviewedAt?: string; reviewedByUserId?: string; claimedByUserId?: string; claimedAt?: string }
type Detail = Item & { verifiedAt?: string; reviewerNotes?: string }
type VerificationDocument = { id: string; originalFileName: string; contentType: string; sizeBytes: number; sha256: string; uploadedAt: string; scannedAt?: string; scannerVerdict?: string }

async function api(path: string, options: RequestInit = {}) {
  if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.')
  const token = sessionStorage.getItem('milansetu_access_token')
  if (!token) throw new Error('Your session has expired. Please sign in again.')
  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...(options.headers ?? {}) } })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? data.error ?? 'Request failed.')
  return data
}

export default function ReviewerPanel() {
  const [allowed, setAllowed] = useState(false)
  const [items, setItems] = useState<Item[]>([])
  const [selected, setSelected] = useState<Detail | null>(null)
  const [documents, setDocuments] = useState<VerificationDocument[]>([])
  const [notes, setNotes] = useState('')
  const [filter, setFilter] = useState('Pending')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const me = await api('/api/auth/me')
      if (!['Reviewer', 'Admin', 'SuperAdmin'].includes(me.role)) { setAllowed(false); return }
      setAllowed(true)
      const query = filter ? `?status=${encodeURIComponent(filter)}&page=1&pageSize=50` : '?page=1&pageSize=50'
      const result = await api(`/api/reviewer/verifications${query}`)
      setItems(result.items ?? [])
      if (selected && !(result.items ?? []).some((item: Item) => item.id === selected.id)) {
        setSelected(null)
        setDocuments([])
      }
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load reviewer queue.') }
    finally { setLoading(false) }
  }

  useEffect(() => {
    const sync = () => { if (sessionStorage.getItem('milansetu_access_token')) void load(); else { setAllowed(false); setSelected(null); setDocuments([]) } }
    sync()
    window.addEventListener('milansetu:auth-changed', sync)
    return () => window.removeEventListener('milansetu:auth-changed', sync)
  }, [filter])

  if (!allowed) return null

  const selectItem = async (item: Item) => {
    setSelected(item); setDocuments([]); setNotes(item.reviewedAt ? '' : '')
    setMessage('')
    try {
      const detail = await api(`/api/reviewer/verifications/${item.id}`)
      setSelected(detail)
      setNotes(detail.reviewerNotes ?? '')
      const result = await api(`/api/reviewer/verifications/${item.id}/documents`)
      setDocuments(result ?? [])
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to load verification details or scanned documents.') }
  }

  const downloadDocument = async (documentItem: VerificationDocument) => {
    if (!selected) return
    const token = sessionStorage.getItem('milansetu_access_token')
    if (!token || !apiBaseUrl) { setMessage('Your session has expired. Please sign in again.'); return }
    setBusy(true); setMessage('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/reviewer/verifications/${selected.id}/documents/${documentItem.id}`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: 'no-store'
      })
      if (!response.ok) {
        const data = await response.json().catch(() => ({}))
        throw new Error(data.message ?? data.error ?? 'Unable to download the verification document.')
      }
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const link = window.document.createElement('a')
      link.href = url
      link.download = documentItem.originalFileName
      window.document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to download the verification document.') }
    finally { setBusy(false) }
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
      setSelected(null); setDocuments([]); setNotes(''); await load()
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to complete review.') }
    finally { setBusy(false) }
  }

  return <section className="reviewer-panel" aria-label="Verification review queue"><div className="reviewer-head"><div><p className="eyebrow">REVIEWER WORKSPACE</p><h2>Verification review</h2><p>Identity, education and employment requests. Only documents cleared by malware scanning are available for review.</p></div><select value={filter} onChange={e => setFilter(e.target.value)} aria-label="Verification status filter"><option>Pending</option><option>Verified</option><option>Rejected</option></select></div>{message && <p className="reviewer-message" role="status">{message}</p>}<div className="reviewer-grid"><div className="reviewer-list">{loading ? <p role="status">Loading verification queue…</p> : items.length === 0 ? <p>No verification requests in this queue.</p> : items.map(item => <button key={item.id} className={`reviewer-item ${selected?.id === item.id ? 'selected' : ''}`} onClick={() => void selectItem(item)}><strong>{item.type}</strong><span>{item.status}</span><small>{new Date(item.requestedAt).toLocaleString()}</small>{item.claimedByUserId && <small>Claimed by reviewer</small>}</button>)}</div>{selected && <div className="reviewer-detail"><p className="eyebrow">REQUEST</p><h3>{selected.type} verification</h3><p>Status: <strong>{selected.status}</strong></p><p className="reviewer-id">Request: {selected.id}</p><p className="reviewer-id">User: {selected.userId}</p>{selected.status === 'Pending' && selected.claimedByUserId && <p className="reviewer-claim">Claimed for review</p>}<section className="reviewer-documents" aria-label="Scanned verification documents"><h4>Scanned documents</h4>{documents.length === 0 ? <p>No clean, scanned documents are available. Files in quarantine are hidden.</p> : <ul>{documents.map(documentItem => <li key={documentItem.id}><div><strong>{documentItem.originalFileName}</strong><small>{Math.ceil(documentItem.sizeBytes / 1024)} KB · {documentItem.scannerVerdict ?? 'Clean scan'}</small></div><button type="button" className="secondary-button" disabled={busy} onClick={() => void downloadDocument(documentItem)}>Download</button></li>)}</ul>}</section><textarea value={notes} onChange={e => setNotes(e.target.value)} maxLength={2000} placeholder="Review notes / rejection reason" aria-label="Reviewer notes" disabled={selected.status !== 'Pending'}/><div className="reviewer-actions">{selected.status === 'Pending' && <>{(!selected.claimedByUserId || selected.claimedByUserId === items.find(x => x.id === selected.id)?.claimedByUserId) && !selected.claimedAt && <button className="secondary-button" disabled={busy} onClick={() => void claim()}>Claim</button>}<button className="secondary-button" disabled={busy} onClick={() => void review('reject')}>Reject</button><button className="primary-button" disabled={busy} onClick={() => void review('approve')}>Approve</button></>}</div></div>}</div></section>
}
