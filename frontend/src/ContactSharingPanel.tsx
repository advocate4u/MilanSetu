import { useEffect, useState } from 'react'

type Settings={enabled:boolean;requireMutualMatch:boolean;sharePhone:boolean;shareEmail:boolean;shareWhatsApp:boolean}
type RequestItem={id:string;requesterUserId?:string;recipientUserId?:string;status:string}
type Requests={incoming:RequestItem[];outgoing:RequestItem[]}
async function api(path:string,init?:RequestInit){const token=sessionStorage.getItem('milansetu_access_token');const r=await fetch(path,{...init,headers:{'Content-Type':'application/json',Authorization:'Bearer '+token,...(init?.headers||{})}});if(!r.ok)throw new Error((await r.json().catch(()=>null))?.message||'Request failed');return r.json()}
export default function ContactSharingPanel(){
 const [settings,setSettings]=useState<Settings|null>(null),[requests,setRequests]=useState<Requests|null>(null),[message,setMessage]=useState('')
 const load=async()=>{try{setSettings(await api('/api/contact-sharing/settings'));setRequests(await api('/api/contact-sharing/requests'))}catch{}}
 useEffect(()=>{void load()},[])
 const respond=async(id:string,a:'accept'|'decline')=>{try{await api('/api/contact-sharing/requests/'+id+'/'+a,{method:'POST'});setMessage(a==='accept'?'Contact sharing accepted.':'Contact sharing declined.');await load()}catch(e){setMessage(e instanceof Error?e.message:'Request failed')}}
 if(!settings?.enabled)return null
 const incoming=(requests?.incoming||[]).filter(x=>x.status==='Pending')
 return <section className="panel"><h2>Contact sharing</h2><p>Contact details stay private until the required consent is completed.</p>{incoming.length===0?<p>No pending contact-share requests.</p>:incoming.map(x=><div key={x.id} style={{display:'flex',justifyContent:'space-between',gap:12,alignItems:'center',padding:'10px 0',borderBottom:'1px solid #eee'}}><span>Someone you matched with requested your contact details.</span><span><button onClick={()=>void respond(x.id,'accept')}>Accept</button><button onClick={()=>void respond(x.id,'decline')}>Decline</button></span></div>)}{message&&<small>{message}</small>}</section>
}