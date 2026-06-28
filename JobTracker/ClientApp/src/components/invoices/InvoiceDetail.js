import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STATUS_META = {
  draft:   { label: 'Draft',   color: '#6c757d' },
  sent:    { label: 'Sent',    color: '#0d6efd' },
  partial: { label: 'Partial', color: '#fd7e14' },
  paid:    { label: 'Paid',    color: '#198754' },
  void:    { label: 'Void',    color: '#adb5bd' },
};

const PAYMENT_METHODS = ['check', 'cash', 'card', 'bank_transfer', 'other'];

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

export default function InvoiceDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isNew = id === 'new';

  const emptyForm = {
    clientName: '', clientAddress: '', clientEmail: '', clientPhone: '',
    invoiceDate: toInputDate(new Date()), dueDate: toInputDate(new Date(Date.now() + 30 * 86400000)),
    taxRate: '0', notes: '', terms: 'Payment due within 30 days.',
    lineItems: [{ description: '', quantity: '1', unitPrice: '' }],
  };

  const [invoice, setInvoice] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [editMode, setEditMode] = useState(isNew);
  const [saving, setSaving] = useState(false);
  const [showPayment, setShowPayment] = useState(false);
  const [payForm, setPayForm] = useState({ amount: '', method: 'check', reference: '', paidAt: toInputDate(new Date()), notes: '' });

  const fetchInvoice = useCallback(async () => {
    if (isNew) return;
    try {
      const data = await apiService.invoices.getById(id);
      setInvoice(data);
      setForm({
        clientName: data.clientName || '', clientAddress: data.clientAddress || '',
        clientEmail: data.clientEmail || '', clientPhone: data.clientPhone || '',
        invoiceDate: toInputDate(data.invoiceDate), dueDate: toInputDate(data.dueDate),
        taxRate: String(Math.round((data.taxRate || 0) * 10000) / 100),
        notes: data.notes || '', terms: data.terms || '',
        lineItems: (data.lineItems || []).map(li => ({
          description: li.description, quantity: String(li.quantity), unitPrice: String(li.unitPrice)
        })),
      });
    } catch (e) { console.error(e); }
  }, [id, isNew]);

  useEffect(() => { fetchInvoice(); }, [fetchInvoice]);

  const calcTotals = () => {
    const subtotal = form.lineItems.reduce((sum, li) => {
      const qty = parseFloat(li.quantity) || 0;
      const price = parseFloat(li.unitPrice) || 0;
      return sum + (qty * price);
    }, 0);
    const taxRate = parseFloat(form.taxRate) / 100 || 0;
    const taxAmount = subtotal * taxRate;
    const total = subtotal + taxAmount;
    return { subtotal, taxAmount, total };
  };

  const { subtotal, taxAmount, total } = calcTotals();

  const setLineItem = (idx, field, value) => {
    setForm(f => {
      const items = [...f.lineItems];
      items[idx] = { ...items[idx], [field]: value };
      return { ...f, lineItems: items };
    });
  };
  const addLineItem = () => setForm(f => ({ ...f, lineItems: [...f.lineItems, { description: '', quantity: '1', unitPrice: '' }] }));
  const removeLineItem = (idx) => setForm(f => ({ ...f, lineItems: f.lineItems.filter((_, i) => i !== idx) }));

  const buildPayload = () => ({
    ...form,
    taxRate: parseFloat(form.taxRate) || 0,
    invoiceDate: form.invoiceDate || undefined,
    dueDate: form.dueDate || undefined,
    lineItems: form.lineItems
      .filter(li => li.description.trim())
      .map(li => ({ description: li.description, quantity: parseFloat(li.quantity) || 1, unitPrice: parseFloat(li.unitPrice) || 0 })),
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
    if (!window.confirm('Mark this invoice as Sent?')) return;
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
        method: payForm.method,
        reference: payForm.reference,
        paidAt: payForm.paidAt || undefined,
        notes: payForm.notes,
      });
      setShowPayment(false);
      setPayForm({ amount: '', method: 'check', reference: '', paidAt: toInputDate(new Date()), notes: '' });
      fetchInvoice();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const st = invoice?.status || 'draft';
  const meta = STATUS_META[st] || STATUS_META.draft;

  // ── New / Edit form ──
  if (editMode) {
    return (
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <button onClick={() => isNew ? navigate('/invoices') : setEditMode(false)} style={linkBtnStyle}>← Back</button>
            <h2 style={{ margin: '0.25rem 0 0' }}>{isNew ? 'New Invoice' : `Edit ${invoice?.invoiceNumber}`}</h2>
          </div>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {!isNew && <button className="btn" onClick={() => setEditMode(false)} disabled={saving}>Cancel</button>}
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : isNew ? 'Create Invoice' : 'Save Changes'}</button>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
          {/* Left — client + dates */}
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={sectionTitle}>Bill To</div>
            <FormRow label="Client Name"><input className="form-control" value={form.clientName} onChange={e => setForm(f => ({ ...f, clientName: e.target.value }))} /></FormRow>
            <FormRow label="Address"><input className="form-control" value={form.clientAddress} onChange={e => setForm(f => ({ ...f, clientAddress: e.target.value }))} /></FormRow>
            <FormRow label="Email"><input type="email" className="form-control" value={form.clientEmail} onChange={e => setForm(f => ({ ...f, clientEmail: e.target.value }))} /></FormRow>
            <FormRow label="Phone"><input className="form-control" value={form.clientPhone} onChange={e => setForm(f => ({ ...f, clientPhone: e.target.value }))} /></FormRow>
          </div>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={sectionTitle}>Invoice Details</div>
            <FormRow label="Invoice Date"><input type="date" className="form-control" value={form.invoiceDate} onChange={e => setForm(f => ({ ...f, invoiceDate: e.target.value }))} /></FormRow>
            <FormRow label="Due Date"><input type="date" className="form-control" value={form.dueDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} /></FormRow>
            <FormRow label="Tax Rate (%)"><input type="number" className="form-control" min="0" max="100" step="0.01" value={form.taxRate} onChange={e => setForm(f => ({ ...f, taxRate: e.target.value }))} /></FormRow>
          </div>
        </div>

        {/* Line items */}
        <div className="card" style={{ padding: '1.25rem', marginTop: '1rem' }}>
          <div style={{ ...sectionTitle, marginBottom: '0.75rem' }}>Line Items</div>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa' }}>
                <th style={thStyle}>Description</th>
                <th style={{ ...thStyle, width: 90 }}>Qty</th>
                <th style={{ ...thStyle, width: 130 }}>Unit Price</th>
                <th style={{ ...thStyle, width: 120, textAlign: 'right' }}>Total</th>
                <th style={{ ...thStyle, width: 40 }}></th>
              </tr>
            </thead>
            <tbody>
              {form.lineItems.map((li, idx) => {
                const lineTotal = (parseFloat(li.quantity) || 0) * (parseFloat(li.unitPrice) || 0);
                return (
                  <tr key={idx}>
                    <td style={{ padding: '0.4rem' }}><input className="form-control" value={li.description} onChange={e => setLineItem(idx, 'description', e.target.value)} placeholder="Description of work or material" /></td>
                    <td style={{ padding: '0.4rem' }}><input type="number" className="form-control" min="0" step="0.01" value={li.quantity} onChange={e => setLineItem(idx, 'quantity', e.target.value)} /></td>
                    <td style={{ padding: '0.4rem' }}><input type="number" className="form-control" min="0" step="0.01" value={li.unitPrice} onChange={e => setLineItem(idx, 'unitPrice', e.target.value)} placeholder="0.00" /></td>
                    <td style={{ padding: '0.4rem', textAlign: 'right', fontWeight: 600 }}>{fmtMoney(lineTotal)}</td>
                    <td style={{ padding: '0.4rem', textAlign: 'center' }}>
                      {form.lineItems.length > 1 && <button onClick={() => removeLineItem(idx)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: '1.1rem' }}>×</button>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <button onClick={addLineItem} style={{ ...linkBtnStyle, marginTop: '0.5rem' }}>+ Add Line Item</button>

          {/* Totals */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1rem' }}>
            <div style={{ width: 260 }}>
              <TotalRow label="Subtotal" value={fmtMoney(subtotal)} />
              {parseFloat(form.taxRate) > 0 && <TotalRow label={`Tax (${form.taxRate}%)`} value={fmtMoney(taxAmount)} />}
              <TotalRow label="Total" value={fmtMoney(total)} bold />
            </div>
          </div>
        </div>

        {/* Notes & Terms */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginTop: '1rem' }}>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={sectionTitle}>Notes</div>
            <textarea className="form-control" rows={4} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} placeholder="Visible to client..." />
          </div>
          <div className="card" style={{ padding: '1.25rem' }}>
            <div style={sectionTitle}>Terms & Conditions</div>
            <textarea className="form-control" rows={4} value={form.terms} onChange={e => setForm(f => ({ ...f, terms: e.target.value }))} />
          </div>
        </div>
      </div>
    );
  }

  // ── View mode ──
  if (!invoice && !isNew) return <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>;

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <button onClick={() => navigate('/invoices')} style={linkBtnStyle}>← Back to Invoices</button>
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
          {invoice.status !== 'paid' && invoice.status !== 'void' && (
            <button className="btn" onClick={handleVoid} style={{ color: '#dc3545', borderColor: '#dc3545' }}>Void</button>
          )}
        </div>
      </div>

      {/* Invoice card */}
      <div className="card" style={{ padding: '2rem' }}>
        {/* Header row */}
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '2rem', borderBottom: '2px solid #dee2e6', paddingBottom: '1.5rem' }}>
          <div>
            <div style={{ fontSize: '1.5rem', fontWeight: 800, color: '#FF9500' }}>PROVIA</div>
            <div style={{ color: '#6c757d', fontSize: '0.85rem' }}>Enterprise Construction Management</div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: '1.2rem', fontWeight: 700, color: '#2F5A7E' }}>INVOICE</div>
            <div style={{ fontFamily: 'monospace', fontWeight: 600 }}>{invoice.invoiceNumber}</div>
            <div style={{ fontSize: '0.85rem', color: '#6c757d', marginTop: 4 }}>Date: {fmtDate(invoice.invoiceDate)}</div>
            <div style={{ fontSize: '0.85rem', color: '#6c757d' }}>Due: {fmtDate(invoice.dueDate)}</div>
          </div>
        </div>

        {/* Bill to */}
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={sectionTitle}>Bill To</div>
          {invoice.clientName && <div style={{ fontWeight: 600 }}>{invoice.clientName}</div>}
          {invoice.clientAddress && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientAddress}</div>}
          {invoice.clientEmail && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientEmail}</div>}
          {invoice.clientPhone && <div style={{ color: '#6c757d', fontSize: '0.9rem' }}>{invoice.clientPhone}</div>}
        </div>

        {/* Line items table */}
        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '1rem' }}>
          <thead>
            <tr style={{ background: '#2F5A7E', color: '#fff' }}>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem' }}>Description</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontWeight: 600, fontSize: '0.85rem', width: 80 }}>Qty</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontWeight: 600, fontSize: '0.85rem', width: 130 }}>Unit Price</th>
              <th style={{ padding: '0.6rem 1rem', textAlign: 'right', fontWeight: 600, fontSize: '0.85rem', width: 130 }}>Total</th>
            </tr>
          </thead>
          <tbody>
            {(invoice.lineItems || []).map((li, i) => (
              <tr key={li.id} style={{ background: i % 2 === 0 ? '#fff' : '#f8f9fa', borderBottom: '1px solid #dee2e6' }}>
                <td style={{ padding: '0.65rem 1rem', fontSize: '0.9rem' }}>{li.description}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontSize: '0.9rem' }}>{li.quantity}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontSize: '0.9rem' }}>{fmtMoney(li.unitPrice)}</td>
                <td style={{ padding: '0.65rem 1rem', textAlign: 'right', fontWeight: 600 }}>{fmtMoney(li.total)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Totals */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '1.5rem' }}>
          <div style={{ width: 280, borderTop: '2px solid #dee2e6', paddingTop: '0.75rem' }}>
            <TotalRow label="Subtotal" value={fmtMoney(invoice.subtotal)} />
            {invoice.taxRate > 0 && <TotalRow label={`Tax (${Math.round(invoice.taxRate * 10000) / 100}%)`} value={fmtMoney(invoice.taxAmount)} />}
            <TotalRow label="Total" value={fmtMoney(invoice.total)} bold />
            <TotalRow label="Amount Paid" value={fmtMoney(invoice.amountPaid)} color="#198754" />
            <TotalRow label="Balance Due" value={fmtMoney(invoice.balanceDue)} bold color={invoice.balanceDue > 0 ? '#dc3545' : '#198754'} />
          </div>
        </div>

        {/* Notes & Terms */}
        {(invoice.notes || invoice.terms) && (
          <div style={{ display: 'grid', gridTemplateColumns: invoice.notes && invoice.terms ? '1fr 1fr' : '1fr', gap: '1rem', borderTop: '1px solid #dee2e6', paddingTop: '1rem', fontSize: '0.85rem', color: '#6c757d' }}>
            {invoice.notes && <div><div style={{ fontWeight: 700, color: '#495057', marginBottom: 4 }}>Notes</div>{invoice.notes}</div>}
            {invoice.terms && <div><div style={{ fontWeight: 700, color: '#495057', marginBottom: 4 }}>Terms</div>{invoice.terms}</div>}
          </div>
        )}
      </div>

      {/* Payment history */}
      {invoice.payments?.length > 0 && (
        <div className="card" style={{ padding: '1.25rem', marginTop: '1rem' }}>
          <div style={sectionTitle}>Payment History</div>
          {invoice.payments.map(p => (
            <div key={p.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.5rem 0', borderBottom: '1px solid #f0f0f0' }}>
              <div>
                <span style={{ fontWeight: 600, color: '#198754' }}>{fmtMoney(p.amount)}</span>
                <span style={{ marginLeft: 8, fontSize: '0.85rem', color: '#6c757d', textTransform: 'capitalize' }}>{p.method.replace('_', ' ')}</span>
                {p.reference && <span style={{ marginLeft: 8, fontSize: '0.8rem', color: '#aaa' }}>Ref: {p.reference}</span>}
              </div>
              <div style={{ fontSize: '0.82rem', color: '#6c757d' }}>{fmtDate(p.paidAt)} · {p.recordedBy}</div>
            </div>
          ))}
        </div>
      )}

      {/* Record Payment Modal */}
      {showPayment && (
        <div style={overlayStyle}>
          <div style={{ ...modalStyle, maxWidth: 420 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ margin: 0 }}>Record Payment</h4>
              <button onClick={() => setShowPayment(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handlePayment}>
              <FormRow label="Amount *">
                <input type="number" className="form-control" required min="0.01" step="0.01" value={payForm.amount}
                  onChange={e => setPayForm(f => ({ ...f, amount: e.target.value }))} placeholder={fmtMoney(invoice.balanceDue)} />
              </FormRow>
              <FormRow label="Method">
                <select className="form-control" value={payForm.method} onChange={e => setPayForm(f => ({ ...f, method: e.target.value }))}>
                  {PAYMENT_METHODS.map(m => <option key={m} value={m}>{m.replace('_', ' ').replace(/\b\w/g, c => c.toUpperCase())}</option>)}
                </select>
              </FormRow>
              <FormRow label="Date">
                <input type="date" className="form-control" value={payForm.paidAt} onChange={e => setPayForm(f => ({ ...f, paidAt: e.target.value }))} />
              </FormRow>
              <FormRow label="Reference / Check #">
                <input className="form-control" value={payForm.reference} onChange={e => setPayForm(f => ({ ...f, reference: e.target.value }))} />
              </FormRow>
              <FormRow label="Notes">
                <input className="form-control" value={payForm.notes} onChange={e => setPayForm(f => ({ ...f, notes: e.target.value }))} />
              </FormRow>
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

function FormRow({ label, children }) {
  return (
    <div style={{ marginBottom: '0.75rem' }}>
      <label style={{ fontWeight: 600, fontSize: '0.8rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '0.2rem', display: 'block' }}>{label}</label>
      {children}
    </div>
  );
}

function TotalRow({ label, value, bold, color }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '3px 0', fontSize: bold ? '1rem' : '0.9rem', fontWeight: bold ? 700 : 400, color: color || 'inherit', borderTop: bold ? '1px solid #dee2e6' : 'none', marginTop: bold ? 4 : 0 }}>
      <span>{label}</span><span>{value}</span>
    </div>
  );
}

const overlayStyle = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle = { background: '#fff', borderRadius: 8, padding: '1.75rem', width: '100%', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const sectionTitle = { fontWeight: 700, fontSize: '0.78rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.6rem' };
const linkBtnStyle = { background: 'none', border: 'none', color: '#2F5A7E', cursor: 'pointer', padding: 0, fontSize: '0.9rem' };
const thStyle = { padding: '0.6rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.82rem' };
