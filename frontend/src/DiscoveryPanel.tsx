import { useCallback, useEffect, useMemo, useState } from 'react'
import ReportDialog from './ReportDialog'
import { blockUser, discover, expressInterest, isSignedIn, unblockUser, type DiscoveryProfile } from './discoveryApi'

function ageFromDob(value: string) {
  const dob = new Date(`${value}T00:00:00Z`)
  const now = new Date()
  let age = now.getUTCFullYear() - dob.getUTCFullYear()
  const beforeBirthday = now.getUTCMonth() < dob.getUTCMonth() || (now.getUTCMonth() === dob.getUTCMonth() && now.getUTCDate() < dob.getUTCDate())
  if (beforeBirthday) age -= 1
  return age
}

export default function DiscoveryPanel() {
  const [items, setItems] = useState<DiscoveryProfile[]>([])
  const [total, setTotal] = useState(0)
  const [city, setCity] = useState('')
  const [minAge, setMinAge] = useState(18)
  const [maxAge, setMaxAge] = useState(70)
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<DiscoveryProfile | null>(null)
  const [reportingUserId, setReportingUserId] = useState<string | null>(null)
  const [blocked, setBlocked] = useState<Set<string>>(new Set())
  const [interestStatus, setInterestStatus] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const signedIn = isSignedIn()
  const pageSize = 12

  const load = useCallback(async () => {
    if (!isSignedIn()) return
    setLoading(true); setError('')
    try {
      const result = await discover({ minAge, maxAge, city, page, pageSize })
      setItems(result.items); setTotal(result.total)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load profiles.')
    } finally { setLoading(false) }
  }, [city, maxAge, minAge, page])

  useEffect(() => { void load() }, [load])

  const visibleItems = useMemo(() => items.filter(item => !blocked.has(item.userId)), [blocked, items])
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  const interest = async (profile: DiscoveryProfile) => {
    setError('')
    try {
      const result = await expressInterest(profile.id)
      setInterestStatus(current => ({ ...current, [profile.id]: result.status ?? 'Pending' }))
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to express interest.') }
  }

  const toggleBlock = async (profile: DiscoveryProfile) => {
    setError('')
    try {
      if (blocked.has(profile.userId)) {
        await unblockUser(profile.userId)
        setBlocked(current => { const next = new Set(current); next.delete(profile.userId); return next })
      } else {
        await blockUser(profile.userId)
        setBlocked(current => new Set(current).add(profile.userId))
        if (selected?.userId === profile.userId) setSelected(null)
      }
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to update the block.') }
  }

  if (!signedIn) return <section className="discovery-panel"><div className="discovery-empty"><p className="eyebrow">PRIVATE DISCOVERY</p><h2>Sign in to discover profiles</h2><p>Your discovery results are private and are only available to authenticated members.</p></div></section>

  return <section className="discovery-panel" aria-label="Profile discovery">
    <div className="discovery-head"><div><p className="eyebrow">DISCOVER</p><h2>People looking for a meaningful connection</h2><p>Only profile information needed for discovery is shown. Phone numbers, email addresses and exact addresses stay private.</p></div><button className="secondary-button" onClick={() => void load()} disabled={loading}>Refresh</button></div>
    <div className="discovery-filters">
      <label>City<input value={city} onChange={e => { setCity(e.target.value); setPage(1) }} placeholder="Any city" maxLength={100}/></label>
      <label>Age from<select value={minAge} onChange={e => { setMinAge(Number(e.target.value)); setPage(1) }}>{[18,21,25,28,30,35,40,50].map(x => <option key={x}>{x}</option>)}</select></label>
      <label>Age to<select value={maxAge} onChange={e => { setMaxAge(Number(e.target.value)); setPage(1) }}>{[25,30,35,40,45,50,60,70].map(x => <option key={x}>{x}</option>)}</select></label>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}
    {loading ? <div className="discovery-empty">Loading profiles…</div> : visibleItems.length === 0 ? <div className="discovery-empty"><h3>No profiles found</h3><p>Try widening the age range or removing the city filter.</p></div> : <div className="discovery-grid">{visibleItems.map(profile => {
      const age = ageFromDob(profile.dateOfBirth)
      return <article className="discovery-card" key={profile.id}>
        <div className="discovery-avatar">{profile.displayName?.[0]?.toUpperCase() ?? '?'}</div>
        <h3>{profile.displayName}, {age}</h3>
        <p>{[profile.city, profile.profession].filter(Boolean).join(' • ') || 'Location and profession not provided'}</p>
        <p className="discovery-meta">{[profile.education, profile.motherTongue, profile.maritalStatus].filter(Boolean).join(' • ')}</p>
        {profile.bio && <p className="discovery-bio">{profile.bio}</p>}
        <div className="discovery-actions"><button className="secondary-button" onClick={() => setSelected(profile)}>View</button><button className="primary-button" onClick={() => void interest(profile)} disabled={Boolean(interestStatus[profile.id])}>{interestStatus[profile.id] ?? 'Interested'}</button></div>
        <div className="discovery-safety-actions"><button className="text-button" onClick={() => void toggleBlock(profile)}>{blocked.has(profile.userId) ? 'Unblock' : 'Block'}</button><button className="text-button danger-link" onClick={() => setReportingUserId(profile.userId)}>Report</button></div>
      </article>
    })}</div>}
    <div className="discovery-pagination"><button className="secondary-button" disabled={page <= 1 || loading} onClick={() => setPage(p => p - 1)}>Previous</button><span>Page {page} of {totalPages}</span><button className="secondary-button" disabled={page >= totalPages || loading} onClick={() => setPage(p => p + 1)}>Next</button></div>
    {selected && <div className="discovery-detail"><div className="discovery-detail-head"><div><p className="eyebrow">PROFILE</p><h3>{selected.displayName}, {ageFromDob(selected.dateOfBirth)}</h3></div><button className="text-button" onClick={() => setSelected(null)}>Close</button></div><p>{[selected.city, selected.profession, selected.education].filter(Boolean).join(' • ')}</p>{selected.bio && <p>{selected.bio}</p>}<p className="discovery-private-note">Contact details are intentionally not displayed. A conversation becomes available only after mutual connection.</p><div className="discovery-actions"><button className="primary-button" onClick={() => void interest(selected)} disabled={Boolean(interestStatus[selected.id])}>{interestStatus[selected.id] ?? 'Express interest'}</button><button className="secondary-button" onClick={() => setReportingUserId(selected.userId)}>Report profile</button></div></div>}
    {reportingUserId && <ReportDialog reportedUserId={reportingUserId} onClose={() => setReportingUserId(null)} />}
  </section>
}
