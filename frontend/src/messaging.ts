export type MessagingDemoMessage = {
  id: number
  sender: 'me' | 'them'
  body: string
  time: string
}

export const messagingLimits = {
  maxLength: 4000,
  pageSize: 50,
}
