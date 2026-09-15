# MilanSetu — Core Functionality Roadmap

Verification/document review and mobile OTP are intentionally deferred.

## Current core work
- Complete user profile and profile editing experience.
- Complete discovery/search/filter experience.
- Complete production messaging experience: conversations, history, unread state, block/report and safety controls.
- Add notification foundation for messages and account events.
- Complete admin/user-management screens and operational controls.
- Add production API configuration for the frontend and clear configuration errors.
- Improve reproducible frontend builds by pinning dependencies when the lockfile can be generated and validated.
- Run end-to-end smoke testing for registration, login, email verification, discovery, messaging and logout.

## Explicitly deferred
- Identity/Education/Employment document verification enhancements.
- Real document malware scanner/private production storage.
- Mobile OTP.

## Release order
1. Core profile/discovery/messaging/admin functionality.
2. Production configuration and smoke tests.
3. Verification/document production work.
4. Mobile OTP last.
