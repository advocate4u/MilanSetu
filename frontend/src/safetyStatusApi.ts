const base=(import.meta.env.VITE_API_BASE_URL??'').trim().replace(/\/$/,'')
function auth(): Record<string, string> { const t=sessionStorage.getItem('milansetu_access_token'); return t ? { Authorization: `Bearer ${t}` } : {} }
async function request(path:string, options:RequestInit={}) { if(!base) throw new Error('MilanSetu API is not configured for this deployment.'); const r=await fetch(base+path,{credentials:'include',...options,headers:{...auth(),...(options.headers??{})}}); const d=await r.json().catch(()=>null); if(!r.ok) throw new Error(d?.message??'Unable to complete the request.'); return d }
export async function getSafety(){return request('/api/safety/me')}
export type BlockedProfile={userId:string;displayName:string;blockedAt:string}
export async function getBlockedProfiles():Promise<BlockedProfile[]>{return request('/api/block')}
export async function unblockProfile(userId:string):Promise<void>{await request('/api/block/'+encodeURIComponent(userId),{method:'DELETE'})}
