import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'

const tokenKey = 'milansetu_access_token'

let connection: HubConnection | null = null
let starting: Promise<void> | null = null

const apiBaseUrl = () => (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export async function startRealtime() {
  const token = sessionStorage.getItem(tokenKey)
  const base = apiBaseUrl()
  if (!token || !base) return
  if (connection?.state === 'Connected' || connection?.state === 'Connecting') return starting

  connection = new HubConnectionBuilder()
    .withUrl(`${base}/hubs/notifications`, { accessTokenFactory: () => sessionStorage.getItem(tokenKey) ?? '' })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on('notification', event => {
    window.dispatchEvent(new CustomEvent('milansetu:realtime-notification', { detail: event }))
  })
  connection.onreconnected(() => window.dispatchEvent(new Event('milansetu:realtime-connected')))
  connection.onclose(() => window.dispatchEvent(new Event('milansetu:realtime-disconnected')))

  starting = connection.start().then(() => {
    window.dispatchEvent(new Event('milansetu:realtime-connected'))
  }).catch(() => {
    connection = null
  }).finally(() => { starting = null })

  return starting
}

export async function stopRealtime() {
  const active = connection
  connection = null
  if (active) await active.stop()
}
