import { useCallback, useEffect, useState } from 'react'
import { getBlockedProfiles, getSafety, unblockProfile, type BlockedProfile } from './safetyStatusApi'

type Safety = {
  openReports: number
  blockedProfiles: number
  unreadSafetyNotifications: number
  reportsLast30Days: number
  protections: string[]
}

export default function SafetyPanel() {
  const [safety, setSafety] = useState<Safety | null>(null)
  const [blocked, setBlocked] = useState<BlockedProfile[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [status, profiles] = await Promise.all([getSafety(), getBlockedProfiles()])
      setSafety(status)
      setBlocked(profiles)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load safety information.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
    const onChanged = () => void load()
    window.addEventListener('milansetu:notifications-changed', onChanged)
    return () => window.removeEventListener('milansetu:notifications-changed', onChanged)
  }, [load])

  const unblock = async (profile: BlockedProfile) => {
    if (busyId) return
    setBusyId(profile.userId)
    setError('')
    setSuccess('')
    try {
      await unblockProfile(profile.userId)
      setBlocked(current => current.filter(x => x.userId !== profile.userId))
      setSafety(current => current ? { ...current, blockedProfiles: Math.max(0, current.blockedProfiles - 1) } : current)
      setSuccess(`${profile.displayName} has been unblocked.`)
      window.dispatchEvent(new Event('milansetu:discovery-refresh'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to unblock this profile.')
    } finally {
      setBusyId(null)
    }
  }

  return <section className="profile-editor" aria-label="Safety status">
    <div className="profile-editor-head">
      <div><p className="eyebrow">SAFETY</p><h2>Your safety center</h2><p>Review your protections, blocked profiles and reporting activity.</p></div>
      <button className="secondary-button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {error && <p role="alert">{error}</p>}
    {success && <p role="status">{success}</p>}
    {loading && !safety ? <p role="status">Loading your safety information…</p> : null}
    {safety && <>
      <div className="profile-fields">
        <div><strong>{safety.openReports}</strong><small>Open reports</small></div>
        <div><strong>{safety.blockedProfiles}</strong><small>Blocked profiles</small></div>
        <div><strong>{safety.unreadSafetyNotifications}</strong><small>Unread safety notices</small></div>
        <div><strong>{safety.reportsLast30Days}</strong><small>Reports in 30 days</small></div>
      </div>
      <ul>{safety.protections.map(x => <li key={x}>{x}</li>)}</ul>
      <div className="discovery-section">
        <h3>Blocked profiles</h3>
        <p>These members cannot contact you or appear in your discovery results. You can unblock them at any time.</p>
        {blocked.length === 0 ? <p>No profiles are currently blocked.</p> : <div className="discovery-grid">
          {blocked.map(profile => <article className="discovery-card" key={profile.userId}>
            <div className="profile-card-content">
              <h3>{profile.displayName}</h3>
              <p>Blocked {new Date(profile.blockedAt).toLocaleDateString()}</p>
              <button className="secondary-button" disabled={busyId !== null} onClick={() => void unblock(profile)}>{busyId === profile.userId ? 'Unblocking…' : 'Unblock'}</button>
            </div>
          </article>)}
        </div>}
      </div>
    </>}
  </section>
}
