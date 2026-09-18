import { useEffect, useState } from 'react'
import {
  acceptInterest,
  declineInterest,
  getConnections,
  getIncomingInterests,
  type Connection,
  type IncomingInterest,
} from './connectionApi'

function openMessaging(userId: string) {
  window.dispatchEvent(new CustomEvent('milansetu:open-conversation', { detail: userId }))
}

export default function ConnectionsPanel() {
  const [connections, setConnections] = useState<Connection[]>([])
  const [incoming, setIncoming] = useState<IncomingInterest[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)
  const [contactSettings, setContactSettings] = useState<any>(null)
  const [contactRequests, setContactRequests] = useState<any>(null)
  const [contacts, setContacts] = useState<Record<string, any>>({})

  const load = async () => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    setLoading(true)
    setError('')
    try {
      const [nextConnections, nextIncoming] = await Promise.all([
        getConnections(),
        getIncomingInterests(),
      ])
      setConnections(nextConnections)
      try { const s=await fetch('/api/contact-sharing/settings',{headers:{Authorization:'Bearer '+sessionStorage.getItem('milansetu_access_token')}}); if(s.ok)setContactSettings(await s.json()); const q=await fetch('/api/contact-sharing/requests',{headers:{Authorization:'Bearer '+sessionStorage.getItem('milansetu_access_token')}}); if(q.ok)setContactRequests(await q.json()) } catch {}
      setIncoming(nextIncoming)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load connections.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    const onChanged = () => void load()
    window.addEventListener('milansetu:connections-changed', onChanged)
    return () => window.removeEventListener('milansetu:connections-changed', onChanged)
  }, [])

  const respond = async (id: string, accept: boolean) => {
    if (busyId) return
    setBusyId(id)
    setError('')
    try {
      if (accept) await acceptInterest(id)
      else await declineInterest(id)
      setIncoming(current => current.filter(item => item.id !== id))
      window.dispatchEvent(new Event('milansetu:connections-changed'))
      window.dispatchEvent(new Event('milansetu:notifications-changed'))
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to respond to the interest.')
    } finally {
      setBusyId(null)
    }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  const requestContact = async (userId: string) => { try { await fetch('/api/contact-sharing/request/'+userId,{method:'POST',headers:{Authorization:'Bearer '+sessionStorage.getItem('milansetu_access_token')}}); await load() } catch(e) { setError(e instanceof Error ? e.message : 'Unable to request contact details.') } }
  const loadContact = async (userId: string) => { try { const r=await fetch('/api/contact-sharing/with/'+userId,{headers:{Authorization:'Bearer '+sessionStorage.getItem('milansetu_access_token')}}); if(r.ok) { const x=await r.json(); setContacts(v=>({...v,[userId]:x})) } } catch {} }

  return <section className="discovery-panel" aria-label="Connections">
    <div className="discovery-head">
      <div><p className="eyebrow">CONNECTIONS</p><h2>Your mutual connections</h2><p>Accepting an incoming interest creates a connection and unlocks private messaging.</p></div>
      <button className="secondary-button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}
    {loading && connections.length === 0 && incoming.length === 0 ? <p role="status" aria-live="polite">Loading connections…</p> : null}

    {incoming.length > 0 && <div className="discovery-section" aria-live="polite">
      <h3>Incoming interests ({incoming.length})</h3>
      <div className="discovery-grid">
        {incoming.map(item => <article className="discovery-card" key={item.id}>
          <div className="discovery-avatar" aria-hidden="true">♥</div>
          <h3>Someone is interested</h3>
          <p>Interest received {new Date(item.createdAt).toLocaleDateString()}</p>
          <div className="discovery-actions">
            <button className="primary-button" disabled={busyId !== null} onClick={() => void respond(item.id, true)}>{busyId === item.id ? 'Connecting…' : 'Accept & connect'}</button>
            <button className="secondary-button" disabled={busyId !== null} onClick={() => void respond(item.id, false)}>{busyId === item.id ? 'Working…' : 'Decline'}</button>
          </div>
        </article>)}
      </div>
    </div>}

    {!loading && connections.length === 0 ? <div className="discovery-empty"><h3>No mutual connections yet</h3><p>When interest is mutual, the connection will appear here.</p></div> : <div className="discovery-grid">{connections.map(connection => <article className="discovery-card" key={connection.id}><div className="discovery-avatar" aria-hidden="true">✓</div><h3>Mutual connection</h3><p>Connection created {new Date(connection.createdAt).toLocaleDateString()}</p><div className="discovery-actions"><button className="primary-button" onClick={() => openMessaging(connection.otherUserId)}>Open messages</button>{contactSettings?.enabled && <button className="secondary-button" onClick={() => void requestContact(connection.otherUserId)}>Request contact</button>}{contacts[connection.otherUserId]?.visible ? <div className="contact-details">{contacts[connection.otherUserId].phone && <div>Mobile: {contacts[connection.otherUserId].phone}</div>}{contacts[connection.otherUserId].email && <div>Email: {contacts[connection.otherUserId].email}</div>}</div> : <button className="secondary-button" onClick={() => void loadContact(connection.otherUserId)}>Check shared contact</button>}</div></article>)}</div>}
  </section>
}
