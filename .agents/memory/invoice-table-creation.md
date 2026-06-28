---
name: Invoice table creation pattern
description: How new tables are added to the existing PROVIA PostgreSQL DB at runtime
---

EnsureCreated() is a no-op when the DB already has any tables. New tables added after the initial schema must be created via raw SQL in the `addColumns` array in Program.cs (lines ~230–290). Pattern is `CREATE TABLE IF NOT EXISTS` so it's idempotent on every restart.

**Why:** The DB was already bootstrapped before Invoices was added, so EF's EnsureCreated skipped it. Any future new table (Vendors, FieldPhotos, etc.) needs the same treatment.

**How to apply:** Add `CREATE TABLE IF NOT EXISTS "TableName" (...)` entries to the `addColumns` string array in Program.cs startup block. Also add index `CREATE INDEX IF NOT EXISTS` entries in the same array. The loop catches exceptions and logs warnings so a bad entry won't crash startup.
