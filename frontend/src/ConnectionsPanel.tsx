import { useEffect, useState } from 'react'
import { getConnections, type Connection } from './connectionApi'

function openMessaging(userId: string) {
  window.dispatchEvent(new CustomEvent('milansetu:open-conversation', { detail: userId }))
}

export default function ConnectionsPanel() {
  const [connections, setConnections] = useState<Connection[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    setLoading(true)
    setError('')
    try {
      setConnections(await getConnections())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load connections.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="discovery-panel" aria-label="Connections">
    <div className="discovery-head">
      <div><p className="eyebrow">CONNECTIONS</p><h2>Your mutual connections</h2><p>Only mutual connections can be opened for private messaging.</p></div>
      <button className="secondary-button" onClick={() => void load()} disabled={loading}>{loading ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}
    {!loading && connections.length === 0 ? <div className="discovery-empty"><h3>No mutual connections yet</h3><p>When interest is mutual, the connection will appear here.</p></div> : <div className="discovery-grid">{connections.map(connection => <article className="discovery-card" key={connection.id}><div className="discovery-avatar">✓</div><h3>Mutual connection</h3><p>Connection created {new Date(connection.createdAt).toLocaleDateString()}</p><p className="discovery-meta">{connection.lastMessageAt ? `Last message ${new Date(connection.lastMessageAt).toLocaleString()}` : 'No messages yet'}</p><div className="discovery-actions"><button className="primary-button" onClick={() => openMessaging(connection.otherUserId)}>Open messages</button></div></article>)}</div>}
  </section>
}
