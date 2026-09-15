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
      setIncoming(nextIncoming)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load connections.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  const respond = async (id: string, accept: boolean) => {
    setError('')
    try {
      if (accept) await acceptInterest(id)
      else await declineInterest(id)
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to respond to the interest.')
    }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="discovery-panel" aria-label="Connections">
    <div className="discovery-head">
      <div><p className="eyebrow">CONNECTIONS</p><h2>Your mutual connections</h2><p>Accepting an incoming interest creates a connection and unlocks private messaging.</p></div>
      <button className="secondary-button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}

    {incoming.length > 0 && <div className="discovery-section">
      <h3>Incoming interests</h3>
      <div className="discovery-grid">
        {incoming.map(item => <article className="discovery-card" key={item.id}>
          <div className="discovery-avatar">♥</div>
          <h3>Someone is interested</h3>
          <p>Interest received {new Date(item.createdAt).toLocaleDateString()}</p>
          <div className="discovery-actions">
            <button className="primary-button" onClick={() => void respond(item.id, true)}>Accept & connect</button>
            <button className="secondary-button" onClick={() => void respond(item.id, false)}>Decline</button>
          </div>
        </article>)}
      </div>
    </div>}

    {!loading && connections.length === 0 ? <div className="discovery-empty"><h3>No mutual connections yet</h3><p>When interest is mutual, the connection will appear here.</p></div> : <div className="discovery-grid">{connections.map(connection => <article className="discovery-card" key={connection.id}><div className="discovery-avatar">✓</div><h3>Mutual connection</h3><p>Connection created {new Date(connection.createdAt).toLocaleDateString()}</p><div className="discovery-actions"><button className="primary-button" onClick={() => openMessaging(connection.otherUserId)}>Open messages</button></div></article>)}</div>}
  </section>
}
