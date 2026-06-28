import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STATUS_META = {
  draft:   { label: 'Draft',   color: '#6c757d' },
  sent:    { label: 'Sent',    color: '#0d6efd' },
  partial: { label: 'Partial', color: '#fd7e14' },
  paid:    { label: 'Paid',    color: '#198754' },
  overdue: { label: 'Overdue', color: '#dc3545' },
  void:    { label: 'Void',    color: '#adb5bd' },
};

const PAYMENT_METHODS = ['check', 'cash', 'card', 'bank_transfer', 'zelle', 'other'];

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

// Parse lineItemsJson string into array
function parseLineItems(json) {
  try { return JSON.parse(json || '[]'); } catch { return []; }
}

// Build lineItemsJson from form rows
function buildLineItemsJson(rows) {
  return JSON.stringify(rows.map(r => ({
    description: r.description,
    quantity: parseFloat(r.quantity) || 1,
    unitPrice: parseFloat(r.unitPrice) || 0,
    amount: Math.round((parseFloat(r.quantity) || 1) * (parseFloat(r.unitPrice) || 0) * 100) / 100,
  })));
}

const emptyForm = {
  clientName: '', clientAddress: '', clientEmail: '', clientPhone: '',
  issuedAt: toInputDate(new Date()),
  dueAt: toInputDate(new Date(Date.now() + 30 * 86400000)),
  taxRate: '0', notes: '', terms: 'Payment due within 30 days.',
  rows: [{ description: '', quantity: '1', unitPrice: '' }],
};

