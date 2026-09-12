import { useEffect, useState } from 'react'
import { deleteMessage, getConversations, getMessages, openConversation, sendMessage, type Conversation, type Message } from './messagingApi'

const currentUserId = () => { try { return JSON.parse(atob((sessionStorage.getItem('milansetu_access_token') ?? '').split('.')[1] ?? '')).sub ?? '' } catch { return '' } }

export default function MessagingPanel() {
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [selected, setSelected] = useState<Conversation | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [body, setBody] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [sending, setSending] = useState(false)

  const loadConversations = async () => { if (!sessionStorage.getItem('milansetu_access_token')) return; try { setConversations(await getConversations()) } catch (e) { setError(e instanceof Error ? e.message : 'Unable to load conversations.') } }
  const loadMessages = async (conversation: Conversation) => { setSelected(conversation); setLoading(true); setError(''); try { setMessages(await getMessages(conversation.id)) } catch (e) { setError(e instanceof Error ? e.message : 'Unable to load messages.') } finally { setLoading(false) } }

  useEffect(() => { void loadConversations(); const handler = (event: Event) => { const id = (event as CustomEvent<string>).detail; if (!id) return; void (async () => { try { const opened = await openConversation(id); const conversation: Conversation = { id: opened.id, otherUserId: opened.otherUserId, createdAt: new Date().toISOString(), lastMessageAt: null }; setConversations(current => [conversation, ...current.filter(x => x.id !== conversation.id)]); await loadMessages(conversation) } catch (e) { setError(e instanceof Error ? e.message : 'Unable to open conversation.') } })() }; window.addEventListener('milansetu:open-conversation', handler); return () => window.removeEventListener('milansetu:open-conversation', handler) }, [])

  const send = async () => { if (!selected || !body.trim() || sending) return; setSending(true); setError(''); try { const message = await sendMessage(selected.id, body.trim()); setMessages(current => [...current, message]); setBody(''); setConversations(current => current.map(x => x.id === selected.id ? { ...x, lastMessageAt: message.createdAt } : x)) } catch (e) { setError(e instanceof Error ? e.message : 'Unable to send message.') } finally { setSending(false) } }
  const remove = async (message: Message) => { if (!selected || message.senderUserId !== currentUserId()) return; try { await deleteMessage(selected.id, message.id); setMessages(current => current.filter(x => x.id !== message.id)) } catch (e) { setError(e instanceof Error ? e.message : 'Unable to delete message.') } }
  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="messaging-panel" aria-label="Messages"><div className="messaging-header"><div><p className="eyebrow">MESSAGES</p><h2>Your conversations</h2><p>Messaging is available only after a mutual connection. Contact details remain private.</p></div><button className="secondary-button" onClick={() => void loadConversations()}>Refresh</button></div>{error && <p className="discovery-error" role="alert">{error}</p>}<div className="messaging-layout"><aside className="conversation-list"><h3>Conversations</h3>{conversations.length === 0 ? <p className="discovery-empty">No conversations yet.</p> : conversations.map(c => <button key={c.id} className={`conversation-item ${selected?.id === c.id ? 'active' : ''}`} onClick={() => void loadMessages(c)}><strong>Connection</strong><span>{c.lastMessageAt ? new Date(c.lastMessageAt).toLocaleString() : 'New connection'}</span></button>)}</aside><div className="chat-pane">{!selected ? <div className="discovery-empty"><h3>Select a conversation</h3><p>Choose a mutual connection to start messaging.</p></div> : <><div className="chat-header"><strong>Private conversation</strong><span>Messages are limited to 4,000 characters.</span></div><div className="message-list">{loading ? <p>Loading messages…</p> : messages.length === 0 ? <p className="discovery-empty">No messages yet. Start respectfully.</p> : messages.map(m => <div key={m.id} className={`message-bubble ${m.senderUserId === currentUserId() ? 'mine' : 'theirs'}`}><p>{m.body}</p><small>{new Date(m.createdAt).toLocaleString()}{m.senderUserId === currentUserId() && <button className="text-button" onClick={() => void remove(m)}>Delete</button>}</small></div>)}</div><div className="message-composer"><textarea value={body} onChange={e => setBody(e.target.value)} maxLength={4000} rows={3} placeholder="Write a respectful message…"/><div><span>{body.length}/4000</span><button className="primary-button" disabled={!body.trim() || sending} onClick={() => void send()}>{sending ? 'Sending…' : 'Send'}</button></div></div></>}</div></div></section>
}
