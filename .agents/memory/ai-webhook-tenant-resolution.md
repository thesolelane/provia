---
name: AI webhook tenant resolution
description: Tenant isolation rule for provider webhooks that do not carry an authenticated application user.
---

Provider webhooks must not infer CompanyId from sender phone numbers, message text, request payload fields, or model output. If the application cannot establish a trusted tenant context before retrieval, AI processing and company-specific responses must be refused.

**Why:** A shared provider endpoint has no authenticated application tenant by default, so guessing from untrusted webhook data could route one company's data into another company's conversation.

**How to apply:** Add an explicit trusted provider-account-to-company mapping before enabling tenant-specific webhook AI. Until then, keep the webhook acknowledgement available but fail closed for retrieval and generated responses.