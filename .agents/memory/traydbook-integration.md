---
name: TraydBook Integration Notes
description: Integration opportunities between PROVIA and TraydBook (construction trade network)
---

# TraydBook ↔ PROVIA Integration

## What TraydBook Is
- Professional network / marketplace for construction trades ("LinkedIn + Angie's List for contractors")
- traydbook.com = waitlist landing page
- dev.traydbook.com = live app (active, with signup)
- Free forever for trade accounts (electricians, plumbers, HVAC, GCs, etc.)
- Credit-based for homeowners, investors, agents
- Auth: Google / Apple / LinkedIn SSO + email
- Stats: 12,000+ verified contractors, 8,400+ projects, 50,000+ bids, 38 states
- Trade specialties: GC, Electrician, Plumber, HVAC, Carpenter, Ironworker, Mason, Painter, Roofer, Welder, Pipefitter, Sheet Metal, Concrete, Drywall, Flooring, Tile, Glazier, Insulation, Landscaping, Construction Manager

## The Strategic Fit
TraydBook and PROVIA solve adjacent problems:
- TraydBook = find the job, win the bid, build reputation (public marketplace)
- PROVIA = manage the job after winning it (internal back-office ops)
They complement rather than compete.

## Integration Tiers

### Tier 1 — Minimum (no API, build independently)
- Add `TraydBookProfileUrl` field to PROVIA Vendor/Subcontractor model → "View on TraydBook" link
- "Post to TraydBook" button on PROVIA jobs needing subs → opens pre-filled TraydBook RFQ URL with job address, scope, budget from PROVIA job record
- Deep-link onboarding: TraydBook user clicks "Manage in PROVIA" → PROVIA signup pre-filled with name, trade, email from URL query params
- TraydBook badge/verified status shown on PROVIA subcontractor cards

### Tier 2 — Shared Auth (if both codebases are controlled by same team)
- Single sign-on: one account works on both platforms
- "Already on TraydBook? Import your profile" during PROVIA subcontractor onboarding
- "Already on PROVIA? Connect your jobs" on TraydBook GC signup flow
- Shared user identity / company ID mapping

### Tier 3 — Full API Sync (deep integration)
- Bid accepted on TraydBook → auto-create job record in PROVIA
- Job created in PROVIA needing subs → auto-post RFQ to TraydBook
- TraydBook verified badge displayed live on PROVIA subcontractor profiles
- Contractor import: TraydBook profile fields pre-populate PROVIA vendor record (trade, license, contact info, reviews)

## Open Questions (decide before building)
- Is the same team/person building both TraydBook and PROVIA? (determines which tiers are feasible)
- Does TraydBook have or plan a REST API with API keys?
- Should SSO be bidirectional or PROVIA-as-identity-provider?
- Priority: onboarding flow first, or subcontractor sourcing first?

## Files to Touch When Building
- `JobTracker/Models/Vendor.cs` — add TraydBookProfileUrl field
- `JobTracker/ClientApp/src/components/vendors/VendorDetail.js` — show badge/link
- New: `JobTracker/Controllers/TraydBookController.cs` — deep-link onboarding endpoint
- New: `JobTracker/ClientApp/src/components/onboarding/TraydBookConnect.js`
