import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STATUS_FILTERS = ['all', 'draft', 'sent', 'partial', 'paid', 'overdue', 'void'];

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
  const [statusFilter, setStatusFilter] = useState('all');
  const [search, setSearch] = useState('');
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

  const effectiveStatus = (inv) =>
    inv.isOverdue && inv.status === 'sent' ? 'overdue' : inv.status;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Invoices</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>Billing and payment tracking</p>
        </div>
        <button className="btn btn-primary" onClick={() => navigate('/invoices/new')}>+ New Invoice</button>
      </div>

      {summary && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
          <SummaryCard label="Outstanding"    value={fmtMoney(summary.totalOutstanding)} color="#fd7e14" />
          <SummaryCard label="Paid This Month" value={fmtMoney(summary.paidThisMonth)}   color="#198754" />
          <SummaryCard label="Overdue"        value={summary.overdueCount}               color="#dc3545" isCount />
          <SummaryCard label="Drafts"         value={summary.draftCount}                 color="#6c757d" isCount />
        </div>
      )}

      <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap' }}>
          {STATUS_FILTERS.map(s => (
            <button key={s} onClick={() => setStatusFilter(s)} style={{
              padding: '4px 12px', borderRadius: 20, border: '1px solid', cursor: 'pointer', fontSize: '0.82rem', fontWeight: 600,
              background: statusFilter === s ? '#2F5A7E' : '#fff',
              color: statusFilter === s ? '#fff' : '#6c757d',
              borderColor: statusFilter === s ? '#2F5A7E' : '#dee2e6',
            }}>
              {s === 'all' ? 'All' : STATUS_META[s]?.label}
            </button>
          ))}
        </div>
        <input className="form-control" placeholder="Search by number or client..." value={search}
          onChange={e => setSearch(e.target.value)} style={{ maxWidth: 260 }} />
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        {loading ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                {['Invoice #', 'Client', 'Issued', 'Due', 'Total', 'Paid', 'Balance', 'Status', ''].map(h => (
                  <th key={h} style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.82rem', color: '#495057' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {invoices.length === 0 && (
                <tr><td colSpan={9} style={{ padding: '2rem', textAlign: 'center', color: '#aaa' }}>No invoices found</td></tr>
              )}
              {invoices.map((inv, i) => {
                const st = effectiveStatus(inv);
                const meta = STATUS_META[st] || STATUS_META.draft;
                return (
                  <tr key={inv.id} style={{ borderBottom: '1px solid #dee2e6', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                    <td style={{ padding: '0.75rem 1rem', fontFamily: 'monospace', fontSize: '0.82rem', color: '#2F5A7E', fontWeight: 700 }}>{inv.invoiceNumber}</td>
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600, fontSize: '0.9rem' }}>{inv.clientName || '—'}</td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem', color: '#6c757d' }}>{fmtDate(inv.issuedAt)}</td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem', color: st === 'overdue' ? '#dc3545' : '#6c757d', fontWeight: st === 'overdue' ? 700 : 400 }}>{fmtDate(inv.dueAt)}</td>
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600 }}>{fmtMoney(inv.total)}</td>
                    <td style={{ padding: '0.75rem 1rem', color: '#198754' }}>{fmtMoney(inv.amountPaid)}</td>
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600, color: inv.balanceDue > 0 ? '#dc3545' : '#198754' }}>{fmtMoney(inv.balanceDue)}</td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: '0.78rem', fontWeight: 700, background: meta.bg, color: meta.color }}>{meta.label}</span>
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <button onClick={() => navigate(`/invoices/${inv.id}`)} className="btn btn-sm" style={{ fontSize: '0.8rem', padding: '3px 10px' }}>Open</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function SummaryCard({ label, value, color, isCount }) {
  return (
    <div className="card" style={{ padding: '1rem 1.25rem' }}>
      <div style={{ fontSize: '0.78rem', fontWeight: 700, color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.4rem' }}>{label}</div>
      <div style={{ fontSize: isCount ? '2rem' : '1.4rem', fontWeight: 800, color }}>{value}</div>
    </div>
  );
}
