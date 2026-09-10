import { useState } from 'react'
import { getNotifications, markAllNotificationsRead, markNotificationRead, type NotificationItem } from './safetyApi'

export default function NotificationPanel() {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<NotificationItem[]>([])
  const [error, setError] = useState('')
  const unread = items.filter(x => !x.readAt).length

  const load = async () => {
    setOpen(true)
    setError('')
    try { setItems(await getNotifications()) }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to load notifications.') }
  }

  const markRead = async (item: NotificationItem) => {
    if (item.readAt) return
    try {
      await markNotificationRead(item.id)
      setItems(current => current.map(x => x.id === item.id ? { ...x, readAt: new Date().toISOString() } : x))
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to update notification.') }
  }

  const markAll = async () => {
    try {
      await markAllNotificationsRead()
      const now = new Date().toISOString()
      setItems(current => current.map(x => ({ ...x, readAt: x.readAt ?? now })))
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to update notifications.') }
  }

  return <>
    <button className="ms-notification-button" onClick={load} aria-label="Open notifications">
      Notifications{unread > 0 && <span className="ms-notification-badge">{unread}</span>}
    </button>
    {open && <aside className="ms-notification-panel" aria-label="Notifications">
      <div className="ms-notification-head"><div><strong>Notifications</strong><small>Private account activity and safety notices.</small></div><button onClick={() => setOpen(false)}>Close</button></div>
      {error && <p className="ms-notification-error" role="alert">{error}</p>}
      {items.length > 0 && <button className="ms-mark-all" onClick={markAll} disabled={unread === 0}>Mark all read</button>}
      {items.length === 0 && !error && <p>No notifications yet.</p>}
      {items.map(item => <button key={item.id} className={`ms-notification-item ${item.readAt ? '' : 'unread'}`} onClick={() => markRead(item)}><strong>{item.title}</strong><span>{item.body}</span><small>{new Date(item.createdAt).toLocaleString()}</small></button>)}
    </aside>}
    <style>{`.ms-notification-button{position:fixed;right:18px;top:18px;z-index:30;padding:10px 14px;border:1px solid #d8cbc6;border-radius:999px;background:#fff;cursor:pointer}.ms-notification-badge{display:inline-grid;place-items:center;min-width:18px;height:18px;margin-left:6px;padding:0 5px;border-radius:99px;background:#8a4b3d;color:#fff;font-size:11px}.ms-notification-panel{position:fixed;right:18px;top:62px;z-index:31;width:min(420px,calc(100vw - 36px));max-height:70vh;overflow:auto;padding:18px;border:1px solid #eadfdb;border-radius:18px;background:#fff;box-shadow:0 16px 50px rgba(0,0,0,.12)}.ms-notification-head{display:flex;justify-content:space-between;gap:12px;align-items:center}.ms-notification-head small{display:block;opacity:.65;margin-top:4px}.ms-notification-head button,.ms-mark-all{border:0;background:transparent;cursor:pointer;padding:6px}.ms-notification-item{display:flex;flex-direction:column;align-items:flex-start;width:100%;text-align:left;gap:4px;padding:13px 7px;border:0;border-bottom:1px solid #eee3df;background:transparent;cursor:pointer}.ms-notification-item.unread{background:#fcf7f5}.ms-notification-item small{opacity:.55;font-size:11px}.ms-notification-error{padding:9px;border-radius:9px;background:#fff0ef}@media(max-width:700px){.ms-notification-button{right:10px;top:10px}.ms-notification-panel{right:10px;top:54px;width:calc(100vw - 20px)}}`}</style>
  </>
}
