import { useCallback, useEffect, useMemo, useState } from 'react'
import ReportDialog from './ReportDialog'
import { getOutgoingInterests } from './connectionApi'
import { blockUser, discover, expressInterest, isSignedIn, unblockUser, type DiscoveryProfile } from './discoveryApi'

function ageFromDob(value: string) {
  const dob = new Date(`${value}T00:00:00Z`)
  const now = new Date()
  let age = now.getUTCFullYear() - dob.getUTCFullYear()
  const beforeBirthday = now.getUTCMonth() < dob.getUTCMonth() || (now.getUTCMonth() === dob.getUTCMonth() && now.getUTCDate() < dob.getUTCDate())
  if (beforeBirthday) age -= 1
  return age
}

const PAGE_SIZE = 12
const AGE_OPTIONS = [18, 21, 25, 28, 30, 35, 40, 50, 60, 70]

export default function DiscoveryPanel() {
  const [items, setItems] = useState<DiscoveryProfile[]>([])
  const [total, setTotal] = useState(0)
  const [city, setCity] = useState('')
  const [minAge, setMinAge] = useState(18)
  const [maxAge, setMaxAge] = useState(70)
  const [appliedCity, setAppliedCity] = useState('')
  const [appliedMinAge, setAppliedMinAge] = useState(18)
  const [appliedMaxAge, setAppliedMaxAge] = useState(70)
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<DiscoveryProfile | null>(null)
  const [reportingUserId, setReportingUserId] = useState<string | null>(null)
  const [blocked, setBlocked] = useState<Set<string>>(new Set())
  const [interestStatus, setInterestStatus] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [actionUserId, setActionUserId] = useState<string | null>(null)
  const signedIn = isSignedIn()

  const load = useCallback(async () => {
    if (!isSignedIn()) return
    setLoading(true); setError('')
    try {
      const [result, outgoing] = await Promise.all([
        discover({ minAge: appliedMinAge, maxAge: appliedMaxAge, city: appliedCity, page, pageSize: PAGE_SIZE }),
        getOutgoingInterests(),
      ])
      setItems(result.items); setTotal(result.total)
      const statuses: Record<string, string> = {}
      for (const item of result.items) {
        const interest = outgoing.find(x => x.receiverUserId === item.userId)
        if (interest) statuses[item.id] = interest.status
      }
      setInterestStatus(statuses)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load profiles.')
    } finally { setLoading(false) }
  }, [appliedCity, appliedMaxAge, appliedMinAge, page])

  useEffect(() => { void load() }, [load])

  const visibleItems = useMemo(() => items.filter(item => !blocked.has(item.userId)), [blocked, items])
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const filtersDirty = city.trim() !== appliedCity || minAge !== appliedMinAge || maxAge !== appliedMaxAge

  const applyFilters = () => {
    if (minAge > maxAge) { setError('Minimum age cannot be greater than maximum age.'); return }
    setError(''); setPage(1)
    setAppliedCity(city.trim()); setAppliedMinAge(minAge); setAppliedMaxAge(maxAge)
  }

  const resetFilters = () => {
    setCity(''); setMinAge(18); setMaxAge(70); setAppliedCity(''); setAppliedMinAge(18); setAppliedMaxAge(70); setPage(1); setError('')
  }

  const interest = async (profile: DiscoveryProfile) => {
    setError(''); setActionUserId(profile.userId)
    try {
      const result = await expressInterest(profile.id)
      setInterestStatus(current => ({ ...current, [profile.id]: result.status ?? 'Pending' }))
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to express interest.') }
    finally { setActionUserId(null) }
  }

  const toggleBlock = async (profile: DiscoveryProfile) => {
    setError(''); setActionUserId(profile.userId)
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
    finally { setActionUserId(null) }
  }

  if (!signedIn) return <section className="discovery-panel"><div className="discovery-empty"><p className="eyebrow">PRIVATE DISCOVERY</p><h2>Sign in to discover profiles</h2><p>Your discovery results are private and are only available to authenticated members.</p></div></section>

  return <section className="discovery-panel" aria-label="Profile discovery">
    <div className="discovery-head"><div><p className="eyebrow">DISCOVER</p><h2>People looking for a meaningful connection</h2><p>Only profile information needed for discovery is shown. Phone numbers, email addresses and exact addresses stay private.</p></div><button className="secondary-button" onClick={() => void load()} disabled={loading}>Refresh</button></div>
    <div className="discovery-filters" aria-label="Discovery filters">
      <label>City<input value={city} onChange={e => setCity(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') applyFilters() }} placeholder="Any city" maxLength={100}/></label>
      <label>Age from<select value={minAge} onChange={e => setMinAge(Number(e.target.value))}>{AGE_OPTIONS.filter(x => x <= maxAge).map(x => <option key={x} value={x}>{x}</option>)}</select></label>
      <label>Age to<select value={maxAge} onChange={e => setMaxAge(Number(e.target.value))}>{AGE_OPTIONS.filter(x => x >= minAge).map(x => <option key={x} value={x}>{x}</option>)}</select></label>
      <div className="discovery-filter-actions"><button className="primary-button" onClick={applyFilters} disabled={loading || !filtersDirty}>Apply filters</button><button className="text-button" onClick={resetFilters} disabled={loading || (!filtersDirty && !appliedCity && appliedMinAge === 18 && appliedMaxAge === 70)}>Reset</button></div>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}
    {!loading && total > 0 && <p className="discovery-result-count" aria-live="polite">Showing {((page - 1) * PAGE_SIZE) + 1}–{Math.min(page * PAGE_SIZE, total)} of {total} profiles</p>}
    {loading ? <div className="discovery-empty" role="status"><h3>Finding profiles…</h3><p>Applying your discovery preferences.</p></div> : visibleItems.length === 0 ? <div className="discovery-empty"><h3>{total === 0 ? 'No profiles found' : 'No visible profiles on this page'}</h3><p>{total === 0 ? 'Try widening the age range or removing the city filter.' : 'Some profiles on this page may be hidden because you blocked them.'}</p>{total === 0 && <button className="secondary-button" onClick={resetFilters}>Clear filters</button>}</div> : <div className="discovery-grid">{visibleItems.map(profile => {
      const age = ageFromDob(profile.dateOfBirth)
      const status = interestStatus[profile.id]
      const actionBusy = actionUserId === profile.userId
      return <article className="discovery-card" key={profile.id}>
        <div className="discovery-avatar" aria-hidden="true">{profile.displayName?.[0]?.toUpperCase() ?? '?'}</div>
        <h3>{profile.displayName}, {age}</h3>
        <p>{[profile.city, profile.profession].filter(Boolean).join(' • ') || 'Location and profession not provided'}</p>
        <p className="discovery-meta">{[profile.education, profile.motherTongue, profile.maritalStatus].filter(Boolean).join(' • ')}</p>
        {profile.bio && <p className="discovery-bio">{profile.bio}</p>}
        <div className="discovery-actions"><button className="secondary-button" onClick={() => setSelected(profile)}>View profile</button><button className="primary-button" onClick={() => void interest(profile)} disabled={Boolean(status) || actionBusy}>{actionBusy ? 'Working…' : status ?? 'Interested'}</button></div>
        <div className="discovery-safety-actions"><button className="text-button" disabled={actionBusy} onClick={() => void toggleBlock(profile)}>{blocked.has(profile.userId) ? 'Unblock' : 'Block'}</button><button className="text-button danger-link" disabled={actionBusy} onClick={() => setReportingUserId(profile.userId)}>Report</button></div>
      </article>
    })}</div>}
    <div className="discovery-pagination" aria-label="Discovery pagination"><button className="secondary-button" disabled={page <= 1 || loading} onClick={() => setPage(p => p - 1)}>Previous</button><span aria-live="polite">Page {page} of {totalPages}</span><button className="secondary-button" disabled={page >= totalPages || loading} onClick={() => setPage(p => p + 1)}>Next</button></div>
    {selected && <div className="discovery-detail" role="dialog" aria-label={`${selected.displayName} profile`}><div className="discovery-detail-head"><div><p className="eyebrow">PROFILE</p><h3>{selected.displayName}, {ageFromDob(selected.dateOfBirth)}</h3></div><button className="text-button" onClick={() => setSelected(null)}>Close</button></div><p>{[selected.city, selected.profession, selected.education].filter(Boolean).join(' • ')}</p>{selected.bio && <p>{selected.bio}</p>}<p className="discovery-private-note">Contact details are intentionally not displayed. A conversation becomes available only after mutual connection.</p><div className="discovery-actions"><button className="primary-button" onClick={() => void interest(selected)} disabled={Boolean(interestStatus[selected.id]) || actionUserId === selected.userId}>{interestStatus[selected.id] ?? 'Express interest'}</button><button className="secondary-button" onClick={() => setReportingUserId(selected.userId)}>Report profile</button></div></div>}
    {reportingUserId && <ReportDialog reportedUserId={reportingUserId} onClose={() => setReportingUserId(null)} />}
  </section>
}
