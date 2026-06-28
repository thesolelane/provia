import React, { useState, useEffect, useCallback } from 'react';
import { apiService } from '../../services/apiService';

const PRIORITY_META = {
  urgent: { label: 'Urgent', color: '#dc3545', dot: '🔴' },
  high:   { label: 'High',   color: '#fd7e14', dot: '🟠' },
  medium: { label: 'Medium', color: '#0d6efd', dot: '🔵' },
  low:    { label: 'Low',    color: '#6c757d', dot: '⚪' },
};

const RECURRENCE_LABELS = {
  daily: 'Daily', weekly: 'Weekly', biweekly: 'Every 2 weeks',
  monthly: 'Monthly', yearly: 'Yearly',
};

function fmtDate(d) {
  if (!d) return null;
  const date = new Date(d);
  const today = new Date(); today.setHours(0, 0, 0, 0);
  const tomorrow = new Date(today); tomorrow.setDate(today.getDate() + 1);
  if (date < today) return { label: 'Overdue ' + date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }), overdue: true };
  if (date.toDateString() === today.toDateString()) return { label: 'Today', today: true };
  if (date.toDateString() === tomorrow.toDateString()) return { label: 'Tomorrow', soon: true };
  return { label: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined }) };
}

function toInputDate(d) {
  if (!d) return '';
  return new Date(d).toISOString().slice(0, 10);
}

const emptyForm = { title: '', description: '', dueDate: '', assignedToName: '', priority: 'medium', isRecurring: false, recurrenceRule: 'weekly', jobId: '', contactId: '', leadId: '' };

