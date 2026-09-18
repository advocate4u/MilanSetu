import { useCallback, useEffect, useRef, useState } from 'react'
import { getNotifications, getNotificationSummary, markAllNotificationsRead, markNotificationRead, type NotificationItem } from './safetyApi'

const REFRESH_INTERVAL_MS = 15_000

export default function NotificationPanel() {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<NotificationItem[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)
  const requestId = useRef(0)
  const [serverUnread, setServerUnread] = useState<number | null>(null)
  const unread = serverUnread ?? items.filter(x => !x.readAt).length

  const refresh = useCallback(async () => {
    if (!sessionStorage.getItem('milansetu_access_token')) {
      setItems([])
      return
    }
    const currentRequest = ++requestId.current
    setLoading(true)
    setError('')
    try {
      const [next, summary] = await Promise.all([getNotifications(), getNotificationSummary()])
      if (currentRequest === requestId.current) { setItems(next); setServerUnread(summary.unreadCount) }
    } catch (e) {
      if (currentRequest === requestId.current) setError(e instanceof Error ? e.message : 'Unable to load notifications.')
    } finally {
      if (currentRequest === requestId.current) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
    const onChanged = () => void refresh()
    const onAuthChanged = () => void refresh()
    const onAuthExpired = () => { setItems([]); setOpen(false) }
    window.addEventListener('milansetu:notifications-changed', onChanged)
    window.addEventListener('milansetu:auth-changed', onAuthChanged)
    window.addEventListener('milansetu:auth-expired', onAuthExpired)
    const timer = window.setInterval(() => { if (document.visibilityState === 'visible') void refresh() }, REFRESH_INTERVAL_MS)
    return () => {
      window.clearInterval(timer)
      window.removeEventListener('milansetu:notifications-changed', onChanged)
      window.removeEventListener('milansetu:auth-changed', onAuthChanged)
      window.removeEventListener('milansetu:auth-expired', onAuthExpired)
    }
  }, [refresh])

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => { if (event.key === 'Escape') setOpen(false) }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open])

  const load = async () => {
    setOpen(true)
    await refresh()
  }

  const markRead = async (item: NotificationItem) => {
    if (item.readAt || busyId) return
    setBusyId(item.id)
    setError('')
    try {
      await markNotificationRead(item.id)
      setItems(current => current.map(x => x.id === item.id ? { ...x, readAt: new Date().toISOString() } : x))
      setServerUnread(current => Math.max(0, (current ?? unread) - 1))
      window.dispatchEvent(new Event('milansetu:notifications-changed'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to update notification.')
    } finally { setBusyId(null) }
  }

  const markAll = async () => {
    if (unread === 0 || busyId) return
    setBusyId('__all__')
    setError('')
    try {
      await markAllNotificationsRead()
      const now = new Date().toISOString()
      setItems(current => current.map(x => ({ ...x, readAt: x.readAt ?? now })))
      setServerUnread(0)
      window.dispatchEvent(new Event('milansetu:notifications-changed'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to update notifications.')
    } finally { setBusyId(null) }
  }

  return <>
    <button className="ms-notification-button" onClick={() => void load()} aria-label={`Open notifications${unread ? `, ${unread} unread` : ''}`} aria-expanded={open}>
      Notifications{unread > 0 && <span className="ms-notification-badge">{unread > 99 ? '99+' : unread}</span>}
    </button>
    {open && <aside className="ms-notification-panel" aria-label="Notifications">
      <div className="ms-notification-head"><div><strong>Notifications</strong><small>Private account activity and safety notices.</small></div><button onClick={() => setOpen(false)} aria-label="Close notifications">Close</button></div>
      {error && <p className="ms-notification-error" role="alert">{error}</p>}
      <div className="ms-notification-toolbar"><span>{loading ? 'Refreshing…' : unread ? `${unread} unread` : 'All caught up'}</span>{items.length > 0 && <button className="ms-mark-all" onClick={() => void markAll()} disabled={unread === 0 || busyId !== null}>{busyId === '__all__' ? 'Updating…' : 'Mark all read'}</button>}</div>
      {loading && items.length === 0 && <p aria-live="polite">Loading notifications…</p>}
      {!loading && items.length === 0 && !error && <p>No notifications yet.</p>}
      {items.map(item => <button key={item.id} className={`ms-notification-item ${item.readAt ? '' : 'unread'}`} onClick={() => void markRead(item)} disabled={busyId !== null} aria-label={`${item.readAt ? '' : 'Unread: '}${item.title}`}><strong>{item.title}</strong><span>{item.body}</span><small>{new Date(item.createdAt).toLocaleString()}</small>{busyId === item.id && <em>Updating…</em>}</button>)}
    </aside>}
    <style>{`.ms-notification-button{position:fixed;right:18px;top:18px;z-index:30;padding:10px 14px;border:1px solid #d8cbc6;border-radius:999px;background:#fff;cursor:pointer}.ms-notification-badge{display:inline-grid;place-items:center;min-width:18px;height:18px;margin-left:6px;padding:0 5px;border-radius:99px;background:#8a4b3d;color:#fff;font-size:11px}.ms-notification-panel{position:fixed;right:18px;top:62px;z-index:31;width:min(420px,calc(100vw - 36px));max-height:70vh;overflow:auto;padding:18px;border:1px solid #eadfdb;border-radius:18px;background:#fff;box-shadow:0 16px 50px rgba(0,0,0,.12)}.ms-notification-head{display:flex;justify-content:space-between;gap:12px;align-items:center}.ms-notification-head small{display:block;opacity:.65;margin-top:4px}.ms-notification-head button,.ms-mark-all{border:0;background:transparent;cursor:pointer;padding:6px}.ms-notification-toolbar{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:12px;font-size:12px;opacity:.75}.ms-notification-item{display:flex;flex-direction:column;align-items:flex-start;width:100%;text-align:left;gap:4px;padding:13px 7px;border:0;border-bottom:1px solid #eee3df;background:transparent;cursor:pointer}.ms-notification-item.unread{background:#fcf7f5}.ms-notification-item:disabled{cursor:wait;opacity:.7}.ms-notification-item small{opacity:.55;font-size:11px}.ms-notification-item em{font-size:11px;opacity:.65}.ms-notification-error{padding:9px;border-radius:9px;background:#fff0ef}.ms-notification-panel p{line-height:1.5}@media(max-width:700px){.ms-notification-button{right:10px;top:10px}.ms-notification-panel{right:10px;top:54px;width:calc(100vw - 20px)}}`}</style>
  </>
}
