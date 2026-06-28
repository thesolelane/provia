---
name: Company model field names
description: Actual field names on the Company entity to avoid compile errors
---

- Company name: `CompanyName` (not `Name`)
- Contact email: `ContactEmail` (not `Email`)
- These caused a compile error in InvoicesController print endpoint (`company?.Name` → `company?.CompanyName`).

**Why:** The Company model predates standard naming conventions and uses verbose property names.
