import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

const TRADE_HINTS = [
  { value: '', label: 'Select trade (optional)...' },
  { value: 'electrical', label: 'Electrical' },
  { value: 'plumbing', label: 'Plumbing' },
  { value: 'hvac', label: 'HVAC' },
  { value: 'roofing', label: 'Roofing' },
  { value: 'framing', label: 'Framing / Carpentry' },
  { value: 'concrete', label: 'Concrete / Foundation' },
  { value: 'drywall', label: 'Drywall / Finishing' },
  { value: 'painting', label: 'Painting' },
  { value: 'flooring', label: 'Flooring' },
  { value: 'general construction', label: 'General Construction' },
];

function JobForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEditMode = !!id;

  const [formData, setFormData] = useState({
    name: '', description: '', location: '', jobNumber: '',
    startDate: '', targetCompletionDate: '', status: 'Not Started',
    clientName: '', clientEmail: '', clientPhone: '',
    budget: 0, actualCost: 0, notes: ''
  });

  const [loading,      setLoading]      = useState(isEditMode);
  const [saving,       setSaving]       = useState(false);
  const [error,        setError]        = useState(null);
  const [tradeHint,    setTradeHint]    = useState('');
  const [aiGenerating, setAiGenerating] = useState(false);
  const [aiMsg,        setAiMsg]        = useState(null); // { type: 'success'|'error', text }

  useEffect(() => { if (isEditMode) fetchJob(); }, [id]);

  const fetchJob = async () => {
    try {
      setLoading(true);
      const job = await apiService.jobs.getById(id);
      setFormData({
        ...job,
        startDate:            job.startDate            ? new Date(job.startDate).toISOString().split('T')[0]            : '',
        targetCompletionDate: job.targetCompletionDate ? new Date(job.targetCompletionDate).toISOString().split('T')[0] : '',
        actualCompletionDate: job.actualCompletionDate ? new Date(job.actualCompletionDate).toISOString().split('T')[0] : '',
      });
      setError(null);
    } catch (err) {
      setError('Failed to load job. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({ ...prev, [name]: type === 'number' ? parseFloat(value) || 0 : value }));
  };

  // ── AI scope generator ──────────────────────────────────────────────────────
  const handleAiGenerate = async () => {
    if (!formData.name.trim()) {
      setAiMsg({ type: 'error', text: 'Enter a job name first so the AI has something to work with.' });
      return;
    }
    setAiGenerating(true);
    setAiMsg(null);
    try {
      const result = await apiService.ai.generateScope({
        jobName:    formData.name,
        location:   formData.location,
        clientName: formData.clientName,
        budget:     formData.budget,
        tradeHint:  tradeHint,
      });
      setFormData(prev => ({
        ...prev,
        description: result.description || prev.description,
        notes:       result.notes       || prev.notes,
      }));
      setAiMsg({
        type: 'success',
        text: result.aiPowered ? '✨ Generated with GPT-4o — edit as needed.' : '✨ Generated from trade templates — add OPENAI_API_KEY for smarter results.',
      });
    } catch (e) {
      setAiMsg({ type: 'error', text: 'AI generation failed. Fill in the description manually.' });
    } finally {
      setAiGenerating(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      const jobData = {
        ...formData,
        targetCompletionDate: formData.targetCompletionDate || null,
        actualCompletionDate: formData.actualCompletionDate || null,
      };
      const savedJob = isEditMode
        ? await apiService.jobs.update(id, jobData)
        : await apiService.jobs.create(jobData);
      navigate(`/jobs/${savedJob.id}`);
    } catch (err) {
      setError('Failed to save job. Please check your inputs and try again.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div>Loading job...</div>;

  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">{isEditMode ? 'Edit Job' : 'Create New Job'}</h2>
      </div>

      <div className="card">
        {error && <div className="alert alert-danger">{error}</div>}

        <form onSubmit={handleSubmit}>
          {/* Basic info */}
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="name">Job Name</label>
                <input type="text" id="name" name="name" className="form-control"
                  value={formData.name} onChange={handleChange} required maxLength="100" />
              </div>
            </div>
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="jobNumber">Job Number</label>
                <input type="text" id="jobNumber" name="jobNumber" className="form-control"
                  value={formData.jobNumber} onChange={handleChange} required maxLength="50" />
              </div>
            </div>
          </div>

          {/* ── AI Scope Generator ── */}
          <div style={{ background: 'linear-gradient(135deg, #f0f4ff 0%, #fff8f0 100%)', border: '1px solid #dee2e6', borderRadius: 8, padding: '1rem', marginBottom: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.5rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
                <span style={{ fontWeight: 700, fontSize: '0.85rem', color: '#2F5A7E' }}>✨ AI Scope Generator</span>
                <select value={tradeHint} onChange={e => setTradeHint(e.target.value)}
                  style={{ fontSize: '0.82rem', padding: '4px 8px', borderRadius: 6, border: '1px solid #dee2e6', color: '#495057' }}>
                  {TRADE_HINTS.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </div>
              <button type="button" onClick={handleAiGenerate} disabled={aiGenerating}
                style={{
                  background: aiGenerating ? '#aaa' : 'linear-gradient(135deg, #2F5A7E, #FF9500)',
                  color: '#fff', border: 'none', borderRadius: 6, padding: '6px 16px',
                  fontWeight: 700, fontSize: '0.82rem', cursor: aiGenerating ? 'not-allowed' : 'pointer',
                  whiteSpace: 'nowrap',
                }}>
                {aiGenerating ? '⏳ Generating...' : '✨ Generate Description & Notes'}
              </button>
            </div>
            {aiMsg && (
              <div style={{
                marginTop: '0.5rem', fontSize: '0.8rem', fontWeight: 600, padding: '6px 10px', borderRadius: 5,
                background: aiMsg.type === 'success' ? '#e6f7ee' : '#fdecea',
                color:      aiMsg.type === 'success' ? '#198754' : '#dc3545',
              }}>
                {aiMsg.text}
              </div>
            )}
            <div style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#6c757d' }}>
              Fill in Job Name + Location first, then click Generate. The AI will draft the Description and Notes — you can edit them after.
            </div>
          </div>

          {/* Description */}
          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea id="description" name="description" className="form-control"
              value={formData.description} onChange={handleChange}
              required maxLength="500" rows="3" />
          </div>

          {/* Location */}
          <div className="form-group">
            <label htmlFor="location">Location</label>
            <input type="text" id="location" name="location" className="form-control"
              value={formData.location} onChange={handleChange} required maxLength="200" />
          </div>

          {/* Dates + Status */}
          <div className="row">
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="startDate">Start Date</label>
                <input type="date" id="startDate" name="startDate" className="form-control"
                  value={formData.startDate} onChange={handleChange} required />
              </div>
            </div>
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="targetCompletionDate">Target Completion Date</label>
                <input type="date" id="targetCompletionDate" name="targetCompletionDate" className="form-control"
                  value={formData.targetCompletionDate || ''} onChange={handleChange} />
              </div>
            </div>
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="status">Status</label>
                <select id="status" name="status" className="form-control"
                  value={formData.status} onChange={handleChange} required>
                  <option value="Not Started">Not Started</option>
                  <option value="In Progress">In Progress</option>
                  <option value="Completed">Completed</option>
                  <option value="Delayed">Delayed</option>
                  <option value="Cancelled">Cancelled</option>
                </select>
              </div>
            </div>
          </div>

          {/* Client */}
          <h3 className="mt-4">Client Information</h3>
          <div className="row">
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientName">Client Name</label>
                <input type="text" id="clientName" name="clientName" className="form-control"
                  value={formData.clientName} onChange={handleChange} required maxLength="100" />
              </div>
            </div>
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientEmail">Client Email</label>
                <input type="email" id="clientEmail" name="clientEmail" className="form-control"
                  value={formData.clientEmail} onChange={handleChange} required maxLength="100" />
              </div>
            </div>
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientPhone">Client Phone</label>
                <input type="tel" id="clientPhone" name="clientPhone" className="form-control"
                  value={formData.clientPhone} onChange={handleChange} required maxLength="20" />
              </div>
            </div>
          </div>

          {/* Financials */}
          <h3 className="mt-4">Financial Information</h3>
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="budget">Budget ($)</label>
                <input type="number" id="budget" name="budget" className="form-control"
                  value={formData.budget} onChange={handleChange} required min="0" step="0.01" />
              </div>
            </div>
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="actualCost">Actual Cost ($)</label>
                <input type="number" id="actualCost" name="actualCost" className="form-control"
                  value={formData.actualCost} onChange={handleChange} required min="0" step="0.01" />
              </div>
            </div>
          </div>

          {/* Notes */}
          <div className="form-group">
            <label htmlFor="notes">Notes</label>
            <textarea id="notes" name="notes" className="form-control"
              value={formData.notes || ''} onChange={handleChange} rows="3" />
          </div>

          {/* Actions */}
          <div className="form-group mt-4">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving...' : 'Save Job'}
            </button>
            <button type="button" className="btn btn-secondary ms-2"
              onClick={() => navigate(-1)} disabled={saving}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default JobForm;
