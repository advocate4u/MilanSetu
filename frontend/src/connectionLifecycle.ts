export type InterestStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Cancelled'

export function isMutualConnection(status: string | null | undefined) {
  return status === 'Accepted'
}
