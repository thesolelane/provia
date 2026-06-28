import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STATUS_META = {
  draft:   { label: 'Draft',   color: '#6c757d' },
  sent:    { label: 'Sent',    color: '#0d6efd' },
  partial: { label: 'Partial', color: '#fd7e14' },
  paid:    { label: 'Paid',    color: '#198754' },
  overdue: { label: 'Overdue', color: '#dc3545' },
  void:    { label: 'Void',    color: '#adb5bd' },
};

const PAYMENT_METHODS = ['check', 'cash', 'credit_card', 'ach', 'zelle', 'bank_transfer', 'other'];

function fmtMoney(n) {
  return `$${Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
function fmtDate(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function LineItemEditor({ items, onChange }) {
  const addItem = () => onChange([...items, { description: '', quantity: 1, unitPrice: 0, amount: 0 }]);

  const updateItem = (idx, field, value) => {
    const updated = items.map((item, i) => {
      if (i !== idx) return item;
      const next = { ...item, [field]: value };
      if (field === 'quantity' || field === 'unitPrice') {
        const q = field === 'quantity' ? parseFloat(value) || 0 : parseFloat(item.quantity) || 0;
        const u = field === 'unitPrice' ? parseFloat(value) || 0 : parseFloat(item.unitPrice) || 0;
        next.amount = parseFloat((q * u).toFixed(2));
      }
      return next;
    });
    onChange(updated);
  };

  const removeItem = (idx) => onChange(items.filter((_, i) => i !== idx));

  return (
    <div>
      <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 12 }}>
        <thead>
          <tr style={{ background: '#f8f9fa' }}>
            <th style={{ padding: '8px 10px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Description</th>
            <th style={{ padding: '8px 10px', textAlign: 'center', fontSize: 12, color: '#666', fontWeight: 600, width: 80 }}>Qty</th>
            <th style={{ padding: '8px 10px', textAlign: 'right', fontSize: 12, color: '#666', fontWeight: 600, width: 120 }}>Unit Price</th>
            <th style={{ padding: '8px 10px', textAlign: 'right', fontSize: 12, color: '#666', fontWeight: 600, width: 110 }}>Amount</th>
            <th style={{ width: 36 }}></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item, idx) => (
            <tr key={idx} style={{ borderBottom: '1px solid #f0f0f0' }}>
              <td style={{ padding: '6px 8px' }}>
                <input
                  value={item.description || ''}
                  onChange={e => updateItem(idx, 'description', e.target.value)}
                  style={{ width: '100%', padding: '6px 8px', border: '1px solid #e0e0e0', borderRadius: 4, fontSize: 14, boxSizing: 'border-box' }}
                  placeholder="Item description…"
                />
              </td>
              <td style={{ padding: '6px 8px' }}>
                <input type="number" min="0" step="0.01"
                  value={item.quantity || ''}
                  onChange={e => updateItem(idx, 'quantity', e.target.value)}
                  style={{ width: '100%', padding: '6px 8px', border: '1px solid #e0e0e0', borderRadius: 4, fontSize: 14, textAlign: 'center', boxSizing: 'border-box' }}
                />
              </td>
              <td style={{ padding: '6px 8px' }}>
                <input type="number" min="0" step="0.01"
                  value={item.unitPrice || ''}
                  onChange={e => updateItem(idx, 'unitPrice', e.target.value)}
                  style={{ width: '100%', padding: '6px 8px', border: '1px solid #e0e0e0', borderRadius: 4, fontSize: 14, textAlign: 'right', boxSizing: 'border-box' }}
                />
              </td>
              <td style={{ padding: '6px 8px', textAlign: 'right', fontSize: 14, fontWeight: 500 }}>
                {fmtMoney(item.amount || 0)}
              </td>
              <td style={{ padding: '6px 4px' }}>
                <button onClick={() => removeItem(idx)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: 18, padding: '0 4px', lineHeight: 1 }}>×</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <button onClick={addItem} style={{ background: 'none', border: '1px dashed #aaa', borderRadius: 4, padding: '6px 16px', color: '#555', cursor: 'pointer', fontSize: 13 }}>
        + Add Line Item
      </button>
    </div>
  );
}

export default function InvoiceDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editMode, setEditMode] = useState(false);
  const [lineItems, setLineItems] = useState([]);
  const [editForm, setEditForm] = useState({});
  const [saving, setSaving] = useState(false);
  const [actionMsg, setActionMsg] = useState('');
  const [actionError, setActionError] = useState('');
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [paymentForm, setPaymentForm] = useState({ amount: '', method: 'check', paidAt: new Date().toISOString().slice(0, 10), reference: '', notes: '' });
  const [paymentSaving, setPaymentSaving] = useState(false);

  const fetchInvoice = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiService.invoices.getById(id);
      setInvoice(data);
      let items = [];
      try { items = JSON.parse(data.lineItemsJson || '[]'); } catch (e) { items = []; }
      setLineItems(items);
      setEditForm({
        clientName: data.clientName || '',
        clientEmail: data.clientEmail || '',
        clientAddress: data.clientAddress || '',
        clientPhone: data.clientPhone || '',
        notes: data.notes || '',
        terms: data.terms || '',
        taxRate: data.taxRate || 0,
        issuedAt: data.issuedAt ? data.issuedAt.slice(0, 10) : '',
        dueAt: data.dueAt ? data.dueAt.slice(0, 10) : '',
      });
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { fetchInvoice(); }, [fetchInvoice]);

  const flash = (msg, isError = false) => {
    if (isError) setActionError(msg); else setActionMsg(msg);
    setTimeout(() => { setActionMsg(''); setActionError(''); }, 4000);
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await apiService.invoices.update(id, {
        ...editForm,
        issuedAt: editForm.issuedAt ? new Date(editForm.issuedAt).toISOString() : null,
        dueAt: editForm.dueAt ? new Date(editForm.dueAt).toISOString() : null,
        lineItemsJson: JSON.stringify(lineItems),
        taxRate: parseFloat(editForm.taxRate) || 0,
      });
      setEditMode(false);
      flash('Invoice saved');
      fetchInvoice();
    } catch (e) {
      flash(e.message || 'Save failed', true);
    } finally {
      setSaving(false);
    }
  };

  const handleSend = async () => {
    if (!invoice.clientEmail) { flash('Add a client email before sending', true); return; }
    if (!window.confirm(`Send invoice ${invoice.invoiceNumber} to ${invoice.clientEmail}?`)) return;
    try {
      const result = await apiService.invoices.send(id);
      flash(result.message || 'Invoice sent');
      fetchInvoice();
    } catch (e) {
      flash(e.message || 'Failed to send', true);
    }
  };

  const handleVoid = async () => {
    if (!window.confirm('Void this invoice? This cannot be undone.')) return;
    try {
      await apiService.invoices.void(id);
      flash('Invoice voided');
      fetchInvoice();
    } catch (e) {
      flash(e.message || 'Failed to void', true);
    }
  };

  const handleDelete = async () => {
    if (!window.confirm('Delete this invoice permanently?')) return;
    try {
      await apiService.invoices.delete(id);
      navigate('/invoices');
    } catch (e) {
      flash(e.message || 'Failed to delete', true);
    }
  };

  const handleAddPayment = async (e) => {
    e.preventDefault();
    setPaymentSaving(true);
    try {
      await apiService.invoices.addPayment(id, {
        amount: parseFloat(paymentForm.amount),
        method: paymentForm.method,
        paidAt: paymentForm.paidAt ? new Date(paymentForm.paidAt).toISOString() : null,
        reference: paymentForm.reference,
        notes: paymentForm.notes,
      });
      setShowPaymentModal(false);
      setPaymentForm({ amount: '', method: 'check', paidAt: new Date().toISOString().slice(0, 10), reference: '', notes: '' });
      flash('Payment recorded');
      fetchInvoice();
    } catch (e) {
      flash(e.message || 'Failed to record payment', true);
    } finally {
      setPaymentSaving(false);
    }
  };

  const handleDeletePayment = async (paymentId) => {
    if (!window.confirm('Remove this payment?')) return;
    try {
      await apiService.invoices.deletePayment(id, paymentId);
      flash('Payment removed');
      fetchInvoice();
    } catch (e) {
      flash(e.message || 'Failed to remove payment', true);
    }
  };

  const handlePrint = () => window.open(`/api/invoices/${id}/print`, '_blank');

  if (loading) return <div style={{ textAlign: 'center', padding: 60, color: '#888' }}>Loading invoice…</div>;
  if (!invoice) return <div style={{ textAlign: 'center', padding: 60, color: '#888' }}>Invoice not found.</div>;

  const canEdit = invoice.status !== 'void';
  const canSend = invoice.status !== 'void' && invoice.status !== 'paid';
  const canVoid = invoice.status !== 'void';
  const canDelete = invoice.status === 'draft' || invoice.status === 'void';
  const isOverdue = invoice.isOverdue && invoice.status !== 'paid' && invoice.status !== 'void';
  const displayStatus = isOverdue && invoice.status === 'sent' ? 'overdue' : invoice.status;
  const meta = STATUS_META[displayStatus] || STATUS_META.draft;

  const subtotal = lineItems.reduce((s, i) => s + (parseFloat(i.amount) || 0), 0);
  const taxRate = parseFloat(editMode ? editForm.taxRate : invoice.taxRate) || 0;
  const taxAmount = parseFloat((subtotal * taxRate / 100).toFixed(2));
  const total = subtotal + taxAmount;

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: '24px 16px' }}>
      {/* Breadcrumb */}
      <div style={{ marginBottom: 16, fontSize: 14 }}>
        <Link to="/invoices" style={{ color: '#2F5A7E', textDecoration: 'none' }}>← Invoices</Link>
      </div>

      {/* Flash messages */}
      {actionMsg && <div style={{ background: '#d4edda', border: '1px solid #c3e6cb', color: '#155724', padding: '10px 16px', borderRadius: 6, marginBottom: 16, fontSize: 14 }}>{actionMsg}</div>}
      {actionError && <div style={{ background: '#f8d7da', border: '1px solid #f5c6cb', color: '#721c24', padding: '10px 16px', borderRadius: 6, marginBottom: 16, fontSize: 14 }}>{actionError}</div>}

      {/* Header */}
      <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 12, padding: '24px 28px', marginBottom: 20, boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <div style={{ fontSize: 22, fontWeight: 700, color: '#2F5A7E' }}>{invoice.invoiceNumber}</div>
            <div style={{ fontSize: 14, color: '#888', marginTop: 4 }}>
              Created {fmtDate(invoice.createdAt)}
              {invoice.jobNumber && <span> · Job <Link to={`/jobs/${invoice.jobId}`} style={{ color: '#2F5A7E' }}>{invoice.jobNumber}</Link></span>}
            </div>
          </div>
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', alignItems: 'center' }}>
            <span style={{ background: meta.color + '22', color: meta.color, padding: '5px 14px', borderRadius: 20, fontSize: 12, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.5 }}>
              {meta.label}
            </span>
            <button onClick={handlePrint} style={{ padding: '8px 14px', background: '#f0f0f0', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>🖨 Print</button>
            {canSend && <button onClick={handleSend} style={{ padding: '8px 14px', background: '#0d6efd', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>📧 Send</button>}
            {canEdit && !editMode && <button onClick={() => setEditMode(true)} style={{ padding: '8px 14px', background: '#2F5A7E', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>Edit</button>}
            {editMode && (
              <>
                <button onClick={handleSave} disabled={saving} style={{ padding: '8px 14px', background: '#198754', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>{saving ? 'Saving…' : 'Save'}</button>
                <button onClick={() => { setEditMode(false); fetchInvoice(); }} style={{ padding: '8px 14px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13 }}>Cancel</button>
              </>
            )}
          </div>
        </div>

        {/* Client & dates */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 20, marginTop: 24 }}>
          <div>
            <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 6 }}>Bill To</div>
            {editMode ? (
              <>
                <input value={editForm.clientName} onChange={e => setEditForm(f => ({ ...f, clientName: e.target.value }))} placeholder="Client name" style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, marginBottom: 6, boxSizing: 'border-box' }} />
                <input value={editForm.clientEmail} onChange={e => setEditForm(f => ({ ...f, clientEmail: e.target.value }))} placeholder="Email" style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, marginBottom: 6, boxSizing: 'border-box' }} />
                <input value={editForm.clientPhone} onChange={e => setEditForm(f => ({ ...f, clientPhone: e.target.value }))} placeholder="Phone" style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, marginBottom: 6, boxSizing: 'border-box' }} />
                <input value={editForm.clientAddress} onChange={e => setEditForm(f => ({ ...f, clientAddress: e.target.value }))} placeholder="Address" style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, boxSizing: 'border-box' }} />
              </>
            ) : (
              <>
                <div style={{ fontWeight: 600 }}>{invoice.clientName || '—'}</div>
                {invoice.clientEmail && <div style={{ fontSize: 13, color: '#555' }}>{invoice.clientEmail}</div>}
                {invoice.clientPhone && <div style={{ fontSize: 13, color: '#555' }}>{invoice.clientPhone}</div>}
                {invoice.clientAddress && <div style={{ fontSize: 13, color: '#555' }}>{invoice.clientAddress}</div>}
              </>
            )}
          </div>
          <div>
            <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 6 }}>Issue Date</div>
            {editMode
              ? <input type="date" value={editForm.issuedAt} onChange={e => setEditForm(f => ({ ...f, issuedAt: e.target.value }))} style={{ padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14 }} />
              : <div style={{ fontWeight: 500 }}>{fmtDate(invoice.issuedAt)}</div>}
          </div>
          <div>
            <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 6 }}>Due Date</div>
            {editMode
              ? <input type="date" value={editForm.dueAt} onChange={e => setEditForm(f => ({ ...f, dueAt: e.target.value }))} style={{ padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14 }} />
              : <div style={{ fontWeight: 500, color: isOverdue ? '#dc3545' : '#333' }}>{fmtDate(invoice.dueAt)}</div>}
          </div>
        </div>

        {/* Notes & Terms */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginTop: 20 }}>
          <div>
            <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 6 }}>Notes</div>
            {editMode
              ? <textarea value={editForm.notes} onChange={e => setEditForm(f => ({ ...f, notes: e.target.value }))} rows={2} style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, resize: 'vertical', boxSizing: 'border-box' }} placeholder="Notes visible to client…" />
              : <div style={{ fontSize: 14, color: '#444' }}>{invoice.notes || <span style={{ color: '#aaa' }}>—</span>}</div>}
          </div>
          <div>
            <div style={{ fontSize: 11, color: '#888', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 6 }}>Terms</div>
            {editMode
              ? <textarea value={editForm.terms} onChange={e => setEditForm(f => ({ ...f, terms: e.target.value }))} rows={2} style={{ width: '100%', padding: '7px 10px', border: '1px solid #ddd', borderRadius: 5, fontSize: 14, resize: 'vertical', boxSizing: 'border-box' }} placeholder="Payment terms…" />
              : <div style={{ fontSize: 14, color: '#444' }}>{invoice.terms || <span style={{ color: '#aaa' }}>—</span>}</div>}
          </div>
        </div>
      </div>

      {/* Line Items */}
      <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 12, padding: '24px 28px', marginBottom: 20, boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
        <h3 style={{ margin: '0 0 18px', color: '#2F5A7E', fontSize: 16 }}>Line Items</h3>
        {editMode ? (
          <LineItemEditor items={lineItems} onChange={setLineItems} />
        ) : lineItems.length === 0 ? (
          <div style={{ color: '#aaa', fontStyle: 'italic', fontSize: 14 }}>No line items yet. Click Edit to add items.</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa' }}>
                <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Description</th>
                <th style={{ padding: '8px 12px', textAlign: 'center', fontSize: 12, color: '#666', fontWeight: 600, width: 80 }}>Qty</th>
                <th style={{ padding: '8px 12px', textAlign: 'right', fontSize: 12, color: '#666', fontWeight: 600, width: 120 }}>Unit Price</th>
                <th style={{ padding: '8px 12px', textAlign: 'right', fontSize: 12, color: '#666', fontWeight: 600, width: 110 }}>Amount</th>
              </tr>
            </thead>
            <tbody>
              {lineItems.map((item, i) => (
                <tr key={i} style={{ borderBottom: '1px solid #f0f0f0' }}>
                  <td style={{ padding: '10px 12px', fontSize: 14 }}>{item.description || '—'}</td>
                  <td style={{ padding: '10px 12px', fontSize: 14, textAlign: 'center' }}>{item.quantity}</td>
                  <td style={{ padding: '10px 12px', fontSize: 14, textAlign: 'right' }}>{fmtMoney(item.unitPrice)}</td>
                  <td style={{ padding: '10px 12px', fontSize: 14, textAlign: 'right', fontWeight: 500 }}>{fmtMoney(item.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {/* Totals */}
        <div style={{ marginTop: 20, display: 'flex', justifyContent: 'flex-end' }}>
          <div style={{ width: 280 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', fontSize: 14 }}>
              <span style={{ color: '#666' }}>Subtotal</span>
              <span>{fmtMoney(editMode ? subtotal : invoice.subtotal)}</span>
            </div>
            {editMode ? (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '6px 0', fontSize: 14 }}>
                <span style={{ color: '#666' }}>Tax Rate (%)</span>
                <input type="number" min="0" max="100" step="0.1" value={editForm.taxRate} onChange={e => setEditForm(f => ({ ...f, taxRate: e.target.value }))} style={{ width: 80, padding: '4px 8px', border: '1px solid #ddd', borderRadius: 4, fontSize: 14, textAlign: 'right' }} />
              </div>
            ) : (invoice.taxRate > 0) && (
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', fontSize: 14 }}>
                <span style={{ color: '#666' }}>Tax ({invoice.taxRate}%)</span>
                <span>{fmtMoney(invoice.taxAmount)}</span>
              </div>
            )}
            {editMode && taxRate > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', fontSize: 14 }}>
                <span style={{ color: '#666' }}>Tax ({taxRate}%)</span>
                <span>{fmtMoney(taxAmount)}</span>
              </div>
            )}
            {invoice.amountPaid > 0 && (
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0', fontSize: 14 }}>
                <span style={{ color: '#198754' }}>Amount Paid</span>
                <span style={{ color: '#198754' }}>-{fmtMoney(invoice.amountPaid)}</span>
              </div>
            )}
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 12px', background: '#2F5A7E', color: '#fff', borderRadius: 6, fontSize: 15, fontWeight: 700, marginTop: 6 }}>
              <span>Balance Due</span>
              <span>{fmtMoney(editMode ? total : invoice.balanceDue)}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Payments */}
      <div style={{ background: '#fff', border: '1px solid #e8e8e8', borderRadius: 12, padding: '24px 28px', marginBottom: 20, boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
          <h3 style={{ margin: 0, color: '#2F5A7E', fontSize: 16 }}>Payments</h3>
          {invoice.status !== 'void' && (
            <button onClick={() => setShowPaymentModal(true)} style={{ background: '#198754', color: '#fff', border: 'none', borderRadius: 6, padding: '8px 16px', cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>+ Record Payment</button>
          )}
        </div>

        {!invoice.payments || invoice.payments.length === 0 ? (
          <div style={{ color: '#aaa', fontStyle: 'italic', fontSize: 14 }}>No payments recorded yet.</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa' }}>
                <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Date</th>
                <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Method</th>
                <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Reference</th>
                <th style={{ padding: '8px 12px', textAlign: 'right', fontSize: 12, color: '#666', fontWeight: 600 }}>Amount</th>
                <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 12, color: '#666', fontWeight: 600 }}>Recorded By</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {invoice.payments.map(p => (
                <tr key={p.id} style={{ borderBottom: '1px solid #f0f0f0' }}>
                  <td style={{ padding: '10px 12px', fontSize: 14 }}>{fmtDate(p.paidAt)}</td>
                  <td style={{ padding: '10px 12px', fontSize: 13 }}><span style={{ background: '#f0f4f8', padding: '2px 8px', borderRadius: 4, textTransform: 'capitalize' }}>{p.method.replace(/_/g, ' ')}</span></td>
                  <td style={{ padding: '10px 12px', fontSize: 13, color: '#555' }}>{p.reference || '—'}</td>
                  <td style={{ padding: '10px 12px', fontSize: 14, fontWeight: 600, textAlign: 'right', color: '#198754' }}>{fmtMoney(p.amount)}</td>
                  <td style={{ padding: '10px 12px', fontSize: 13, color: '#888' }}>{p.recordedBy || '—'}</td>
                  <td style={{ padding: '10px 12px' }}>
                    <button onClick={() => handleDeletePayment(p.id)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: 13 }}>Remove</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Danger zone */}
      <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end', marginTop: 8 }}>
        {canVoid && <button onClick={handleVoid} style={{ padding: '8px 18px', background: 'none', border: '1px solid #dc3545', color: '#dc3545', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>Void Invoice</button>}
        {canDelete && <button onClick={handleDelete} style={{ padding: '8px 18px', background: '#dc3545', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 500 }}>Delete Invoice</button>}
      </div>

      {/* Payment Modal */}
      {showPaymentModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }} onClick={() => setShowPaymentModal(false)}>
          <div style={{ background: '#fff', borderRadius: 12, padding: 32, width: '100%', maxWidth: 440, boxShadow: '0 8px 40px rgba(0,0,0,0.18)' }} onClick={e => e.stopPropagation()}>
            <h3 style={{ margin: '0 0 16px', color: '#2F5A7E' }}>Record Payment</h3>
            <div style={{ background: '#f0f4f8', borderRadius: 6, padding: '10px 14px', marginBottom: 16, fontSize: 14 }}>
              Balance Due: <strong>{fmtMoney(invoice.balanceDue)}</strong>
            </div>
            <form onSubmit={handleAddPayment}>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Amount *</label>
                <input type="number" min="0.01" step="0.01" required value={paymentForm.amount} onChange={e => setPaymentForm(f => ({ ...f, amount: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} placeholder={String((invoice.balanceDue || 0).toFixed(2))} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Method</label>
                <select value={paymentForm.method} onChange={e => setPaymentForm(f => ({ ...f, method: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, background: '#fff', boxSizing: 'border-box' }}>
                  {PAYMENT_METHODS.map(m => <option key={m} value={m}>{m.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase())}</option>)}
                </select>
              </div>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Payment Date</label>
                <input type="date" value={paymentForm.paidAt} onChange={e => setPaymentForm(f => ({ ...f, paidAt: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Reference / Check #</label>
                <input value={paymentForm.reference} onChange={e => setPaymentForm(f => ({ ...f, reference: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} placeholder="Check number, transaction ID…" />
              </div>
              <div style={{ marginBottom: 22 }}>
                <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#444', marginBottom: 5 }}>Notes</label>
                <input value={paymentForm.notes} onChange={e => setPaymentForm(f => ({ ...f, notes: e.target.value }))} style={{ width: '100%', padding: '9px 12px', border: '1px solid #ddd', borderRadius: 6, fontSize: 14, boxSizing: 'border-box' }} placeholder="Optional notes…" />
              </div>
              <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
                <button type="button" onClick={() => setShowPaymentModal(false)} style={{ padding: '10px 20px', background: '#f0f0f0', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 14 }}>Cancel</button>
                <button type="submit" disabled={paymentSaving} style={{ padding: '10px 24px', background: '#198754', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 14, fontWeight: 600 }}>
                  {paymentSaving ? 'Recording…' : 'Record Payment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
