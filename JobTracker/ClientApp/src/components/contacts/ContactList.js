import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

function ContactList() {
  const [contacts, setContacts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    fullName: '', companyName: '', contactType: 'Residential',
    phone: '', email: '', address: '', city: '', state: 'MA', zipCode: '', notes: ''
  });

  const fetchContacts = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiService.contacts.getAll(search);
      setContacts(data);
      setError(null);
    } catch (err) {
      setError('Failed to load contacts.');
    } finally {
      setLoading(false);
    }
  }, [search]);

  useEffect(() => {
    const timer = setTimeout(fetchContacts, 300);
    return () => clearTimeout(timer);
  }, [fetchContacts]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.fullName.trim()) return;
    try {
      setSaving(true);
      await apiService.contacts.create(form);
      setShowModal(false);
      setForm({ fullName: '', companyName: '', contactType: 'Residential', phone: '', email: '', address: '', city: '', state: 'MA', zipCode: '', notes: '' });
      fetchContacts();
    } catch (err) {
      alert('Failed to create contact: ' + err.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 className="card-title" style={{ margin: 0 }}>Contacts</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>
            {contacts.length} contact{contacts.length !== 1 ? 's' : ''}
          </p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowModal(true)}>+ New Contact</button>
      </div>

      <div style={{ marginBottom: '1rem' }}>
        <input
          type="text"
          className="form-control"
          placeholder="Search by name, number, email, phone, city..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          style={{ maxWidth: 400 }}
        />
      </div>

      {loading && <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading contacts...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && contacts.length === 0 && (
        <div className="card" style={{ textAlign: 'center', padding: '3rem' }}>
          <p style={{ color: '#6c757d', marginBottom: '1rem' }}>
            {search ? 'No contacts match your search.' : 'No contacts yet. Add your first customer.'}
          </p>
          {!search && <button className="btn btn-primary" onClick={() => setShowModal(true)}>+ New Contact</button>}
        </div>
      )}

      {!loading && contacts.length > 0 && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                <th style={thStyle}>#</th>
                <th style={thStyle}>Name</th>
                <th style={thStyle}>Type</th>
                <th style={thStyle}>Phone</th>
                <th style={thStyle}>Email</th>
                <th style={thStyle}>City</th>
                <th style={thStyle}>Jobs</th>
                <th style={thStyle}></th>
              </tr>
            </thead>
            <tbody>
              {contacts.map((c, i) => (
                <tr key={c.id} style={{ borderBottom: '1px solid #dee2e6', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                  <td style={tdStyle}>
                    <span style={{ fontFamily: 'monospace', fontSize: '0.8rem', color: '#6c757d' }}>{c.customerNumber}</span>
                  </td>
                  <td style={tdStyle}>
                    <Link to={`/contacts/${c.id}`} style={{ fontWeight: 600, color: '#2F5A7E', textDecoration: 'none' }}>
                      {c.fullName}
                    </Link>
                    {c.companyName && <div style={{ fontSize: '0.8rem', color: '#6c757d' }}>{c.companyName}</div>}
                  </td>
                  <td style={tdStyle}>
                    <span style={{
                      padding: '2px 8px', borderRadius: 12, fontSize: '0.78rem',
                      background: c.contactType === 'Commercial' ? '#e3f2fd' : '#f3e5f5',
                      color: c.contactType === 'Commercial' ? '#1565c0' : '#6a1b9a'
                    }}>
                      {c.contactType}
                    </span>
                  </td>
                  <td style={tdStyle}>{c.phone || '—'}</td>
                  <td style={tdStyle}>{c.email || '—'}</td>
                  <td style={tdStyle}>{c.city || '—'}</td>
                  <td style={tdStyle}>
                    <span style={{ fontWeight: 600, color: c.jobCount > 0 ? '#2F5A7E' : '#aaa' }}>{c.jobCount}</span>
                  </td>
                  <td style={tdStyle}>
                    <Link to={`/contacts/${c.id}`} className="btn btn-sm" style={{ padding: '4px 12px', fontSize: '0.8rem' }}>View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showModal && (
        <div style={overlayStyle}>
          <div style={modalStyle}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
              <h3 style={{ margin: 0 }}>New Contact</h3>
              <button onClick={() => setShowModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.5rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handleSubmit}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Full Name *</label>
                  <input className="form-control" required value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Company Name</label>
                  <input className="form-control" value={form.companyName} onChange={e => setForm(f => ({ ...f, companyName: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Type</label>
                  <select className="form-control" value={form.contactType} onChange={e => setForm(f => ({ ...f, contactType: e.target.value }))}>
                    <option>Residential</option>
                    <option>Commercial</option>
                  </select>
                </div>
                <div>
                  <label style={labelStyle}>Phone</label>
                  <input className="form-control" value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Email</label>
                  <input type="email" className="form-control" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} />
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Address</label>
                  <input className="form-control" value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>City</label>
                  <input className="form-control" value={form.city} onChange={e => setForm(f => ({ ...f, city: e.target.value }))} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                  <div>
                    <label style={labelStyle}>State</label>
                    <input className="form-control" value={form.state} onChange={e => setForm(f => ({ ...f, state: e.target.value }))} />
                  </div>
                  <div>
                    <label style={labelStyle}>ZIP</label>
                    <input className="form-control" value={form.zipCode} onChange={e => setForm(f => ({ ...f, zipCode: e.target.value }))} />
                  </div>
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Notes</label>
                  <textarea className="form-control" rows={3} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
                </div>
              </div>
              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '1.5rem' }}>
                <button type="button" className="btn" onClick={() => setShowModal(false)} disabled={saving}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Create Contact'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

const thStyle = { padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem', color: '#495057' };
const tdStyle = { padding: '0.75rem 1rem', verticalAlign: 'middle' };
const overlayStyle = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle = { background: '#fff', borderRadius: 8, padding: '2rem', width: '100%', maxWidth: 600, maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const labelStyle = { display: 'block', fontWeight: 600, fontSize: '0.85rem', marginBottom: '0.25rem', color: '#495057' };

export default ContactList;
