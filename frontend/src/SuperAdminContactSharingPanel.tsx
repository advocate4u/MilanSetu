import { useEffect, useState } from 'react'

type S={enabled:boolean;requireMutualMatch:boolean;sharePhone:boolean;shareEmail:boolean;shareWhatsApp:boolean}
async function api(path:string,init?:RequestInit){const t=sessionStorage.getItem('milansetu_access_token');const r=await fetch(path,{...init,headers:{'Content-Type':'application/json',Authorization:'Bearer '+t,...(init?.headers||{})}});if(!r.ok)throw new Error((await r.json().catch(()=>null))?.message||'Request failed');return r.json()}
export default function SuperAdminContactSharingPanel(){
 const [role,setRole]=useState(''),[s,setS]=useState<S|null>(null),[msg,setMsg]=useState('')
 useEffect(()=>{(async()=>{try{const me=await api('/api/auth/me');setRole(me.role||'');if(me.role==='SuperAdmin')setS(await api('/api/admin/contact-sharing'))}catch{}})()},[])
 if(role!=='SuperAdmin'||!s)return null
 const save=async()=>{try{await api('/api/admin/contact-sharing',{method:'PUT',body:JSON.stringify(s)});setMsg('Contact-sharing settings saved.')}catch(e){setMsg(e instanceof Error?e.message:'Unable to save settings.')}}
 return <section className="panel"><h2>Contact sharing controls</h2><p>Control when matched users may share contact details.</p>
 <label><input type="checkbox" checked={s.enabled} onChange={e=>setS({...s,enabled:e.target.checked})}/> Enable contact sharing</label>
 <label><input type="checkbox" checked={s.requireMutualMatch} onChange={e=>setS({...s,requireMutualMatch:e.target.checked})}/> Require both users to accept contact sharing</label>
 <label><input type="checkbox" checked={s.sharePhone} onChange={e=>setS({...s,sharePhone:e.target.checked})}/> Share mobile number</label>
 <label><input type="checkbox" checked={s.shareWhatsApp} onChange={e=>setS({...s,shareWhatsApp:e.target.checked})}/> Share WhatsApp number</label>
 <label><input type="checkbox" checked={s.shareEmail} onChange={e=>setS({...s,shareEmail:e.target.checked})}/> Share email address</label>
 <button className="primary-button" onClick={()=>void save()}>Save contact-sharing settings</button>{msg&&<small>{msg}</small>}</section>
}