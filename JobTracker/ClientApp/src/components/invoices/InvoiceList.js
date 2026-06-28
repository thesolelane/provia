import React, { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STATUS_OPTIONS = [
  { value: '', label: 'All Statuses' },
  { value: 'draft', label: 'Draft' },
  { value: 'sent', label: 'Sent' },
  { value: 'partial', label: 'Partial' },
  { value: 'paid', label: 'Paid' },
  { value: 'overdue', label: 'Overdue' },
  { value: 'void', label: 'Void' },
];

const STATUS_META = {
  draft:   { label: 'Draft',   color: '#6c757d', bg: '#f8f9fa' },
  sent:    { label: 'Sent',    color: '#0d6efd', bg: '#e7f0ff' },
  partial: { label: 'Partial', color: '#fd7e14', bg: '#fff3e0' },
  paid:    { label: 'Paid',    color: '#198754', bg: '#e6f7ee' },
  overdue: { label: 'Overdue', color: '#dc3545', bg: '#fdecea' },
  void:    { label: 'Void',    color: '#adb5bd', bg: '#f8f9fa' },
};

function fmtMoney(n) {
  return `$${Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
function fmtDate(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

export default function InvoiceList() {
  const [invoices, setInvoices] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({ clientName: '', clientEmail: '', notes: '', dueAt: '', issuedAt: new Date().toISOString().slice(0, 10) });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const fetchAll = useCallback(async () => {
    try {
      setLoading(true);
      const [inv, sum] = await Promise.all([
        apiService.invoices.getAll(statusFilter, search),
        apiService.invoices.getSummary(),
      ]);
      setInvoices(inv);
      setSummary(sum);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }, [statusFilter, search]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  const filtered = invoices.filter(inv => {
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      inv.invoiceNumber?.toLowerCase().includes(q) ||
      inv.clientName?.toLowerCase().includes(q) ||
      inv.jobNumber?.toLowerCase().includes(q) ||
      inv.jobName?.toLowerCase().includes(q)
    );
  });

  const effectiveStatus = (inv) => {
    if (inv.isOverdue && inv.status !== 'paid' && inv.status !== 'void') return 'overdue';
    return inv.status;
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      const result = await apiService.invoices.create({
        clientName: form.clientName,
        clientEmail: form.clientEmail,
        notes: form.notes,
        issuedAt: form.issuedAt ? new Date(form.issuedAt).toISOString() : null,
        dueAt: form.dueAt ? new Date(form.dueAt).toISOString() : null,
        lineItemsJson: '[]',
        taxRate: 0,
      });
      navigate(`/invoices/${result.id}`);
    } catch (e) {
      setError(e.message || 'Failed to create invoice');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: '24px 16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h1 style={{ margin: 0, fontSize: 26, color: '#2F5A7E' }}>Invoices</h1>
          <p style={{ margin: '4px 0 0', color: '#888', fontSize: 14 }}>Billing and payment tracking</p>
        </div>
        <button onClick={() => setCreating(true)} style={{ background: '#FF9500', color: '#fff', border: 'none', borderRadius: 6, padding: '10px 20px', fontWeight: 600, cursor: 'pointer', fontSize: 14 }}>
          + New Invoice
        </button>
      </div>

      {/* Summary cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16, marginBottom: 28 }}>
        {summary ? (
          <>
            <SummaryCard label="Outstanding" value={fmtMoney(summary.totalOutstanding)} color="#fd7e14" />
            <SummaryCard label="Paid This Month" value={fmtMoney(summary.paidThisMonth)} color="#198754" />
            <SummaryCard label="Overdue" value={summary.overdueCount} color="#dc3545" isCount />
            <SummaryCard label="Drafts" value={summary.draftCount} color="#6c757d" isCount />
          </>
        ) : (
          [
            { label: 'Outstanding', value: fmtMoney(invoices.filter(i => ['sent','partial','overdue'].includes(i.status)).reduce((s,i) => s + (i.balanceDue||0), 0)), color: '#fd7e14' },
            { label: 'Collected', value: fmtMoney(invoices.filter(i => i.status === 'paid').reduce((s,i) => s + (i.total||0), 0)), color: '#198754' },
            { label: 'Drafts', value: invoices.filter(i => i.status === 'draft').length, color: '#6c757d' },
            { label: 'Overdue', value: invoices.filter(i => i.isOverdue).length, color: '#dc3545' },
          ].map(card => <SummaryCard key={card.label} label={card.label} value={card.value} color={card.color} isCount={typeof card.value === 'number'} />)
        )}
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 20, flexWrap: 'wrap', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
          {STATUS_OPTIONS.map(o => (
            <button key={o.value} onClick={() => setStatusFilter(o.value)} style={{
              padding: '5px 14px', borderRadius: 20, border: '1px solid', cursor: 'pointer', fontSize: 12, fontWeight: 600,
              background: statusFilter === o.value ? '#2F5A7E' : '#fff',
              color: statusFilter === o.value ? '#fff' : '#6c757d',
              borderColor: statusFilter === o.value ? '#2F5A7E' : '#dee2e6',
            }}>
              {o.label}
            </button>
          ))}
        </div>
        <input
          placeholder="Search by number, client, job…"
          value={search}
          onChange={e => setSearch(e.target.value)}
          style={{ flex: 1, minWidth: 200, padding: '8px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14 }}
        />
        <button onClick={fetchAll} style={{ padding: '8px 16px', background: '#2F5A7E', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 14 }}>Refresh</button>
      </div>

      {/* Invoice table */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: 60, color: '#888' }}>Loading invoices…</div>
      ) : filtered.length === 0 ? (
        <div style={{ textAlign: 'center', padding: 60, color: '#888', background: '#fafafa', borderRadius: 10, border: '1px dashed #ddd' }}>
          <div style={{ fontSize: 40, marginBottom: 12 }}>🧾</div>
          <div style={{ fontSize: 16, fontWeight: 600 }}>No invoices found</div>
          <div style={{ fontSize: 14, marginTop: 6 }}>Create your first invoice to get started</div>
        </div>
      ) : (
        <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 10, overflow: 'hidden', boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                {['Invoice #', 'Client', 'Job', 'Due', 'Total', 'Balance', 'Status', ''].map(h => (
                  <th key={h} style={{ padding: '10px 14px', textAlign: 'left', fontSize: 12, color: '#495057', textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filtered.map((inv, i) => {
                const st = effectiveStatus(inv);
                const meta = STATUS_META[st] || STATUS_META.draft;
                return (
                  <tr key={inv.id} style={{ borderBottom: '1px solid #f0f0f0', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                    <td style={{ padding: '10px 14px', fontFamily: 'monospace', fontWeight: 700, color: '#2F5A7E' }}>
                      <Link to={`/invoices/${inv.id}`} style={{ color: '#2F5A7E', textDecoration: 'none' }}>{inv.invoiceNumber}</Link>
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      <div style={{ fontWeight: 500, fontSize: 14 }}>{inv.clientName || '—'}</div>
                      {inv.clientEmail && <div style={{ fontSize: 12, color: '#888' }}>{inv.clientEmail}</div>}
                    </td>
                    <td style={{ padding: '10px 14px', fontSize: 13, color: '#555' }}>
                      {inv.jobNumber ? <span style={{ background: '#f0f4f8', padding: '2px 8px', borderRadius: 4, fontSize: 12 }}>{inv.jobNumber}</span> : '—'}
                    </td>
                    <td style={{ padding: '10px 14px', fontSize: 13, color: st === 'overdue' ? '#dc3545' : '#555', fontWeight: st === 'overdue' ? 700 : 400 }}>
                      {fmtDate(inv.dueAt)}
                    </td>
                    <td style={{ padding: '10px 14px', fontWeight: 600, fontSize: 14 }}>{fmtMoney(inv.total)}</td>
                    <td style={{ padding: '10px 14px', fontWeight: 600, fontSize: 14, color: (inv.balanceDue||0) > 0 ? '#dc3545' : '#198754' }}>
                      {fmtMoney(inv.balanceDue)}
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: 11, fontWeight: 700, background: meta.bg, color: meta.color, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                        {meta.label}
                      </span>
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      <Link to={`/invoices/${inv.id}`} style={{ color: '#2F5A7E', fontSize: 13, textDecoration: 'none' }}>View →</Link>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* New Invoice Modal */}
      {creating && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }} onClick={() => setCreating(false)}>
          <div style={{ background: '#fff', borderRadius: 12, padding: 32, width: '100%', maxWidth: 480, boxShadow: '0 8px 40px rgba(0,0,0,0.18)' }} onClick={e => e.stopPropagation()}>
            <h2 style={{ margin: '0 0 20px', color: '#2F5A7E', fontSize: 20 }}>New Invoice</h2>
            {error && <div style={{ background: '#fff0f0', border: '1px solid #fcc', color: '#c00', padding: '10px 14px', borderRadius: 6, marginBottom: 16, fontSize: 14 }}>{error}</div>}
            <form onSubmit={handleCreate}>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Client Name</label>
                <input value={form.clientName} onChange={e => setForm(f => ({ ...f, clientName: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} placeholder="Client or company name" />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Client Email</label>
                <input type="email" value={form.clientEmail} onChange={e => setForm(f => ({ ...f, clientEmail: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} placeholder="client@example.com" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 14 }}>
                <div>
                  <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Issue Date</label>
                  <input type="date" value={form.issuedAt} onChange={e => setForm(f => ({ ...f, issuedAt: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} />
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Due Date</label>
                  <input type="date" value={form.dueAt} onChange={e => setForm(f => ({ ...f, dueAt: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} />
                </div>
              </div>
              <div style={{ marginBottom: 22 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Notes (optional)</label>
                <textarea value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} rows={3} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, resize: 'vertical', boxSizing: 'border-box' }} placeholder="Payment terms, project reference, etc." />
              </div>
              <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
                <button type="button" onClick={() => setCreating(false)} style={{ padding: '10px 20px', background: '#f0f0f0', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 14 }}>Cancel</button>
                <button type="submit" disabled={saving} style={{ padding: '10px 24px', background: '#FF9500', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 14, fontWeight: 600 }}>
                  {saving ? 'Creating…' : 'Create Invoice'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ label, value, color, isCount }) {
  return (
    <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 10, padding: '16px 20px', boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
      <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, fontWeight: 700, marginBottom: 6 }}>{label}</div>
      <div style={{ fontSize: isCount ? '2rem' : '1.4rem', fontWeight: 800, color }}>{value}</div>
    </div>
  );
}
