import { useEffect, useState } from 'react'

type S={messagingEnabled:boolean;maxProfilePhotos:number}
async function api(path:string,init?:RequestInit){const t=sessionStorage.getItem('milansetu_access_token');const r=await fetch(path,{...init,headers:{'Content-Type':'application/json',Authorization:'Bearer '+t,...(init?.headers||{})}});if(!r.ok)throw new Error((await r.json().catch(()=>null))?.message||'Request failed');return r.json()}
export default function SuperAdminPlatformSettingsPanel(){
 const [role,setRole]=useState(''),[s,setS]=useState<S|null>(null),[msg,setMsg]=useState('')
 useEffect(()=>{(async()=>{try{const me=await api('/api/auth/me');setRole(me.role||'');if(me.role==='SuperAdmin')setS(await api('/api/admin/platform-settings'))}catch{}})()},[])
 if(role!=='SuperAdmin'||!s)return null
 const save=async()=>{try{await api('/api/admin/platform-settings',{method:'PUT',body:JSON.stringify(s)});setMsg('Platform settings saved.')}catch(e){setMsg(e instanceof Error?e.message:'Unable to save settings.')}}
 return <section className="panel"><h2>Platform controls</h2><p>Control messaging and the maximum number of profile photos per user.</p>
 <label><input type="checkbox" checked={s.messagingEnabled} onChange={e=>setS({...s,messagingEnabled:e.target.checked})}/> Enable messaging</label>
 <label>Maximum profile photos per user <input type="number" min={1} max={20} value={s.maxProfilePhotos} onChange={e=>setS({...s,maxProfilePhotos:Math.max(1,Math.min(20,Number(e.target.value)||1))})}/></label>
 <button className="primary-button" onClick={()=>void save()}>Save platform settings</button>{msg&&<small>{msg}</small>}</section>
}