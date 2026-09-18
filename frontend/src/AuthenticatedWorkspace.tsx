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
import PrivacyPanel from './PrivacyPanel'
import LegalAcceptancePanel from './LegalAcceptancePanel'
import SafetyPanel from './SafetyPanel'
import AdminAnalyticsPanel from './AdminAnalyticsPanel'
import AdminMetricsPanel from './AdminMetricsPanel'
import SuperAdminAdvertisingPanel from './SuperAdminAdvertisingPanel'
import ContactSharingPanel from './ContactSharingPanel'
import SuperAdminContactSharingPanel from './SuperAdminContactSharingPanel'
import AdvertisingPanel from './AdvertisingPanel'
import { startRealtime, stopRealtime } from './realtime'

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
  useEffect(() => {
    if (authenticated) { void startRealtime() } else { void stopRealtime() }
    return () => { void stopRealtime() }
  }, [authenticated])
  if (!authenticated) return null
  return <>
    <AccountSessionPanel />
    <LegalAcceptancePanel />
    <AdvertisingPanel />
    <ProfileEditor />
    <ProfilePhotoManager />
    <PrivacyPanel />
    <SafetyPanel />
    <VerificationPanel />
    <DiscoveryPanel />
    <IncomingInterestsPanel />
    <ConnectionsPanel />
    <ContactSharingPanel />
    <MessagingPanel />
    <NotificationPanel />
    <MyReportsPanel />
    <ReviewerPanel />
    <AdminDashboardPanel />
    <AdminAnalyticsPanel />
    <AdminMetricsPanel />
    <AdminModerationPanel />
    <AdminAuditPanel />
    <AdminUsersPanel />
    <SuperAdminAdvertisingPanel />
    <SuperAdminContactSharingPanel />
  </>
}
