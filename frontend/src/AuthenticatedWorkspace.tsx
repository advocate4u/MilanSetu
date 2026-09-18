import { useEffect, useState } from 'react'
import AccountSessionPanel from './AccountSessionPanel'
import ProfileEditor from './ProfileEditor'
import ProfilePhotoManager from './ProfilePhotoManager'
import DiscoveryPanel from './DiscoveryPanel'
import IncomingInterestsPanel from './IncomingInterestsPanel'
import MessagingPanel from './MessagingPanel'
import NotificationPanel from './NotificationPanel'
import MyReportsPanel from './MyReportsPanel'
import ConnectionsPanel from './ConnectionsPanel'
import ReviewerPanel from './ReviewerPanel'
import VerificationPanel from './VerificationPanel'
import AdminModerationPanel from './AdminModerationPanel'
import AdminDashboardPanel from './AdminDashboardPanel'
import AdminAuditPanel from './AdminAuditPanel'
import AdminUsersPanel from './AdminUsersPanel'

const accessTokenKey = 'milansetu_access_token'
function isAuthenticated() { return Boolean(sessionStorage.getItem(accessTokenKey)) }

export default function AuthenticatedWorkspace() {
  const [authenticated, setAuthenticated] = useState(isAuthenticated)
  useEffect(() => {
    const sync = () => setAuthenticated(isAuthenticated())
    sync()
    const timer = window.setInterval(sync, 1000)
    window.addEventListener('milansetu:auth-changed', sync)
    return () => { window.clearInterval(timer); window.removeEventListener('milansetu:auth-changed', sync) }
  }, [])
  if (!authenticated) return null
  return <>
    <AccountSessionPanel />
    <ProfileEditor />
    <ProfilePhotoManager />
    <VerificationPanel />
    <DiscoveryPanel />
    <IncomingInterestsPanel />
    <ConnectionsPanel />
    <MessagingPanel />
    <NotificationPanel />
    <MyReportsPanel />
    <ReviewerPanel />
    <AdminDashboardPanel />
    <AdminModerationPanel />
    <AdminAuditPanel />
    <AdminUsersPanel />
  </>
}
