import { useEffect, useState } from 'react'
import {
  acceptInterest,
  declineInterest,
  getConnections,
  getIncomingInterests,
  type Connection,
  type IncomingInterest,
} from './connectionApi'

type ContactShareRequest = {
  id: string
  recipientUserId?: string
  status: 'Pending' | 'Accepted' | 'Declined' | string
  createdAt?: string
}
type ContactShareRequests = { incoming: ContactShareRequest[]; outgoing: ContactShareRequest[] }
type ContactSharingSettings = { enabled: boolean }

function openMessaging(userId: string) {
  window.dispatchEvent(new CustomEvent('milansetu:open-conversation', { detail: userId }))
}

function authHeaders() {
  return { Authorization: 'Bearer ' + sessionStorage.getItem('milansetu_access_token') }
}

async function readResponse(response: Response) {
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message || 'Request failed.')
  return data
}

export default function ConnectionsPanel() {
  const [connections, setConnections] = useState<Connection[]>([])
  const [incoming, setIncoming] = useState<IncomingInterest[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)
  const [contactSettings, setContactSettings] = useState<ContactSharingSettings | null>(null)
  const [contactRequests, setContactRequests] = useState<ContactShareRequests>({ incoming: [], outgoing: [] })
  const [contacts, setContacts] = useState<Record<string, any>>({})

  const load = async () => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    setLoading(true)
    setError('')
    try {
      const [nextConnections, nextIncoming] = await Promise.all([getConnections(), getIncomingInterests()])
      setConnections(nextConnections)
      setIncoming(nextIncoming)

      const [settingsResponse, requestsResponse] = await Promise.all([
        fetch('/api/contact-sharing/settings', { headers: authHeaders() }),
        fetch('/api/contact-sharing/requests', { headers: authHeaders() }),
      ])
      const [settings, requests] = await Promise.all([
        readResponse(settingsResponse),
        readResponse(requestsResponse),
      ])
      setContactSettings(settings)
      setContactRequests(requests)
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

  const requestContact = async (userId: string) => {
    if (busyId) return
    setBusyId(userId)
    setError('')
    try {
      const response = await fetch('/api/contact-sharing/request/' + userId, {
        method: 'POST',
        headers: authHeaders(),
      })
      await readResponse(response)
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to request contact details.')
    } finally {
      setBusyId(null)
    }
  }

  const loadContact = async (userId: string) => {
    setError('')
    try {
      const response = await fetch('/api/contact-sharing/with/' + userId, { headers: authHeaders() })
      const result = await readResponse(response)
      setContacts(current => ({ ...current, [userId]: result }))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to check shared contact details.')
    }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null

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

    {!loading && connections.length === 0 ? <div className="discovery-empty"><h3>No mutual connections yet</h3><p>When interest is mutual, the connection will appear here.</p></div> : <div className="discovery-grid">{connections.map(connection => {
      const outgoing = contactRequests.outgoing.find(item => item.recipientUserId === connection.otherUserId)
      const contact = contacts[connection.otherUserId]
      return <article className="discovery-card" key={connection.id}>
        <div className="discovery-avatar" aria-hidden="true">✓</div>
        <h3>Mutual connection</h3>
        <p>Connection created {new Date(connection.createdAt).toLocaleDateString()}</p>
        <div className="discovery-actions">
          <button className="primary-button" onClick={() => openMessaging(connection.otherUserId)}>Open messages</button>
          {contactSettings?.enabled && (!outgoing || outgoing.status === 'Declined') && <button className="secondary-button" disabled={busyId !== null} onClick={() => void requestContact(connection.otherUserId)}>{busyId === connection.otherUserId ? 'Sending…' : outgoing?.status === 'Declined' ? 'Request contact again' : 'Request contact'}</button>}
          {contactSettings?.enabled && outgoing?.status === 'Pending' && <p role="status">Contact request sent. Waiting for their response.</p>}
          {contactSettings?.enabled && outgoing?.status === 'Accepted' && <p role="status">Your contact-sharing request was accepted.</p>}
          {contact?.visible ? <div className="contact-details">{contact.phone && <div>Mobile: {contact.phone}</div>}{contact.email && <div>Email: {contact.email}</div>}{contact.whatsApp && <div>WhatsApp: {contact.whatsApp}</div>}</div> : <button className="secondary-button" onClick={() => void loadContact(connection.otherUserId)}>Check shared contact</button>}
        </div>
      </article>
    })}</div>}
  </section>
}
