import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const STAGES = [
  { key: 'incoming',          label: 'New Lead',       staleDays: 1,  color: '#6c757d' },
  { key: 'callback_done',     label: 'Callback Done',  staleDays: 2,  color: '#0d6efd' },
  { key: 'appointment_booked',label: 'Appt. Booked',  staleDays: 2,  color: '#6610f2' },
  { key: 'site_visit_done',   label: 'Site Visited',   staleDays: 3,  color: '#fd7e14' },
  { key: 'quote_sent',        label: 'Quote Sent',     staleDays: 7,  color: '#d63384' },
  { key: 'follow_up',         label: 'Following Up',   staleDays: 7,  color: '#0dcaf0' },
  { key: 'signed',            label: 'Signed ✓',       staleDays: 30, color: '#198754' },
];

const SOURCES = ['Direct', 'Referral', 'Website', 'Google', 'Social Media', 'Marblism', 'Other'];
const ARCHIVE_REASONS = ['Price', 'Timing', 'Competitor', 'Ghosted', 'Mistake', 'Out of service area'];

export default function LeadPipeline() {
  const [leads, setLeads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [showNew, setShowNew] = useState(false);
  const [showArchive, setShowArchive] = useState(false);
  const [view, setView] = useState('board'); // 'board' | 'list'
  const [note, setNote] = useState('');
  const [archiveReason, setArchiveReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ callerName: '', callerPhone: '', callerEmail: '', source: 'Direct', jobAddress: '', jobCity: '', jobType: 'Residential', jobScope: '', initialNote: '' });
  const [editMode, setEditMode] = useState(false);
  const [editForm, setEditForm] = useState({});

  const fetchLeads = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiService.leads.getAll();
      setLeads(data);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchLeads(); }, [fetchLeads]);

  const openLead = async (id) => {
    try {
      const data = await apiService.leads.getById(id);
      setSelected(data);
      setEditForm({ callerName: data.callerName, callerPhone: data.callerPhone || '', callerEmail: data.callerEmail || '', source: data.source || 'Direct', jobAddress: data.jobAddress || '', jobCity: data.jobCity || '', jobType: data.jobType || 'Residential', jobScope: data.jobScope || '', appointmentAt: data.appointmentAt ? data.appointmentAt.slice(0,16) : '' });
      setEditMode(false);
      setNote('');
    } catch (e) { console.error(e); }
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      await apiService.leads.create(form);
      setShowNew(false);
      setForm({ callerName: '', callerPhone: '', callerEmail: '', source: 'Direct', jobAddress: '', jobCity: '', jobType: 'Residential', jobScope: '', initialNote: '' });
      fetchLeads();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleStage = async (leadId, stage) => {
    try {
      await apiService.leads.changeStage(leadId, stage);
      fetchLeads();
      if (selected?.id === leadId) openLead(leadId);
    } catch (e) { alert('Failed: ' + e.message); }
  };

  const handleAddNote = async () => {
    if (!note.trim() || !selected) return;
    try {
      setSaving(true);
      await apiService.leads.addNote(selected.id, note.trim());
      setNote('');
      openLead(selected.id);
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleUpdate = async () => {
    try {
      setSaving(true);
      await apiService.leads.update(selected.id, { ...editForm, appointmentAt: editForm.appointmentAt || null });
      setEditMode(false);
      openLead(selected.id);
      fetchLeads();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleArchive = async () => {
    if (!archiveReason) { alert('Please select a reason.'); return; }
    try {
      setSaving(true);
      await apiService.leads.archive(selected.id, archiveReason);
      setSelected(null);
      setShowArchive(false);
      setArchiveReason('');
      fetchLeads();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleGraduate = async () => {
    if (!window.confirm('Graduate this lead to a Contact? This will create a new contact record.')) return;
    try {
      const result = await apiService.leads.graduate(selected.id);
      alert(`Created contact ${result.customerNumber}`);
      setSelected(null);
      fetchLeads();
    } catch (e) { alert('Failed: ' + e.message); }
  };

  const stageLeads = (stageKey) => leads.filter(l => l.stage === stageKey);
  const isStale = (lead) => {
    const s = STAGES.find(s => s.key === lead.stage);
    return s ? lead.daysInStage >= s.staleDays : false;
  };

  if (loading) return <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading pipeline...</div>;

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Leads Pipeline</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>
            {leads.filter(l => l.stage !== 'signed').length} active · {leads.filter(l => l.stage === 'signed').length} signed
          </p>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => setView(view === 'board' ? 'list' : 'board')} className="btn" style={{ fontSize: '0.85rem' }}>
            {view === 'board' ? '☰ List' : '⊞ Board'}
          </button>
          <button onClick={() => setShowNew(true)} className="btn btn-primary">+ New Lead</button>
        </div>
      </div>

      {/* Board view */}
      {view === 'board' && (
        <div style={{ display: 'flex', gap: '0.75rem', overflowX: 'auto', paddingBottom: '1rem', alignItems: 'flex-start' }}>
          {STAGES.map(stage => {
            const stageItems = stageLeads(stage.key);
            return (
              <div key={stage.key} style={{ minWidth: 220, maxWidth: 240, flexShrink: 0 }}>
                <div style={{ padding: '0.5rem 0.75rem', borderRadius: '6px 6px 0 0', background: stage.color, color: '#fff', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontWeight: 700, fontSize: '0.82rem' }}>{stage.label}</span>
                  <span style={{ background: 'rgba(255,255,255,0.25)', borderRadius: 12, padding: '1px 7px', fontSize: '0.78rem' }}>{stageItems.length}</span>
                </div>
                <div style={{ background: '#f8f9fa', borderRadius: '0 0 6px 6px', minHeight: 80, padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  {stageItems.map(lead => (
                    <div key={lead.id} onClick={() => openLead(lead.id)} style={{ background: '#fff', border: '1px solid #dee2e6', borderRadius: 6, padding: '0.6rem 0.75rem', cursor: 'pointer', boxShadow: '0 1px 3px rgba(0,0,0,0.06)', borderLeft: `3px solid ${stage.color}` }}>
                      <div style={{ fontWeight: 600, fontSize: '0.88rem', marginBottom: 2 }}>{lead.callerName}</div>
                      {lead.callerPhone && <div style={{ fontSize: '0.78rem', color: '#6c757d' }}>{lead.callerPhone}</div>}
                      {lead.jobCity && <div style={{ fontSize: '0.78rem', color: '#6c757d' }}>{lead.jobCity}</div>}
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '0.4rem' }}>
                        <span style={{ fontSize: '0.72rem', color: '#aaa' }}>{lead.leadNumber}</span>
                        {isStale(lead) ? (
                          <span style={{ fontSize: '0.72rem', background: '#fff3cd', color: '#856404', padding: '1px 6px', borderRadius: 10 }}>⚠ {lead.daysInStage}d</span>
                        ) : (
                          <span style={{ fontSize: '0.72rem', color: '#aaa' }}>{lead.daysInStage}d</span>
                        )}
                      </div>
                    </div>
                  ))}
                  {stageItems.length === 0 && <div style={{ fontSize: '0.8rem', color: '#ccc', textAlign: 'center', padding: '1rem 0' }}>Empty</div>}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* List view */}
      {view === 'list' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f8f9fa', borderBottom: '2px solid #dee2e6' }}>
                {['#', 'Name', 'Phone', 'City', 'Scope', 'Stage', 'Days', ''].map(h => (
                  <th key={h} style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: 600, fontSize: '0.85rem', color: '#495057' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {leads.map((lead, i) => {
                const stage = STAGES.find(s => s.key === lead.stage);
                return (
                  <tr key={lead.id} style={{ borderBottom: '1px solid #dee2e6', background: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                    <td style={{ padding: '0.75rem 1rem', fontFamily: 'monospace', fontSize: '0.78rem', color: '#6c757d' }}>{lead.leadNumber}</td>
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600 }}>{lead.callerName}</td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.9rem' }}>{lead.callerPhone || '—'}</td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.9rem' }}>{lead.jobCity || '—'}</td>
                    <td style={{ padding: '0.75rem 1rem', fontSize: '0.85rem', maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{lead.jobScope || '—'}</td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: '0.78rem', background: stage?.color + '22', color: stage?.color, fontWeight: 600 }}>{stage?.label}</span>
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      {isStale(lead) ? <span style={{ color: '#856404', fontWeight: 600 }}>⚠ {lead.daysInStage}d</span> : <span style={{ color: '#aaa' }}>{lead.daysInStage}d</span>}
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <button onClick={() => openLead(lead.id)} className="btn btn-sm" style={{ fontSize: '0.8rem', padding: '3px 10px' }}>Open</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Lead Detail Modal */}
      {selected && (
        <div style={overlayStyle} onClick={e => e.target === e.currentTarget && setSelected(null)}>
          <div style={{ ...modalStyle, maxWidth: 680 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
              <div>
                <div style={{ fontFamily: 'monospace', fontSize: '0.78rem', color: '#6c757d' }}>{selected.leadNumber}</div>
                <h3 style={{ margin: '0.1rem 0 0' }}>{selected.callerName}</h3>
                {selected.contactId && (
                  <Link to={`/contacts/${selected.contactId}`} style={{ fontSize: '0.8rem', color: '#2F5A7E' }}>→ View Contact</Link>
                )}
              </div>
              <button onClick={() => setSelected(null)} style={{ background: 'none', border: 'none', fontSize: '1.5rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>

            {/* Stage strip */}
            <div style={{ display: 'flex', gap: 4, marginBottom: '1.25rem', flexWrap: 'wrap' }}>
              {STAGES.map(s => (
                <button key={s.key} onClick={() => handleStage(selected.id, s.key)} style={{
                  padding: '3px 10px', borderRadius: 12, border: 'none', cursor: 'pointer', fontSize: '0.78rem', fontWeight: 600,
                  background: selected.stage === s.key ? s.color : '#f0f0f0',
                  color: selected.stage === s.key ? '#fff' : '#666',
                }}>
                  {s.label}
                </button>
              ))}
            </div>

            {/* Fields */}
            {!editMode ? (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
                <Field label="Phone" value={selected.callerPhone} />
                <Field label="Email" value={selected.callerEmail} />
                <Field label="Source" value={selected.source} />
                <Field label="Job Type" value={selected.jobType} />
                <Field label="City" value={selected.jobCity} />
                <Field label="Address" value={selected.jobAddress} />
                {selected.appointmentAt && <Field label="Appointment" value={new Date(selected.appointmentAt).toLocaleString()} />}
                {selected.jobScope && <div style={{ gridColumn: '1 / -1' }}><Field label="Scope of Work" value={selected.jobScope} /></div>}
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '1rem' }}>
                {[['callerName','Name'],['callerPhone','Phone'],['callerEmail','Email'],['jobAddress','Address'],['jobCity','City']].map(([k,l]) => (
                  <div key={k}>
                    <label style={labelStyle}>{l}</label>
                    <input className="form-control" value={editForm[k] || ''} onChange={e => setEditForm(f => ({ ...f, [k]: e.target.value }))} />
                  </div>
                ))}
                <div>
                  <label style={labelStyle}>Job Type</label>
                  <select className="form-control" value={editForm.jobType} onChange={e => setEditForm(f => ({ ...f, jobType: e.target.value }))}>
                    <option>Residential</option><option>Commercial</option>
                  </select>
                </div>
                <div>
                  <label style={labelStyle}>Source</label>
                  <select className="form-control" value={editForm.source} onChange={e => setEditForm(f => ({ ...f, source: e.target.value }))}>
                    {SOURCES.map(s => <option key={s}>{s}</option>)}
                  </select>
                </div>
                <div>
                  <label style={labelStyle}>Appointment</label>
                  <input type="datetime-local" className="form-control" value={editForm.appointmentAt || ''} onChange={e => setEditForm(f => ({ ...f, appointmentAt: e.target.value }))} />
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Scope of Work</label>
                  <textarea className="form-control" rows={3} value={editForm.jobScope || ''} onChange={e => setEditForm(f => ({ ...f, jobScope: e.target.value }))} />
                </div>
              </div>
            )}

            {/* Notes */}
            <div style={{ borderTop: '1px solid #dee2e6', paddingTop: '1rem', marginBottom: '1rem' }}>
              <div style={{ fontWeight: 600, fontSize: '0.85rem', marginBottom: '0.5rem' }}>Notes ({selected.notes?.length || 0})</div>
              <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.75rem' }}>
                <input className="form-control" placeholder="Add a note..." value={note} onChange={e => setNote(e.target.value)} onKeyDown={e => e.key === 'Enter' && !e.shiftKey && handleAddNote()} />
                <button className="btn btn-primary" onClick={handleAddNote} disabled={saving || !note.trim()} style={{ whiteSpace: 'nowrap' }}>Add</button>
              </div>
              <div style={{ maxHeight: 200, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
                {selected.notes?.map(n => (
                  <div key={n.id} style={{ background: '#f8f9fa', borderRadius: 6, padding: '0.5rem 0.75rem', fontSize: '0.88rem' }}>
                    <div>{n.body}</div>
                    <div style={{ fontSize: '0.75rem', color: '#aaa', marginTop: 2 }}>{n.userName} · {new Date(n.createdAt).toLocaleString()}</div>
                  </div>
                ))}
              </div>
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
              {!editMode ? (
                <button className="btn" onClick={() => setEditMode(true)} style={{ fontSize: '0.85rem' }}>Edit</button>
              ) : (
                <>
                  <button className="btn btn-primary" onClick={handleUpdate} disabled={saving} style={{ fontSize: '0.85rem' }}>{saving ? '...' : 'Save'}</button>
                  <button className="btn" onClick={() => setEditMode(false)} style={{ fontSize: '0.85rem' }}>Cancel</button>
                </>
              )}
              {!selected.contactId && (
                <button className="btn" onClick={handleGraduate} style={{ fontSize: '0.85rem', color: '#198754', borderColor: '#198754' }}>→ Graduate to Contact</button>
              )}
              <button className="btn" onClick={() => setShowArchive(true)} style={{ fontSize: '0.85rem', color: '#dc3545', borderColor: '#dc3545', marginLeft: 'auto' }}>Archive</button>
            </div>
          </div>
        </div>
      )}

      {/* Archive Modal */}
      {showArchive && (
        <div style={overlayStyle}>
          <div style={{ ...modalStyle, maxWidth: 380 }}>
            <h4 style={{ margin: '0 0 1rem' }}>Archive Lead</h4>
            <label style={labelStyle}>Reason</label>
            <select className="form-control" value={archiveReason} onChange={e => setArchiveReason(e.target.value)} style={{ marginBottom: '1rem' }}>
              <option value="">Select reason...</option>
              {ARCHIVE_REASONS.map(r => <option key={r}>{r}</option>)}
            </select>
            <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
              <button className="btn" onClick={() => setShowArchive(false)}>Cancel</button>
              <button className="btn" onClick={handleArchive} disabled={saving} style={{ color: '#dc3545', borderColor: '#dc3545' }}>Archive</button>
            </div>
          </div>
        </div>
      )}

      {/* New Lead Modal */}
      {showNew && (
        <div style={overlayStyle}>
          <div style={{ ...modalStyle, maxWidth: 560 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h3 style={{ margin: 0 }}>New Lead</h3>
              <button onClick={() => setShowNew(false)} style={{ background: 'none', border: 'none', fontSize: '1.5rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handleCreate}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.875rem' }}>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Name *</label>
                  <input className="form-control" required value={form.callerName} onChange={e => setForm(f => ({ ...f, callerName: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Phone</label>
                  <input className="form-control" value={form.callerPhone} onChange={e => setForm(f => ({ ...f, callerPhone: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Email</label>
                  <input type="email" className="form-control" value={form.callerEmail} onChange={e => setForm(f => ({ ...f, callerEmail: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Source</label>
                  <select className="form-control" value={form.source} onChange={e => setForm(f => ({ ...f, source: e.target.value }))}>
                    {SOURCES.map(s => <option key={s}>{s}</option>)}
                  </select>
                </div>
                <div>
                  <label style={labelStyle}>Job Type</label>
                  <select className="form-control" value={form.jobType} onChange={e => setForm(f => ({ ...f, jobType: e.target.value }))}>
                    <option>Residential</option><option>Commercial</option>
                  </select>
                </div>
                <div>
                  <label style={labelStyle}>City</label>
                  <input className="form-control" value={form.jobCity} onChange={e => setForm(f => ({ ...f, jobCity: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Address</label>
                  <input className="form-control" value={form.jobAddress} onChange={e => setForm(f => ({ ...f, jobAddress: e.target.value }))} />
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Scope of Work</label>
                  <textarea className="form-control" rows={3} value={form.jobScope} onChange={e => setForm(f => ({ ...f, jobScope: e.target.value }))} placeholder="Briefly describe what they need..." />
                </div>
                <div style={{ gridColumn: '1 / -1' }}>
                  <label style={labelStyle}>Initial Note (optional)</label>
                  <input className="form-control" value={form.initialNote} onChange={e => setForm(f => ({ ...f, initialNote: e.target.value }))} placeholder="e.g. Referred by John Smith" />
                </div>
              </div>
              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '1.25rem' }}>
                <button type="button" className="btn" onClick={() => setShowNew(false)} disabled={saving}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Creating...' : 'Create Lead'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function Field({ label, value }) {
  return (
    <div>
      <div style={labelStyle}>{label}</div>
      <div style={{ fontSize: '0.9rem' }}>{value || <span style={{ color: '#ccc' }}>—</span>}</div>
    </div>
  );
}

const overlayStyle = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle = { background: '#fff', borderRadius: 8, padding: '1.75rem', width: '100%', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const labelStyle = { fontWeight: 600, fontSize: '0.8rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '0.2rem', display: 'block' };
