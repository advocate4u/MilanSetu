import { useEffect, useState } from 'react'
import './admin-users.css'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/$/, '') ?? ''
type User = { id:string; email:string; phoneNumber?:string|null; isActive:boolean; isEmailVerified:boolean; isPhoneVerified:boolean; createdAt:string; role:string }

async function api(path:string, options:RequestInit={}) {
  if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.')
  const token=sessionStorage.getItem('milansetu_access_token')
  const r=await fetch(`${apiBaseUrl}${path}`,{...options,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{ } ),...(options.headers??{})}})
  const d=await r.json().catch(()=>({}))
  if(r.status===401){sessionStorage.removeItem('milansetu_access_token');window.dispatchEvent(new Event('milansetu:auth-changed'));throw new Error('Your session has expired. Please sign in again.')}
  if(!r.ok) throw new Error(d.message??'Request failed.')
  return d
}

export default function AdminUsersPanel(){
  const [allowed,setAllowed]=useState(false),[users,setUsers]=useState<User[]>([]),[search,setSearch]=useState(''),[applied,setApplied]=useState(''),[loading,setLoading]=useState(false),[busy,setBusy]=useState<string|null>(null),[message,setMessage]=useState('')
  const load=async(term=applied)=>{setLoading(true);setMessage('');try{const me=await api('/api/auth/me');if(me.role!=='Admin'&&me.role!=='SuperAdmin'){setAllowed(false);return}setAllowed(true);const q=term?`?search=${encodeURIComponent(term)}&page=1&pageSize=50`:'?page=1&pageSize=50';const d=await api('/api/admin/users'+q);setUsers(d.items??[])}catch(e){setMessage(e instanceof Error?e.message:'Unable to load users.')}finally{setLoading(false)}}
  useEffect(()=>{void load('')},[])
  if(!allowed)return null
  const toggle=async(u:User)=>{setBusy(u.id);setMessage('');try{await api(`/api/admin/users/${u.id}/status`,{method:'POST',body:JSON.stringify({active:!u.isActive})});setUsers(xs=>xs.map(x=>x.id===u.id?{...x,isActive:!u.isActive}:x))}catch(e){setMessage(e instanceof Error?e.message:'Unable to update user status.')}finally{setBusy(null)}}
  const changeRole=async(u:User,role:string)=>{setBusy(u.id);setMessage('');try{await api(`/api/admin/users/${u.id}/role`,{method:'POST',body:JSON.stringify({role})});setUsers(xs=>xs.map(x=>x.id===u.id?{...x,role}:x))}catch(e){setMessage(e instanceof Error?e.message:'Unable to update user role.')}finally{setBusy(null)}}
  return <section className="admin-users-panel" aria-label="Admin user management"><div className="admin-users-head"><div><p className="eyebrow">ADMIN USER MANAGEMENT</p><h2>Users</h2><p>Search accounts, inspect verification status and control account activation.</p></div></div><form onSubmit={e=>{e.preventDefault();setApplied(search.trim());void load(search.trim())}} className="admin-users-search"><input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Search email or phone" aria-label="Search users"/><button className="primary-button" disabled={loading}>{loading?'Searching…':'Search'}</button></form>{message&&<p className="admin-users-message" role="status">{message}</p>}<div className="admin-users-list">{users.length===0?<p>{loading?'Loading users…':'No users found.'}</p>:users.map(u=><article className="admin-user-row" key={u.id}><div><strong>{u.email}</strong><span>{u.phoneNumber||'No phone'} · {u.role}</span><small>{u.isEmailVerified?'Email verified':'Email unverified'} · {u.isPhoneVerified?'Phone verified':'Phone unverified'} · Joined {new Date(u.createdAt).toLocaleDateString()}</small></div><select value={u.role} disabled={busy===u.id || u.role==='Admin' || u.role==='SuperAdmin'} onChange={e=>void changeRole(u,e.target.value)} aria-label={`Role for ${u.email}`}><option>User</option><option>Reviewer</option><option>Admin</option><option>SuperAdmin</option></select><button type="button" className="secondary-button" disabled={busy===u.id} onClick={()=>void toggle(u)}>{busy===u.id?'Saving…':u.isActive?'Deactivate':'Activate'}</button></article>)}</div></section>
}