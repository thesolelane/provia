import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const TABS = ['Overview', 'Jobs', 'Activity'];

function ContactDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [contact, setContact] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [tab, setTab] = useState('Overview');
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({});
  const [saving, setSaving] = useState(false);
  const [note, setNote] = useState('');
  const [addingNote, setAddingNote] = useState(false);

  const fetchContact = async () => {
    try {
      setLoading(true);
      const data = await apiService.contacts.getById(id);
      setContact(data);
      setForm({
        fullName: data.fullName, companyName: data.companyName || '',
        contactType: data.contactType, phone: data.phone || '',
        email: data.email || '', address: data.address || '',
        city: data.city || '', state: data.state || '', zipCode: data.zipCode || '',
        notes: data.notes || ''
      });
      setError(null);
    } catch (err) {
      setError('Contact not found.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchContact(); }, [id]);

  const handleSave = async () => {
    try {
      setSaving(true);
      await apiService.contacts.update(id, form);
      setEditing(false);
      fetchContact();
    } catch (err) {
      alert('Failed to save: ' + err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleAddNote = async () => {
    if (!note.trim()) return;
    try {
      setAddingNote(true);
      await apiService.contacts.addNote(id, note.trim());
      setNote('');
      fetchContact();
    } catch (err) {
      alert('Failed to add note: ' + err.message);
    } finally {
      setAddingNote(false);
    }
  };

  const handleDeactivate = async () => {
    if (!window.confirm(`Deactivate ${contact.fullName}? They will no longer appear in contact lists.`)) return;
    try {
      await apiService.contacts.deactivate(id);
      navigate('/contacts');
    } catch (err) {
      alert('Failed to deactivate: ' + err.message);
    }
  };

  if (loading) return <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>;
  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!contact) return null;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem' }}>
        <div>
          <Link to="/contacts" style={{ color: '#6c757d', textDecoration: 'none', fontSize: '0.9rem' }}>← Contacts</Link>
          <h2 style={{ margin: '0.25rem 0 0', display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            {contact.fullName}
            <span style={{
              fontFamily: 'monospace', fontSize: '0.8rem', padding: '2px 8px',
              background: '#f0f4f8', borderRadius: 4, color: '#6c757d', fontWeight: 400
            }}>{contact.customerNumber}</span>
            <span style={{
              padding: '2px 10px', borderRadius: 12, fontSize: '0.78rem',
              background: contact.contactType === 'Commercial' ? '#e3f2fd' : '#f3e5f5',
              color: contact.contactType === 'Commercial' ? '#1565c0' : '#6a1b9a'
            }}>{contact.contactType}</span>
          </h2>
          {contact.companyName && <p style={{ margin: '0.25rem 0 0', color: '#6c757d' }}>{contact.companyName}</p>}
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          {editing ? (
            <>
              <button className="btn" onClick={() => setEditing(false)} disabled={saving}>Cancel</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : 'Save Changes'}</button>
            </>
          ) : (
            <>
              <button className="btn" onClick={() => setEditing(true)}>Edit</button>
              <button className="btn" onClick={handleDeactivate} style={{ color: '#dc3545', borderColor: '#dc3545' }}>Deactivate</button>
            </>
          )}
        </div>
      </div>

      <div style={{ display: 'flex', gap: 0, borderBottom: '2px solid #dee2e6', marginBottom: '1.5rem' }}>
        {TABS.map(t => (
          <button key={t} onClick={() => setTab(t)} style={{
            padding: '0.6rem 1.25rem', border: 'none', background: 'none', cursor: 'pointer',
            fontWeight: tab === t ? 700 : 400, fontSize: '0.95rem',
            borderBottom: tab === t ? '2px solid #FF9500' : '2px solid transparent',
            color: tab === t ? '#FF9500' : '#6c757d', marginBottom: -2
          }}>
            {t}
            {t === 'Jobs' && contact.jobs?.length > 0 && (
              <span style={{ marginLeft: 6, background: '#2F5A7E', color: '#fff', borderRadius: 10, padding: '1px 7px', fontSize: '0.75rem' }}>
                {contact.jobs.length}
              </span>
            )}
            {t === 'Activity' && contact.activityLogs?.length > 0 && (
              <span style={{ marginLeft: 6, background: '#6c757d', color: '#fff', borderRadius: 10, padding: '1px 7px', fontSize: '0.75rem' }}>
                {contact.activityLogs.length}
              </span>
            )}
          </button>
        ))}
      </div>

      {tab === 'Overview' && (
        <div className="card" style={{ padding: '1.5rem' }}>
          {editing ? (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>Full Name</label>
                <input className="form-control" value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} />
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
                <textarea className="form-control" rows={4} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} />
              </div>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
              <Field label="Phone" value={contact.phone} />
              <Field label="Email" value={contact.email} />
              <Field label="Address" value={[contact.address, contact.city, contact.state, contact.zipCode].filter(Boolean).join(', ')} />
              <Field label="Customer Since" value={new Date(contact.createdAt).toLocaleDateString()} />
              {contact.notes && (
                <div style={{ gridColumn: '1 / -1' }}>
                  <div style={labelStyle}>Notes</div>
                  <div style={{ background: '#f8f9fa', borderRadius: 6, padding: '0.75rem', whiteSpace: 'pre-wrap', fontSize: '0.9rem' }}>{contact.notes}</div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {tab === 'Jobs' && (
        <div>
          {contact.jobs?.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: '2rem', color: '#6c757d' }}>
              No jobs linked to this contact yet.
            </div>
          ) : (
            <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                    <th style={thStyle}>Job #</th>
                    <th style={thStyle}>Name</th>
                    <th style={thStyle}>Location</th>
                    <th style={thStyle}>Status</th>
                    <th style={thStyle}>Created</th>
                  </tr>
                </thead>
                <tbody>
                  {contact.jobs.map((j, i) => (
                    <tr key={j.id} style={{ borderBottom: '1px solid #dee2e6', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                      <td style={tdStyle}>
                        <Link to={`/jobs/${j.id}`} style={{ fontFamily: 'monospace', color: '#2F5A7E', textDecoration: 'none' }}>{j.jobNumber}</Link>
                      </td>
                      <td style={tdStyle}>{j.name}</td>
                      <td style={tdStyle}>{j.location}</td>
                      <td style={tdStyle}>
                        <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: '0.78rem', background: '#e8f5e9', color: '#2e7d32' }}>{j.status}</span>
                      </td>
                      <td style={tdStyle}>{new Date(j.createdAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {tab === 'Activity' && (
        <div>
          <div className="card" style={{ padding: '1rem', marginBottom: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input
                className="form-control"
                placeholder="Add a note..."
                value={note}
                onChange={e => setNote(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && !e.shiftKey && handleAddNote()}
              />
              <button className="btn btn-primary" onClick={handleAddNote} disabled={addingNote || !note.trim()}>
                {addingNote ? '...' : 'Add'}
              </button>
            </div>
          </div>

          {contact.activityLogs?.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: '2rem', color: '#6c757d' }}>No activity yet.</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {contact.activityLogs.map(a => (
                <div key={a.id} className="card" style={{ padding: '0.75rem 1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div>
                    <span style={{
                      padding: '2px 8px', borderRadius: 12, fontSize: '0.75rem', marginRight: '0.5rem',
                      background: a.action === 'Note' ? '#fff3cd' : a.action === 'Created' ? '#d4edda' : '#e2e3e5',
                      color: a.action === 'Note' ? '#856404' : a.action === 'Created' ? '#155724' : '#383d41'
                    }}>{a.action}</span>
                    <span style={{ fontSize: '0.9rem' }}>{a.detail}</span>
                  </div>
                  <div style={{ fontSize: '0.8rem', color: '#6c757d', whiteSpace: 'nowrap', marginLeft: '1rem' }}>
                    {a.userName} · {new Date(a.createdAt).toLocaleString()}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Field({ label, value }) {
  return (
    <div>
      <div style={labelStyle}>{label}</div>
      <div style={{ fontSize: '0.95rem' }}>{value || <span style={{ color: '#aaa' }}>—</span>}</div>
    </div>
  );
}

const thStyle = { padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem', color: '#495057' };
const tdStyle = { padding: '0.75rem 1rem', verticalAlign: 'middle' };
const labelStyle = { fontWeight: 600, fontSize: '0.82rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.25rem' };

export default ContactDetail;