export default function InvoiceDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isNew = id === 'new';

  const [invoice, setInvoice] = useState(null);
  const [contextJobId, setContextJobId] = useState(null);
  const [contextContactId, setContextContactId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [editMode, setEditMode] = useState(isNew);
  const [saving, setSaving] = useState(false);
  const [showPayment, setShowPayment] = useState(false);
  const [payForm, setPayForm] = useState({ amount: '', method: 'check', reference: '', paidAt: toInputDate(new Date()), notes: '' });

  useEffect(() => {
    if (!isNew) return;
    const params = new URLSearchParams(location.search);
    const jId = params.get('jobId');
    const cId = params.get('contactId');
    const name = params.get('clientName') || '';
    const email = params.get('clientEmail') || '';
    if (jId) setContextJobId(parseInt(jId, 10));
    if (cId) setContextContactId(parseInt(cId, 10));
    if (name || email) {
      setForm(f => ({ ...f, clientName: name, clientEmail: email }));
    }
  }, [isNew, location.search]);

  const fetchInvoice = useCallback(async () => {
    if (isNew) return;
    try {
      const data = await apiService.invoices.getById(id);
      setInvoice(data);
      const rows = parseLineItems(data.lineItemsJson).map(li => ({
        description: li.description || '',
        quantity: String(li.quantity ?? 1),
        unitPrice: String(li.unitPrice ?? li.amount ?? 0),
      }));
      setForm({
        clientName: data.clientName || '', clientAddress: data.clientAddress || '',
        clientEmail: data.clientEmail || '', clientPhone: data.clientPhone || '',
        issuedAt: toInputDate(data.issuedAt), dueAt: toInputDate(data.dueAt),
        taxRate: String(data.taxRate || 0),
        notes: data.notes || '', terms: data.terms || '',
        rows: rows.length ? rows : [{ description: '', quantity: '1', unitPrice: '' }],
      });
    } catch (e) { console.error(e); }
  }, [id, isNew]);

  useEffect(() => { fetchInvoice(); }, [fetchInvoice]);

  const calcTotals = () => {
    const subtotal = form.rows.reduce((sum, r) => sum + (parseFloat(r.quantity) || 0) * (parseFloat(r.unitPrice) || 0), 0);
    const taxAmount = subtotal * (parseFloat(form.taxRate) / 100 || 0);
    return { subtotal, taxAmount, total: subtotal + taxAmount };
  };
  const { subtotal, taxAmount, total } = calcTotals();

  const setRow = (idx, field, val) => setForm(f => {
    const rows = [...f.rows]; rows[idx] = { ...rows[idx], [field]: val }; return { ...f, rows };
  });
  const addRow = () => setForm(f => ({ ...f, rows: [...f.rows, { description: '', quantity: '1', unitPrice: '' }] }));
  const removeRow = (idx) => setForm(f => ({ ...f, rows: f.rows.filter((_, i) => i !== idx) }));

  const buildPayload = () => ({
    clientName: form.clientName, clientAddress: form.clientAddress,
    clientEmail: form.clientEmail, clientPhone: form.clientPhone,
    issuedAt: form.issuedAt || undefined, dueAt: form.dueAt || undefined,
    taxRate: parseFloat(form.taxRate) || 0,
    notes: form.notes, terms: form.terms,
    lineItemsJson: buildLineItemsJson(form.rows.filter(r => r.description.trim())),
    ...(contextJobId ? { jobId: contextJobId } : {}),
    ...(contextContactId ? { contactId: contextContactId } : {}),
  });

  const handleSave = async () => {
    try {
      setSaving(true);
      if (isNew) {
        const result = await apiService.invoices.create(buildPayload());
        navigate(`/invoices/${result.id}`);
      } else {
        await apiService.invoices.update(id, buildPayload());
        setEditMode(false);
        fetchInvoice();
      }
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleSend = async () => {
    if (!window.confirm('Mark as Sent?')) return;
    try { await apiService.invoices.send(id); fetchInvoice(); }
    catch (e) { alert('Failed: ' + e.message); }
  };

  const handleVoid = async () => {
    if (!window.confirm('Void this invoice? This cannot be undone.')) return;
    try { await apiService.invoices.void(id); fetchInvoice(); }
    catch (e) { alert('Failed: ' + e.message); }
  };

  const handlePayment = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      await apiService.invoices.recordPayment(id, {
        amount: parseFloat(payForm.amount),
        method: payForm.method, reference: payForm.reference,
        paidAt: payForm.paidAt || undefined, notes: payForm.notes,
      });
      setShowPayment(false);
      setPayForm({ amount: '', method: 'check', reference: '', paidAt: toInputDate(new Date()), notes: '' });
      fetchInvoice();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  // ── Edit / New form ──
  if (editMode) {
    return (
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <button onClick={() => isNew ? navigate('/invoices') : setEditMode(false)} style={linkBtn}>← Back</button>
            <h2 style={{ margin: '0.25rem 0 0' }}>{isNew ? 'New Invoice' : `Edit ${invoice?.invoiceNumber}`}</h2>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {!isNew && <button className="btn" onClick={() => setEditMode(false)} disabled={saving}>Cancel</button>}
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : isNew ? 'Create Invoice' : 'Save'}</button>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Bill To</div>
            <FR label="Client Name"><input className="form-control" value={form.clientName} onChange={e => setForm(f => ({ ...f, clientName: e.target.value }))} /></FR>
            <FR label="Address"><input className="form-control" value={form.clientAddress} onChange={e => setForm(f => ({ ...f, clientAddress: e.target.value }))} /></FR>
            <FR label="Email"><input type="email" className="form-control" value={form.clientEmail} onChange={e => setForm(f => ({ ...f, clientEmail: e.target.value }))} /></FR>
            <FR label="Phone"><input className="form-control" value={form.clientPhone} onChange={e => setForm(f => ({ ...f, clientPhone: e.target.value }))} /></FR>
          </div>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Invoice Details</div>
            <FR label="Issue Date"><input type="date" className="form-control" value={form.issuedAt} onChange={e => setForm(f => ({ ...f, issuedAt: e.target.value }))} /></FR>
            <FR label="Due Date"><input type="date" className="form-control" value={form.dueAt} onChange={e => setForm(f => ({ ...f, dueAt: e.target.value }))} /></FR>
            <FR label="Tax Rate (%)"><input type="number" className="form-control" min="0" max="100" step="0.01" value={form.taxRate} onChange={e => setForm(f => ({ ...f, taxRate: e.target.value }))} /></FR>
          </div>
        </div>

        <div className="card" style={{ padding: '1.25rem', marginBottom: '1rem' }}>
          <div style={{ ...secTitle, marginBottom: '0.75rem' }}>Line Items</div>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa' }}>
                <th style={th}>Description</th>
                <th style={{ ...th, width: 90 }}>Qty</th>
                <th style={{ ...th, width: 130 }}>Unit Price</th>
                <th style={{ ...th, width: 120, textAlign: 'right' }}>Amount</th>
                <th style={{ ...th, width: 40 }}></th>
              </tr>
            </thead>
            <tbody>
              {form.rows.map((r, idx) => {
                const amt = (parseFloat(r.quantity) || 0) * (parseFloat(r.unitPrice) || 0);
                return (
                  <tr key={idx}>
                    <td style={{ padding: '0.4rem' }}><input className="form-control" value={r.description} onChange={e => setRow(idx, 'description', e.target.value)} placeholder="Description of work or material" /></td>
                    <td style={{ padding: '0.4rem' }}><input type="number" className="form-control" min="0" step="0.01" value={r.quantity} onChange={e => setRow(idx, 'quantity', e.target.value)} /></td>
                    <td style={{ padding: '0.4rem' }}><input type="number" className="form-control" min="0" step="0.01" value={r.unitPrice} onChange={e => setRow(idx, 'unitPrice', e.target.value)} placeholder="0.00" /></td>
                    <td style={{ padding: '0.4rem', textAlign: 'right', fontWeight: 600 }}>{fmtMoney(amt)}</td>
                    <td style={{ padding: '0.4rem', textAlign: 'center' }}>
                      {form.rows.length > 1 && <button onClick={() => removeRow(idx)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: '1.1rem' }}>×</button>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <button onClick={addRow} style={{ ...linkBtn, marginTop: '0.5rem' }}>+ Add Line Item</button>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1rem' }}>
            <div style={{ width: 260 }}>
              <TR label="Subtotal" value={fmtMoney(subtotal)} />
              {parseFloat(form.taxRate) > 0 && <TR label={`Tax (${form.taxRate}%)`} value={fmtMoney(taxAmount)} />}
              <TR label="Total" value={fmtMoney(total)} bold />
            </div>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Notes</div>
            <textarea className="form-control" rows={4} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} placeholder="Visible to client..." />
          </div>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={secTitle}>Terms</div>
            <textarea className="form-control" rows={4} value={form.terms} onChange={e => setForm(f => ({ ...f, terms: e.target.value }))} />
          </div>
        </div>
      </div>
    );
  }

  // ── View mode ──
  if (!invoice) return <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>;

  const st = invoice.status || 'draft';
  const meta = STATUS_META[st] || STATUS_META.draft;
  const lineItems = parseLineItems(invoice.lineItemsJson);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <button onClick={() => navigate('/invoices')} style={linkBtn}>← Back to Invoices</button>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '0.25rem' }}>
            <h2 style={{ margin: 0 }}>{invoice.invoiceNumber}</h2>
            <span style={{ padding: '3px 10px', borderRadius: 12, fontSize: '0.82rem', fontWeight: 700, background: meta.color + '22', color: meta.color }}>{meta.label}</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          {invoice.status === 'draft' && <button className="btn" onClick={() => setEditMode(true)}>Edit</button>}
          {(invoice.status === 'draft' || invoice.status === 'sent') && (
            <button className="btn btn-primary" onClick={handleSend} style={{ background: '#0d6efd', border: 'none' }}>Mark as Sent</button>
          )}
          {invoice.status !== 'paid' && invoice.status !== 'void' && (
            <button className="btn btn-primary" onClick={() => setShowPayment(true)} style={{ background: '#198754', border: 'none' }}>Record Payment</button>
          )}
          {invoice.status !== 'void' && (
            <button className="btn" onClick={handleVoid} style={{ color: '#dc3545', borderColor: '#dc3545' }}>Void</button>
          )}
        </div>
      </div>

      <div className="card" style={{ padding: '2rem', marginBottom: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem', borderBottom: '2px solid #dee2e6', paddingBottom: '1.5rem' }}>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#FF9500' }}>PROVIA</div>
            <div style={{ color: '#6c757d', fontSize: '0.85rem' }}>Enterprise Construction Management</div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: '1.2rem', fontWeight: 700, color: '#2F5A7E' }}>INVOICE</div>
            <div style={{ fontFamily: 'monospace', fontWeight: 600 }}>{invoice.invoiceNumber}</div>
            <div style={{ fontSize: '0.85rem', color: '#6c757d', marginTop: 4 }}>Issued: {fmtDate(invoice.issuedAt)}</div>
            <div style={{ fontSize: '0.85rem', color: '#6c757d' }}>Due: {fmtDate(invoice.dueAt)}</div>
          </div>
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <div style={secTitle}>Bill To</div>
          {invoice.clientName    && <div style={{ fontWeight: 600 }}>{invoice.clientName}</div>}
          {invoice.clientAddress && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientAddress}</div>}
          {invoice.clientEmail   && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientEmail}</div>}
          {invoice.clientPhone   && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientPhone}</div>}
        </div>

        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '1rem' }}>
          <thead>
            <tr style={{ background: '#2F5A7E', color: '#fff' }}>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'left', fontSize: '0.85rem' }}>Description</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontSize: '0.85rem', width: 80 }}>Qty</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontSize: '0.85rem', width: 130 }}>Unit Price</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontSize: '0.85rem', width: 130 }}>Amount</th>
            </tr>
          </thead>
          <tbody>
            {lineItems.map((li, i) => (
              <tr key={i} style={{ background: i % 2 === 0 ? '#fff' : '#f8f9fa', borderBottom: '1px solid #dee2e6' }}>
                <td style={{ padding: '0.65rem 1rem', fontSize: '0.9rem' }}>{li.description}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontSize: '0.9rem' }}>{li.quantity}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontSize: '0.9rem' }}>{fmtMoney(li.unitPrice)}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontWeight: 600 }}>{fmtMoney(li.amount)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '1.5rem' }}>
          <div style={{ width: 280, borderTop: '2px solid #dee2e6', paddingTop: '0.75rem' }}>
            <TR label="Subtotal" value={fmtMoney(invoice.subtotal)} />
            {invoice.taxRate > 0 && <TR label={`Tax (${invoice.taxRate}%)`} value={fmtMoney(invoice.taxAmount)} />}
            <TR label="Total" value={fmtMoney(invoice.total)} bold />
            <TR label="Amount Paid" value={fmtMoney(invoice.amountPaid)} color="#198754" />
            <TR label="Balance Due" value={fmtMoney(invoice.balanceDue)} bold color={invoice.balanceDue > 0 ? '#dc3545' : '#198754'} />
          </div>
        </div>

        {(invoice.notes || invoice.terms) && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', borderTop: '1px solid #dee2e6', paddingTop: '1rem', fontSize: '0.85rem', color: '#6c757d' }}>
            {invoice.notes && <div><div style={{ fontWeight: 700, color: '#495057', marginBottom: 4 }}>Notes</div>{invoice.notes}</div>}
            {invoice.terms && <div><div style={{ fontWeight: 700, color: '#495057', marginBottom: 4 }}>Terms</div>{invoice.terms}</div>}
          </div>
        )}
      </div>

      {invoice.payments?.length > 0 && (
        <div className="card" style={{ padding: '1.25rem', marginBottom: '1rem' }}>
          <div style={secTitle}>Payment History</div>
          {invoice.payments.map(p => (
            <div key={p.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '0.5rem 0', borderBottom: '1px solid #f0f0f0' }}>
              <div>
                <span style={{ fontWeight: 600, color: '#198754' }}>{fmtMoney(p.amount)}</span>
                <span style={{ marginLeft: 8, fontSize: '0.85rem', color: '#6c757d', textTransform: 'capitalize' }}>{p.method?.replace('_', ' ')}</span>
                {p.reference && <span style={{ marginLeft: 8, fontSize: '0.8rem', color: '#aaa' }}>#{p.reference}</span>}
              </div>
              <div style={{ fontSize: '0.82rem', color: '#6c757d' }}>{fmtDate(p.paidAt)} · {p.recordedBy}</div>
            </div>
          ))}
        </div>
      )}

      {showPayment && (
        <div style={overlay}>
          <div style={{ ...modal, maxWidth: 400 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ margin: 0 }}>Record Payment</h4>
              <button onClick={() => setShowPayment(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handlePayment}>
              <FR label="Amount *"><input type="number" className="form-control" required min="0.01" step="0.01" value={payForm.amount} onChange={e => setPayForm(f => ({ ...f, amount: e.target.value }))} placeholder={fmtMoney(invoice.balanceDue)} /></FR>
              <FR label="Method">
                <select className="form-control" value={payForm.method} onChange={e => setPayForm(f => ({ ...f, method: e.target.value }))}>
                  {PAYMENT_METHODS.map(m => <option key={m} value={m}>{m.replace('_', ' ').replace(/\b\w/g, c => c.toUpperCase())}</option>)}
                </select>
              </FR>
              <FR label="Date"><input type="date" className="form-control" value={payForm.paidAt} onChange={e => setPayForm(f => ({ ...f, paidAt: e.target.value }))} /></FR>
              <FR label="Reference / Check #"><input className="form-control" value={payForm.reference} onChange={e => setPayForm(f => ({ ...f, reference: e.target.value }))} /></FR>
              <FR label="Notes"><input className="form-control" value={payForm.notes} onChange={e => setPayForm(f => ({ ...f, notes: e.target.value }))} /></FR>
              <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end', marginTop: '1rem' }}>
                <button type="button" className="btn" onClick={() => setShowPayment(false)}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving} style={{ background: '#198754', border: 'none' }}>
                  {saving ? 'Saving...' : 'Record Payment'}
                </button>
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
      <label style={{ fontWeight: 600, fontSize: '0.8rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '0.2rem', display: 'block' }}>{label}</label>
      {children}
    </div>
  );
}

function TR({ label, value, bold, color }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '3px 0', fontSize: bold ? '1rem' : '0.9rem', fontWeight: bold ? 700 : 400, color: color || 'inherit', borderTop: bold ? '1px solid #dee2e6' : 'none', marginTop: bold ? 4 : 0 }}>
      <span>{label}</span><span>{value}</span>
    </div>
  );
}

const overlay = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modal   = { background: '#fff', borderRadius: 8, padding: '1.75rem', width: '100%', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const secTitle = { fontWeight: 700, fontSize: '0.78rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.6rem' };
const linkBtn  = { background: 'none', border: 'none', color: '#2F5A7E', cursor: 'pointer', padding: 0, fontSize: '0.9rem' };
const th = { padding: '0.6rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.82rem' };
