import { useEffect, useRef, useState } from 'react'
import { deleteMessage, getConversations, getMessages, openConversation, sendMessage, type Conversation, type Message } from './messagingApi'
import './messaging.css'

const currentUserId = () => {
  try {
    return JSON.parse(atob((sessionStorage.getItem('milansetu_access_token') ?? '').split('.')[1] ?? '')).sub ?? ''
  } catch {
    return ''
  }
}

export default function MessagingPanel() {
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [selected, setSelected] = useState<Conversation | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [body, setBody] = useState('')
  const [loading, setLoading] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState('')
  const [sending, setSending] = useState(false)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const requestVersion = useRef(0)
  const selectedIdRef = useRef<string | null>(null)

  const loadConversations = async (showBusy = false) => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    if (showBusy) setRefreshing(true)
    setError('')
    try {
      const next = await getConversations()
      setConversations(next)
      setSelected(current => current ? next.find(item => item.id === current.id) ?? null : null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to load conversations.')
    } finally {
      if (showBusy) setRefreshing(false)
    }
  }

  const loadMessages = async (conversation: Conversation) => {
    const version = ++requestVersion.current
    setSelected(conversation)
    selectedIdRef.current = conversation.id
    setLoading(true)
    setError('')
    try {
      const next = await getMessages(conversation.id)
      if (version === requestVersion.current) setMessages(next)
    } catch (e) {
      if (version === requestVersion.current) setError(e instanceof Error ? e.message : 'Unable to load messages.')
    } finally {
      if (version === requestVersion.current) setLoading(false)
    }
  }

  useEffect(() => {
    void loadConversations()
    const timer = window.setInterval(() => {
      if (document.visibilityState === 'visible') {
        void loadConversations()
        if (selectedIdRef.current) {
          const current = conversations.find(x => x.id === selectedIdRef.current)
          if (current) void loadMessages(current)
        }
      }
    }, 15000)
    const handler = (event: Event) => {
      const id = (event as CustomEvent<string>).detail
      if (!id) return
      void (async () => {
        try {
          const opened = await openConversation(id)
          const conversation: Conversation = {
            id: opened.id,
            otherUserId: opened.otherUserId,
            createdAt: new Date().toISOString(),
            lastMessageAt: null,
          }
          setConversations(current => [conversation, ...current.filter(x => x.id !== conversation.id)])
          await loadMessages(conversation)
        } catch (e) {
          setError(e instanceof Error ? e.message : 'Unable to open conversation.')
        }
      })()
    }
    const connectionChanged = () => { void loadConversations() }
    window.addEventListener('milansetu:open-conversation', handler)
    window.addEventListener('milansetu:connections-changed', connectionChanged)
    return () => {
      window.removeEventListener('milansetu:open-conversation', handler)
      window.removeEventListener('milansetu:connections-changed', connectionChanged)
      window.clearInterval(timer)
    }
  }, [])

  const send = async () => {
    if (!selected || !body.trim() || sending) return
    setSending(true)
    setError('')
    try {
      const message = await sendMessage(selected.id, body.trim())
      setMessages(current => [...current, message])
      setBody('')
      setConversations(current => current.map(x => x.id === selected.id ? { ...x, lastMessageAt: message.createdAt } : x))
      window.dispatchEvent(new CustomEvent('milansetu:notifications-changed'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to send message.')
    } finally {
      setSending(false)
    }
  }

  const remove = async (message: Message) => {
    if (!selected || message.senderUserId !== currentUserId() || deletingId) return
    setDeletingId(message.id)
    setError('')
    try {
      await deleteMessage(selected.id, message.id)
      setMessages(current => current.filter(x => x.id !== message.id))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to delete message.')
    } finally {
      setDeletingId(null)
    }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="messaging-panel" aria-label="Messages">
    <div className="messaging-header">
      <div><p className="eyebrow">MESSAGES</p><h2>Your conversations</h2><p>Messaging is available only after a mutual connection. Contact details remain private.</p></div>
      <button className="secondary-button" disabled={refreshing} onClick={() => void loadConversations(true)}>{refreshing ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {error && <p className="discovery-error" role="alert">{error}</p>}
    <div className="messaging-layout">
      <aside className="conversation-list" aria-label="Conversation list">
        <h3>Conversations</h3>
        {conversations.length === 0 ? <p className="discovery-empty">No conversations yet. Connect with someone first.</p> : conversations.map(c => <button key={c.id} className={`conversation-item ${selected?.id === c.id ? 'active' : ''}`} aria-pressed={selected?.id === c.id} onClick={() => void loadMessages(c)}>
          <strong>Connection</strong><span>{c.lastMessageAt ? new Date(c.lastMessageAt).toLocaleString() : 'New connection'}</span>
        </button>)}
      </aside>
      <div className="chat-pane">
        {!selected ? <div className="discovery-empty"><h3>Select a conversation</h3><p>Choose a mutual connection to start messaging.</p></div> : <>
          <div className="chat-header"><strong>Private conversation</strong><span>Messages are limited to 4,000 characters.</span></div>
          <div className="message-list" aria-live="polite">
            {loading ? <p>Loading messages…</p> : messages.length === 0 ? <p className="discovery-empty">No messages yet. Start respectfully.</p> : messages.map(m => <div key={m.id} className={`message-bubble ${m.senderUserId === currentUserId() ? 'mine' : 'theirs'}`}>
              <p>{m.body}</p><small>{new Date(m.createdAt).toLocaleString()}{m.senderUserId === currentUserId() && <button className="text-button" disabled={deletingId === m.id} onClick={() => void remove(m)}>{deletingId === m.id ? 'Deleting…' : 'Delete'}</button>}</small>
            </div>)}
          </div>
          <div className="message-composer">
            <textarea value={body} onChange={e => setBody(e.target.value)} onKeyDown={e => { if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') { e.preventDefault(); void send() } }} maxLength={4000} rows={3} placeholder="Write a respectful message…" aria-label="Message" />
            <div><span>{body.length}/4000 · Ctrl+Enter to send</span><button className="primary-button" disabled={!body.trim() || sending} onClick={() => void send()}>{sending ? 'Sending…' : 'Send'}</button></div>
          </div>
        </>}
      </div>
    </div>
  </section>
}
