import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const CATEGORIES = ['lumber', 'electrical', 'plumbing', 'concrete', 'roofing', 'tools', 'safety', 'equipment', 'finishes', 'general'];
const PAYMENT_TERMS = ['net30', 'net60', 'net90', 'cod', 'prepaid', 'credit_card'];
const PURCHASE_STATUSES = ['pending', 'ordered', 'received', 'invoiced', 'paid'];

const STATUS_META = {
  pending:  { label: 'Pending',  color: '#6c757d' },
  ordered:  { label: 'Ordered',  color: '#0d6efd' },
  received: { label: 'Received', color: '#fd7e14' },
  invoiced: { label: 'Invoiced', color: '#6f42c1' },
  paid:     { label: 'Paid',     color: '#198754' },
};

function fmtMoney(n) {
  return `$${Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
function fmtDate(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
function toInputDate(d) {
  if (!d) return '';
  return new Date(d).toISOString().slice(0, 10);
}

const emptyForm = {
  name: '', category: 'general', contactName: '', email: '', phone: '',
  address: '', city: '', state: 'MA', zipCode: '', website: '',
  accountNumber: '', paymentTerms: 'net30', creditLimit: '', notes: '', isPreferred: false,
};

const emptyPurchase = {
  description: '', amount: '', purchaseOrderNumber: '', status: 'pending',
  orderedAt: toInputDate(new Date()), receivedAt: '', jobId: '',
};

export default function VendorDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isNew = id === 'new';

  const [vendor, setVendor] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [editMode, setEditMode] = useState(isNew);
  const [saving, setSaving] = useState(false);
  const [showPurchase, setShowPurchase] = useState(false);
  const [purchaseForm, setPurchaseForm] = useState(emptyPurchase);
  const [activeTab, setActiveTab] = useState('purchases');

  const fetchVendor = useCallback(async () => {
    if (isNew) return;
    try {
      const data = await apiService.vendors.getById(id);
      setVendor(data);
      setForm({
        name: data.name || '', category: data.category || 'general',
        contactName: data.contactName || '', email: data.email || '', phone: data.phone || '',
        address: data.address || '', city: data.city || '', state: data.state || 'MA',
        zipCode: data.zipCode || '', website: data.website || '',
        accountNumber: data.accountNumber || '', paymentTerms: data.paymentTerms || 'net30',
        creditLimit: data.creditLimit ?? '', notes: data.notes || '',
        isPreferred: data.isPreferred || false,
      });
    } catch (e) { console.error(e); }
  }, [id, isNew]);

  useEffect(() => { fetchVendor(); }, [fetchVendor]);

  const handleSave = async () => {
    if (!form.name.trim()) return alert('Vendor name is required.');
    try {
      setSaving(true);
      const payload = { ...form, creditLimit: form.creditLimit ? parseFloat(form.creditLimit) : null };
      if (isNew) {
        const result = await apiService.vendors.create(payload);
        navigate(`/vendors/${result.id}`);
      } else {
        await apiService.vendors.update(id, payload);
        setEditMode(false);
        fetchVendor();
      }
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleToggleActive = async () => {
    if (!vendor) return;
    const action = vendor.isActive ? 'deactivate' : 'activate';
    if (!window.confirm(`${vendor.isActive ? 'Deactivate' : 'Activate'} this vendor?`)) return;
    try {
      await (vendor.isActive ? apiService.vendors.deactivate(id) : apiService.vendors.activate(id));
      fetchVendor();
    } catch (e) { alert('Failed: ' + e.message); }
  };

  const handleDelete = async () => {
    if (!window.confirm('Delete this vendor? This cannot be undone.')) return;
    try {
      await apiService.vendors.delete(id);
      navigate('/vendors');
    } catch (e) { alert(e.message); }
  };

  const handleAddPurchase = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      await apiService.vendors.addPurchase(id, {
        ...purchaseForm,
        amount: parseFloat(purchaseForm.amount),
        jobId: purchaseForm.jobId ? parseInt(purchaseForm.jobId) : null,
        orderedAt: purchaseForm.orderedAt || null,
        receivedAt: purchaseForm.receivedAt || null,
      });
      setShowPurchase(false);
      setPurchaseForm(emptyPurchase);
      fetchVendor();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleDeletePurchase = async (purchaseId) => {
    if (!window.confirm('Delete this purchase record?')) return;
    try {
      await apiService.vendors.deletePurchase(id, purchaseId);
      fetchVendor();
    } catch (e) { alert('Failed: ' + e.message); }
  };

  // ── Edit / New form ──
  if (editMode) {
    return (
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <button onClick={() => isNew ? navigate('/vendors') : setEditMode(false)} style={linkBtn}>← Back</button>
            <h2 style={{ margin: '0.25rem 0 0' }}>{isNew ? 'New Vendor' : `Edit ${vendor?.name}`}</h2>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {!isNew && <button className="btn" onClick={() => setEditMode(false)} disabled={saving}>Cancel</button>}
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : isNew ? 'Create Vendor' : 'Save Changes'}</button>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Vendor Info</div>
            <FR label="Vendor Name *"><input className="form-control" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} /></FR>
            <FR label="Category">
              <select className="form-control" value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))}>
                {CATEGORIES.map(c => <option key={c} value={c}>{c.charAt(0).toUpperCase() + c.slice(1)}</option>)}
              </select>
            </FR>
            <FR label="Account Number"><input className="form-control" value={form.accountNumber} onChange={e => setForm(f => ({ ...f, accountNumber: e.target.value }))} /></FR>
            <FR label="Website"><input className="form-control" value={form.website} onChange={e => setForm(f => ({ ...f, website: e.target.value }))} /></FR>
            <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', marginTop: '0.5rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.9rem' }}>
                <input type="checkbox" checked={form.isPreferred} onChange={e => setForm(f => ({ ...f, isPreferred: e.target.checked }))} />
                <span>★ Mark as Preferred Vendor</span>
              </label>
            </div>
          </div>

          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Contact</div>
            <FR label="Contact Name"><input className="form-control" value={form.contactName} onChange={e => setForm(f => ({ ...f, contactName: e.target.value }))} /></FR>
            <FR label="Email"><input type="email" className="form-control" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} /></FR>
            <FR label="Phone"><input className="form-control" value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} /></FR>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Address</div>
            <FR label="Street"><input className="form-control" value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} /></FR>
            <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gap: '0.5rem' }}>
              <FR label="City"><input className="form-control" value={form.city} onChange={e => setForm(f => ({ ...f, city: e.target.value }))} /></FR>
              <FR label="State"><input className="form-control" value={form.state} onChange={e => setForm(f => ({ ...f, state: e.target.value }))} /></FR>
              <FR label="ZIP"><input className="form-control" value={form.zipCode} onChange={e => setForm(f => ({ ...f, zipCode: e.target.value }))} /></FR>
            </div>
          </div>

          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Payment Terms</div>
            <FR label="Payment Terms">
              <select className="form-control" value={form.paymentTerms} onChange={e => setForm(f => ({ ...f, paymentTerms: e.target.value }))}>
                {PAYMENT_TERMS.map(t => <option key={t} value={t}>{t.replace('net', 'Net ').replace('_', ' ').replace(/\b\w/g, c => c.toUpperCase())}</option>)}
              </select>
            </FR>
            <FR label="Credit Limit ($)"><input type="number" className="form-control" min="0" step="100" value={form.creditLimit} onChange={e => setForm(f => ({ ...f, creditLimit: e.target.value }))} placeholder="Optional" /></FR>
          </div>
        </div>

        <div className="card" style={{ padding: '1.25rem' }}>
          <div style={secTitle}>Notes</div>
          <textarea className="form-control" rows={4} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} placeholder="Internal notes about this vendor..." />
        </div>
      </div>
    );
  }

  // ── View mode ──
  if (!vendor && !isNew) return <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>;

  const purchases = vendor?.purchases || [];
  const totalSpend = purchases.reduce((sum, p) => sum + (p.amount || 0), 0);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <button onClick={() => navigate('/vendors')} style={linkBtn}>← Back to Vendors</button>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '0.25rem' }}>
            {vendor?.isPreferred && <span style={{ color: '#FF9500', fontSize: '1.1rem' }}>★</span>}
            <h2 style={{ margin: 0 }}>{vendor?.name}</h2>
            <span style={{ fontFamily: 'monospace', fontSize: '0.8rem', color: '#6c757d' }}>{vendor?.vendorNumber}</span>
            {vendor?.category && (
              <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: '0.78rem', fontWeight: 700, textTransform: 'capitalize', background: '#e8f4fd', color: '#2F5A7E' }}>{vendor.category}</span>
            )}
            <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: '0.75rem', fontWeight: 700, background: vendor?.isActive ? '#e6f7ee' : '#f8f9fa', color: vendor?.isActive ? '#198754' : '#6c757d' }}>
              {vendor?.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <button className="btn" onClick={() => setEditMode(true)}>Edit</button>
          <button className="btn btn-primary" onClick={() => setShowPurchase(true)} style={{ background: '#FF9500', border: 'none' }}>+ Log Purchase</button>
          <button className="btn" onClick={handleToggleActive} style={{ color: vendor?.isActive ? '#dc3545' : '#198754', borderColor: vendor?.isActive ? '#dc3545' : '#198754' }}>
            {vendor?.isActive ? 'Deactivate' : 'Activate'}
          </button>
          {!purchases.length && (
            <button className="btn" onClick={handleDelete} style={{ color: '#dc3545', borderColor: '#dc3545' }}>Delete</button>
          )}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
        <div className="card" style={{ padding: '1.25rem' }}>
          <div style={secTitle}>Contact</div>
          <InfoRow label="Name"    value={vendor?.contactName} />
          <InfoRow label="Email"   value={vendor?.email} isEmail />
          <InfoRow label="Phone"   value={vendor?.phone} />
          <InfoRow label="Website" value={vendor?.website} isLink />
        </div>
        <div className="card" style={{ padding: '1.25rem' }}>
          <div style={secTitle}>Address</div>
          {vendor?.address && <div style={{ fontSize: '0.9rem' }}>{vendor.address}</div>}
          {(vendor?.city || vendor?.state) && <div style={{ fontSize: '0.9rem', color: '#6c757d' }}>{[vendor.city, vendor.state, vendor.zipCode].filter(Boolean).join(', ')}</div>}
          {!vendor?.address && !vendor?.city && <span style={{ color: '#aaa', fontSize: '0.85rem' }}>—</span>}
        </div>
        <div className="card" style={{ padding: '1.25rem' }}>
          <div style={secTitle}>Payment</div>
          <InfoRow label="Terms"      value={vendor?.paymentTerms?.replace('net', 'Net ')?.replace('_', ' ')} />
          <InfoRow label="Account #"  value={vendor?.accountNumber} />
          <InfoRow label="Credit Limit" value={vendor?.creditLimit ? fmtMoney(vendor.creditLimit) : null} />
          <div style={{ marginTop: '0.75rem', paddingTop: '0.75rem', borderTop: '1px solid #dee2e6' }}>
            <div style={{ fontSize: '0.75rem', color: '#6c757d', fontWeight: 700, textTransform: 'uppercase' }}>Total Spend</div>
            <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#2F5A7E' }}>{fmtMoney(totalSpend)}</div>
          </div>
        </div>
      </div>

      {vendor?.notes && (
        <div className="card" style={{ padding: '1rem 1.25rem', marginBottom: '1rem', borderLeft: '4px solid #FF9500' }}>
          <div style={secTitle}>Notes</div>
          <div style={{ fontSize: '0.9rem', color: '#495057', whiteSpace: 'pre-wrap' }}>{vendor.notes}</div>
        </div>
      )}

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ padding: '1rem 1.25rem', borderBottom: '1px solid #dee2e6', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ fontWeight: 700, color: '#495057' }}>Purchase History ({purchases.length})</div>
          <button className="btn btn-sm btn-primary" onClick={() => setShowPurchase(true)} style={{ background: '#FF9500', border: 'none', fontSize: '0.82rem' }}>+ Log Purchase</button>
        </div>
        {purchases.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#aaa' }}>No purchases logged yet</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '1px solid #dee2e6' }}>
                {['PO #', 'Description', 'Job', 'Amount', 'Status', 'Ordered', 'Received', ''].map(h => (
                  <th key={h} style={{ padding: '0.6rem 1rem', textAlign: 'left', fontSize: '0.8rem', fontWeight: 600, color: '#6c757d' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {purchases.map((p, i) => {
                const sm = STATUS_META[p.status] || STATUS_META.pending;
                return (
                  <tr key={p.id} style={{ borderBottom: '1px solid #f0f0f0', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                    <td style={{ padding: '0.65rem 1rem', fontFamily: 'monospace', fontSize: '0.8rem', color: '#6c757d' }}>{p.purchaseOrderNumber || '—'}</td>
                    <td style={{ padding: '0.65rem 1rem', fontSize: '0.88rem', maxWidth: 260 }}>{p.description || '—'}</td>
                    <td style={{ padding: '0.65rem 1rem', fontSize: '0.82rem', color: '#2F5A7E' }}>{p.jobNumber || '—'}</td>
                    <td style={{ padding: '0.65rem 1rem', fontWeight: 700 }}>{fmtMoney(p.amount)}</td>
                    <td style={{ padding: '0.65rem 1rem' }}>
                      <span style={{ padding: '2px 8px', borderRadius: 10, fontSize: '0.75rem', fontWeight: 700, background: sm.color + '22', color: sm.color }}>{sm.label}</span>
                    </td>
                    <td style={{ padding: '0.65rem 1rem', fontSize: '0.82rem', color: '#6c757d' }}>{fmtDate(p.orderedAt)}</td>
                    <td style={{ padding: '0.65rem 1rem', fontSize: '0.82rem', color: '#6c757d' }}>{fmtDate(p.receivedAt)}</td>
                    <td style={{ padding: '0.65rem 1rem' }}>
                      <button onClick={() => handleDeletePurchase(p.id)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: '0.8rem' }}>Delete</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {showPurchase && (
        <div style={overlay}>
          <div style={{ ...modal, maxWidth: 480 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ margin: 0 }}>Log Purchase from {vendor?.name}</h4>
              <button onClick={() => setShowPurchase(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handleAddPurchase}>
              <FR label="Description"><textarea className="form-control" rows={2} value={purchaseForm.description} onChange={e => setPurchaseForm(f => ({ ...f, description: e.target.value }))} placeholder="What was purchased?" /></FR>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                <FR label="Amount *"><input type="number" className="form-control" required min="0.01" step="0.01" value={purchaseForm.amount} onChange={e => setPurchaseForm(f => ({ ...f, amount: e.target.value }))} /></FR>
                <FR label="PO Number"><input className="form-control" value={purchaseForm.purchaseOrderNumber} onChange={e => setPurchaseForm(f => ({ ...f, purchaseOrderNumber: e.target.value }))} /></FR>
              </div>
              <FR label="Status">
                <select className="form-control" value={purchaseForm.status} onChange={e => setPurchaseForm(f => ({ ...f, status: e.target.value }))}>
                  {PURCHASE_STATUSES.map(s => <option key={s} value={s}>{s.charAt(0).toUpperCase() + s.slice(1)}</option>)}
                </select>
              </FR>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                <FR label="Order Date"><input type="date" className="form-control" value={purchaseForm.orderedAt} onChange={e => setPurchaseForm(f => ({ ...f, orderedAt: e.target.value }))} /></FR>
                <FR label="Received Date"><input type="date" className="form-control" value={purchaseForm.receivedAt} onChange={e => setPurchaseForm(f => ({ ...f, receivedAt: e.target.value }))} /></FR>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
                <button type="button" className="btn" onClick={() => setShowPurchase(false)}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving} style={{ background: '#FF9500', border: 'none' }}>{saving ? 'Saving...' : 'Log Purchase'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function FR({ label, children }) {
  return (
    <div style={{ marginBottom: '0.75rem' }}>
      <label style={{ fontWeight: 600, fontSize: '0.78rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '0.2rem', display: 'block' }}>{label}</label>
      {children}
    </div>
  );
}

function InfoRow({ label, value, isEmail, isLink }) {
  if (!value) return null;
  return (
    <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.4rem', fontSize: '0.88rem' }}>
      <span style={{ color: '#6c757d', minWidth: 80 }}>{label}:</span>
      {isEmail ? <a href={`mailto:${value}`} style={{ color: '#2F5A7E' }}>{value}</a>
        : isLink ? <a href={value.startsWith('http') ? value : `https://${value}`} target="_blank" rel="noopener noreferrer" style={{ color: '#2F5A7E' }}>{value}</a>
        : <span style={{ fontWeight: 500 }}>{value}</span>}
    </div>
  );
}

const overlay = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modal   = { background: '#fff', borderRadius: 8, padding: '1.75rem', width: '100%', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const secTitle = { fontWeight: 700, fontSize: '0.78rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.6rem' };
const linkBtn  = { background: 'none', border: 'none', color: '#2F5A7E', cursor: 'pointer', padding: 0, fontSize: '0.9rem' };
