import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const CATEGORIES = ['all', 'lumber', 'electrical', 'plumbing', 'concrete', 'roofing', 'tools', 'safety', 'equipment', 'finishes', 'general'];

const CAT_COLORS = {
  lumber:      { bg: '#fff3e0', color: '#e65100' },
  electrical:  { bg: '#fffde7', color: '#f57f17' },
  plumbing:    { bg: '#e3f2fd', color: '#1565c0' },
  concrete:    { bg: '#f3e5f5', color: '#6a1b9a' },
  roofing:     { bg: '#fce4ec', color: '#b71c1c' },
  tools:       { bg: '#e8f5e9', color: '#2e7d32' },
  safety:      { bg: '#ffebee', color: '#c62828' },
  equipment:   { bg: '#e0f7fa', color: '#00695c' },
  finishes:    { bg: '#f9fbe7', color: '#558b2f' },
  general:     { bg: '#f5f5f5', color: '#424242' },
};

function fmtMoney(n) {
  return `$${Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
}

export default function VendorList() {
  const [vendors, setVendors] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [activeOnly, setActiveOnly] = useState(false);
  const navigate = useNavigate();

  const fetchAll = useCallback(async () => {
    try {
      setLoading(true);
      const [vens, sum] = await Promise.all([
        apiService.vendors.getAll({ category, search, activeOnly }),
        apiService.vendors.getSummary(),
      ]);
      setVendors(vens);
      setSummary(sum);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }, [category, search, activeOnly]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Vendors</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>Supplier and material vendor directory</p>
        </div>
        <button className="btn btn-primary" onClick={() => navigate('/vendors/new')}>+ Add Vendor</button>
      </div>

      {summary && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
          <SCard label="Total Vendors"   value={summary.totalVendors}   color="#2F5A7E" />
          <SCard label="Active"          value={summary.activeVendors}  color="#198754" />
          <SCard label="Preferred"       value={summary.preferredCount} color="#FF9500" />
          <SCard label="Total Spend"     value={fmtMoney(summary.totalSpend)} color="#6c757d" money />
          <SCard label="Open Orders"     value={summary.pendingOrders}  color="#dc3545" />
        </div>
      )}

      <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap' }}>
          {CATEGORIES.map(c => (
            <button key={c} onClick={() => setCategory(c)} style={{
              padding: '4px 12px', borderRadius: 20, border: '1px solid', cursor: 'pointer',
              fontSize: '0.8rem', fontWeight: 600, textTransform: 'capitalize',
              background: category === c ? '#2F5A7E' : '#fff',
              color: category === c ? '#fff' : '#6c757d',
              borderColor: category === c ? '#2F5A7E' : '#dee2e6',
            }}>{c === 'all' ? 'All' : c}</button>
          ))}
        </div>
        <input className="form-control" placeholder="Search name, contact..." value={search}
          onChange={e => setSearch(e.target.value)} style={{ maxWidth: 240 }} />
        <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer', fontSize: '0.85rem', color: '#495057', whiteSpace: 'nowrap' }}>
          <input type="checkbox" checked={activeOnly} onChange={e => setActiveOnly(e.target.checked)} />
          Active only
        </label>
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        {loading ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                {['Vendor', 'Category', 'Contact', 'Location', 'Terms', 'Orders', 'Spend', 'Status', ''].map(h => (
                  <th key={h} style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.82rem', color: '#495057' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {vendors.length === 0 && (
                <tr><td colSpan={9} style={{ padding: '2rem', textAlign: 'center', color: '#aaa' }}>No vendors found</td></tr>
              )}
              {vendors.map((v, i) => {
                const catStyle = CAT_COLORS[v.category] || CAT_COLORS.general;
                return (
                  <tr key={v.id} style={{ borderBottom: '1px solid #dee2e6', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
                        {v.isPreferred && <span title="Preferred" style={{ color: '#FF9500', fontSize: '0.9rem' }}>★</span>}
                        <div>
                          <div style={{ fontWeight: 700, fontSize: '0.9rem' }}>{v.name}</div>
                          <div style={{ fontFamily: 'monospace', fontSize: '0.75rem', color: '#6c757d' }}>{v.vendorNumber}</div>
                        </div>
                      </div>
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      {v.category && (
                        <span style={{ padding: '3px 9px', borderRadius: 12, fontSize: '0.75rem', fontWeight: 700, textTransform: 'capitalize', background: catStyle.bg, color: catStyle.color }}>{v.category}</span>
                      )}
                    </td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem' }}>
                      <div>{v.contactName || '—'}</div>
                      {v.phone && <div style={{ color: '#6c757d', fontSize: '0.8rem' }}>{v.phone}</div>}
                    </td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem', color: '#6c757d' }}>
                      {v.city && v.state ? `${v.city}, ${v.state}` : v.city || v.state || '—'}
                    </td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.82rem', color: '#6c757d', textTransform: 'capitalize' }}>
                      {v.paymentTerms?.replace('net', 'Net ') || '—'}
                    </td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem', color: '#495057' }}>{v.purchaseCount}</td>
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600, fontSize: '0.85rem' }}>{fmtMoney(v.totalSpend)}</td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <span style={{
                        padding: '3px 9px', borderRadius: 12, fontSize: '0.75rem', fontWeight: 700,
                        background: v.isActive ? '#e6f7ee' : '#f8f9fa',
                        color: v.isActive ? '#198754' : '#6c757d',
                      }}>{v.isActive ? 'Active' : 'Inactive'}</span>
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <button onClick={() => navigate(`/vendors/${v.id}`)} className="btn btn-sm" style={{ fontSize: '0.8rem', padding: '3px 10px' }}>Open</button>
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

function SCard({ label, value, color, money }) {
  return (
    <div className="card" style={{ padding: '1rem 1.25rem' }}>
      <div style={{ fontSize: '0.75rem', fontWeight: 700, color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.4rem' }}>{label}</div>
      <div style={{ fontSize: money ? '1.2rem' : '2rem', fontWeight: 800, color }}>{value}</div>
    </div>
  );
}
