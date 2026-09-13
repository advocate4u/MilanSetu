import { useEffect, useState } from 'react'
import { approveReview, getReview, getReviewQueue, rejectReview, type ReviewItem, type VerificationType } from './reviewerVerificationApi'

const types: VerificationType[] = ['Identity', 'Education', 'Employment']

export default function ReviewerVerificationPanel() {
  const [items, setItems] = useState<ReviewItem[]>([])
  const [selected, setSelected] = useState<any>(null)
  const [type, setType] = useState<VerificationType | ''>('')
  const [notes, setNotes] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [visible, setVisible] = useState(false)

  const load = async () => { try { const result = await getReviewQueue(type || undefined); setItems(result.items); setVisible(true); setError('') } catch (e) { if ((e instanceof Error ? e.message : '').includes('403')) { setVisible(false); return } setError(e instanceof Error ? e.message : 'Unable to load review queue.') } }
  useEffect(() => { if (sessionStorage.getItem('milansetu_access_token')) void load() }, [])
  if (!visible) return null

  const open = async (id: string) => { setBusy(true); try { setSelected(await getReview(id)); setNotes('') } catch (e) { setError(e instanceof Error ? e.message : 'Unable to load review.') } finally { setBusy(false) } }
  const decide = async (approve: boolean) => { if (!selected || (!approve && !notes.trim())) return; setBusy(true); setError(''); try { if (approve) await approveReview(selected.id, notes); else await rejectReview(selected.id, notes); setSelected(null); setNotes(''); await load() } catch (e) { setError(e instanceof Error ? e.message : 'Unable to save decision.') } finally { setBusy(false) } }

  return <section style={{ margin: '24px 0', padding: 24, border: '1px solid #ddd', borderRadius: 16 }} aria-label="Reviewer verification queue"><p className="eyebrow">TRUST OPERATIONS</p><h2>Verification review queue</h2><p>Reviewer-only workflow for identity, education and employment claims. Private documents are not exposed by this queue.</p>{error && <p className="profile-error">{error}</p>}<div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}><select value={type} onChange={e => setType(e.target.value as VerificationType | '')}><option value="">All types</option>{types.map(x => <option key={x}>{x}</option>)}</select><button type="button" className="secondary-button" onClick={() => void load()}>Refresh</button></div><div style={{ marginTop: 16, display: 'grid', gap: 8 }}>{items.map(item => <button key={item.id} type="button" className="secondary-button" style={{ textAlign: 'left' }} onClick={() => void open(item.id)}>{item.type} · {item.status} · requested {new Date(item.requestedAt).toLocaleString()}</button>)}{items.length === 0 && <p>No pending verification requests.</p>}</div>{selected && <div style={{ marginTop: 20, padding: 16, background: '#f7f7f7', borderRadius: 12 }}><h3>{selected.type} review</h3><p><strong>Profile:</strong> {selected.profile?.displayName ?? 'Not completed'}</p><p><strong>Account email:</strong> {selected.user?.email ?? 'Private'}</p><p>Review only the information necessary for this claim. Do not copy or publish private verification data.</p><textarea value={notes} onChange={e => setNotes(e.target.value.slice(0, 2000))} rows={5} maxLength={2000} placeholder="Decision notes / rejection reason" style={{ width: '100%', boxSizing: 'border-box' }} /><div style={{ display: 'flex', gap: 8, marginTop: 10 }}><button type="button" className="primary-button" disabled={busy} onClick={() => void decide(true)}>Approve</button><button type="button" className="secondary-button" disabled={busy || !notes.trim()} onClick={() => void decide(false)}>Reject</button><button type="button" className="secondary-button" disabled={busy} onClick={() => setSelected(null)}>Close</button></div></div>}</section>
}