export default function TaskList() {
  const [tasks, setTasks] = useState([]);
  const [counts, setCounts] = useState(null);
  const [loading, setLoading] = useState(true);
  const [view, setView] = useState('all');   // all | mine | overdue
  const [showDone, setShowDone] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null); // task being edited
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [completing, setCompleting] = useState(null); // task id being completed
  const [completeNote, setCompleteNote] = useState('');

  const fetchAll = useCallback(async () => {
    try {
      setLoading(true);
      const [taskData, countData] = await Promise.all([
        apiService.tasks.getAll({ view, status: showDone ? undefined : undefined }),
        apiService.tasks.getCounts(),
      ]);
      setTasks(taskData);
      setCounts(countData);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }, [view, showDone]);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  const openNew = () => { setEditing(null); setForm(emptyForm); setShowModal(true); };
  const openEdit = (task) => {
    setEditing(task);
    setForm({
      title: task.title, description: task.description || '',
      dueDate: toInputDate(task.dueDate), assignedToName: task.assignedToName || '',
      priority: task.priority, isRecurring: task.isRecurring,
      recurrenceRule: task.recurrenceRule || 'weekly',
      jobId: task.jobId || '', contactId: task.contactId || '', leadId: task.leadId || '',
    });
    setShowModal(true);
  };

  const handleSave = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      const payload = {
        ...form,
        dueDate: form.dueDate || null,
        jobId: form.jobId ? parseInt(form.jobId) : null,
        contactId: form.contactId ? parseInt(form.contactId) : null,
        leadId: form.leadId ? parseInt(form.leadId) : null,
      };
      if (editing) {
        await apiService.tasks.update(editing.id, payload);
      } else {
        await apiService.tasks.create(payload);
      }
      setShowModal(false);
      fetchAll();
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleComplete = async (taskId) => {
    try {
      setSaving(true);
      const result = await apiService.tasks.complete(taskId, completeNote);
      setCompleting(null);
      setCompleteNote('');
      fetchAll();
      if (result.isRecurring && result.nextTaskId) {
        // Brief visual confirmation
        setTimeout(() => {}, 100);
      }
    } catch (e) { alert('Failed: ' + e.message); }
    finally { setSaving(false); }
  };

  const handleDelete = async (taskId) => {
    if (!window.confirm('Cancel this task?')) return;
    try {
      await apiService.tasks.delete(taskId);
      fetchAll();
    } catch (e) { alert('Failed: ' + e.message); }
  };

  const visibleTasks = showDone ? tasks : tasks.filter(t => t.status !== 'complete');

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ margin: 0 }}>Tasks</h2>
          <p style={{ margin: '0.25rem 0 0', color: '#6c757d', fontSize: '0.9rem' }}>
            To-dos, reminders, and recurring tasks
          </p>
        </div>
        <button className="btn btn-primary" onClick={openNew}>+ New Task</button>
      </div>

      {/* Count badges */}
      {counts && (
        <div style={{ display: 'flex', gap: '1rem', marginBottom: '1.25rem', flexWrap: 'wrap' }}>
          <CountBadge label="Open" value={counts.total} color="#2F5A7E" />
          <CountBadge label="Overdue" value={counts.overdue} color="#dc3545" />
          <CountBadge label="Due Today" value={counts.dueToday} color="#fd7e14" />
        </div>
      )}

      {/* Filters */}
      <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap' }}>
        {[['all', 'All'], ['mine', 'Assigned to Me'], ['overdue', 'Overdue']].map(([v, l]) => (
          <button key={v} onClick={() => setView(v)} style={{
            padding: '4px 14px', borderRadius: 20, border: '1px solid', cursor: 'pointer', fontSize: '0.82rem', fontWeight: 600,
            background: view === v ? '#2F5A7E' : '#fff', color: view === v ? '#fff' : '#6c757d',
            borderColor: view === v ? '#2F5A7E' : '#dee2e6',
          }}>{l}</button>
        ))}
        <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.85rem', color: '#6c757d', marginLeft: 'auto', cursor: 'pointer' }}>
          <input type="checkbox" checked={showDone} onChange={e => setShowDone(e.target.checked)} />
          Show completed
        </label>
      </div>

      {/* Task list */}
      {loading ? (
        <div style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>Loading...</div>
      ) : visibleTasks.length === 0 ? (
        <div className="card" style={{ padding: '3rem', textAlign: 'center', color: '#aaa' }}>
          <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>✓</div>
          <div>No tasks here</div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {visibleTasks.map(task => {
            const due = task.dueDate ? fmtDate(task.dueDate) : null;
            const pMeta = PRIORITY_META[task.priority] || PRIORITY_META.medium;
            const isDone = task.status === 'complete';

            return (
              <div key={task.id} style={{
                background: '#fff', border: '1px solid #dee2e6', borderRadius: 8,
                padding: '0.875rem 1rem', display: 'flex', alignItems: 'flex-start', gap: '0.75rem',
                opacity: isDone ? 0.6 : 1,
                borderLeft: `4px solid ${isDone ? '#dee2e6' : pMeta.color}`,
              }}>
                {/* Complete button */}
                {!isDone && completing !== task.id && (
                  <button onClick={() => setCompleting(task.id)} title="Mark complete" style={{
                    width: 22, height: 22, borderRadius: '50%', border: '2px solid #dee2e6',
                    background: '#fff', cursor: 'pointer', flexShrink: 0, marginTop: 2,
                  }} />
                )}
                {isDone && <span style={{ fontSize: '1.1rem', flexShrink: 0, marginTop: 2 }}>✓</span>}

                {/* Main content */}
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <span style={{ fontWeight: 600, fontSize: '0.95rem', textDecoration: isDone ? 'line-through' : 'none' }}>{task.title}</span>
                    <span style={{ fontSize: '0.75rem' }}>{pMeta.dot}</span>
                    {task.isRecurring && (
                      <span style={{ fontSize: '0.72rem', background: '#e7f0ff', color: '#0d6efd', padding: '1px 6px', borderRadius: 10 }}>
                        ↻ {RECURRENCE_LABELS[task.recurrenceRule] || task.recurrenceRule}
                      </span>
                    )}
                  </div>
                  {task.description && <div style={{ fontSize: '0.85rem', color: '#6c757d', marginTop: 2 }}>{task.description}</div>}
                  <div style={{ display: 'flex', gap: '0.75rem', marginTop: '0.4rem', flexWrap: 'wrap' }}>
                    {due && (
                      <span style={{ fontSize: '0.78rem', fontWeight: 600, color: due.overdue ? '#dc3545' : due.today ? '#fd7e14' : '#6c757d' }}>
                        {due.overdue ? '⚠ ' : '📅 '}{due.label}
                      </span>
                    )}
                    {task.assignedToName && <span style={{ fontSize: '0.78rem', color: '#6c757d' }}>👤 {task.assignedToName}</span>}
                    {isDone && task.completedAt && (
                      <span style={{ fontSize: '0.78rem', color: '#198754' }}>✓ {new Date(task.completedAt).toLocaleDateString()} by {task.completedBy}</span>
                    )}
                  </div>

                  {/* Inline complete confirm */}
                  {completing === task.id && (
                    <div style={{ marginTop: '0.6rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                      <input className="form-control" placeholder="Optional note..." value={completeNote} onChange={e => setCompleteNote(e.target.value)}
                        style={{ fontSize: '0.85rem', padding: '4px 8px', maxWidth: 280 }} onKeyDown={e => e.key === 'Enter' && handleComplete(task.id)} />
                      <button className="btn btn-sm" onClick={() => handleComplete(task.id)} disabled={saving}
                        style={{ background: '#198754', color: '#fff', border: 'none', fontSize: '0.82rem', padding: '4px 12px', whiteSpace: 'nowrap' }}>
                        {task.isRecurring ? '✓ Done (spawn next)' : '✓ Done'}
                      </button>
                      <button className="btn btn-sm" onClick={() => { setCompleting(null); setCompleteNote(''); }} style={{ fontSize: '0.82rem', padding: '4px 10px' }}>×</button>
                    </div>
                  )}
                </div>

                {/* Actions */}
                {!isDone && (
                  <div style={{ display: 'flex', gap: '0.25rem', flexShrink: 0 }}>
                    <button onClick={() => openEdit(task)} style={{ background: 'none', border: 'none', color: '#6c757d', cursor: 'pointer', fontSize: '0.9rem', padding: '2px 6px' }} title="Edit">✎</button>
                    <button onClick={() => handleDelete(task.id)} style={{ background: 'none', border: 'none', color: '#dc3545', cursor: 'pointer', fontSize: '0.9rem', padding: '2px 6px' }} title="Cancel task">×</button>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Create / Edit Modal */}
      {showModal && (
        <div style={overlayStyle}>
          <div style={{ ...modalStyle, maxWidth: 520 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h3 style={{ margin: 0 }}>{editing ? 'Edit Task' : 'New Task'}</h3>
              <button onClick={() => setShowModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.5rem', cursor: 'pointer', color: '#6c757d' }}>×</button>
            </div>
            <form onSubmit={handleSave}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
                <div>
                  <label style={labelStyle}>Title *</label>
                  <input className="form-control" required value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} />
                </div>
                <div>
                  <label style={labelStyle}>Description</label>
                  <textarea className="form-control" rows={2} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                  <div>
                    <label style={labelStyle}>Due Date</label>
                    <input type="date" className="form-control" value={form.dueDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} />
                  </div>
                  <div>
                    <label style={labelStyle}>Priority</label>
                    <select className="form-control" value={form.priority} onChange={e => setForm(f => ({ ...f, priority: e.target.value }))}>
                      <option value="low">Low</option>
                      <option value="medium">Medium</option>
                      <option value="high">High</option>
                      <option value="urgent">Urgent</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label style={labelStyle}>Assigned To</label>
                  <input className="form-control" placeholder="Person's name" value={form.assignedToName} onChange={e => setForm(f => ({ ...f, assignedToName: e.target.value }))} />
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', padding: '0.6rem 0.875rem', background: '#f8f9fa', borderRadius: 6 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer', margin: 0 }}>
                    <input type="checkbox" checked={form.isRecurring} onChange={e => setForm(f => ({ ...f, isRecurring: e.target.checked }))} />
                    <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>Recurring</span>
                  </label>
                  {form.isRecurring && (
                    <select className="form-control" style={{ maxWidth: 180 }} value={form.recurrenceRule} onChange={e => setForm(f => ({ ...f, recurrenceRule: e.target.value }))}>
                      <option value="daily">Daily</option>
                      <option value="weekly">Weekly</option>
                      <option value="biweekly">Every 2 weeks</option>
                      <option value="monthly">Monthly</option>
                      <option value="yearly">Yearly</option>
                    </select>
                  )}
                </div>
              </div>
              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '1.25rem' }}>
                <button type="button" className="btn" onClick={() => setShowModal(false)} disabled={saving}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : editing ? 'Save Changes' : 'Create Task'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function CountBadge({ label, value, color }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', padding: '0.4rem 0.875rem', background: '#fff', border: '1px solid #dee2e6', borderRadius: 8 }}>
      <span style={{ fontWeight: 800, fontSize: '1.2rem', color }}>{value}</span>
      <span style={{ fontSize: '0.82rem', color: '#6c757d' }}>{label}</span>
    </div>
  );
}

const overlayStyle = { position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 };
const modalStyle = { background: '#fff', borderRadius: 8, padding: '1.75rem', width: '100%', maxHeight: '90vh', overflowY: 'auto', boxShadow: '0 20px 60px rgba(0,0,0,0.3)' };
const labelStyle = { fontWeight: 600, fontSize: '0.8rem', color: '#6c757d', textTransform: 'uppercase', letterSpacing: '0.4px', marginBottom: '0.2rem', display: 'block' };
