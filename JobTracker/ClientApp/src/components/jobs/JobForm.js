import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

function JobForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEditMode = !!id;
  
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    location: '',
    jobNumber: '',
    startDate: '',
    targetCompletionDate: '',
    status: 'Not Started',
    clientName: '',
    clientEmail: '',
    clientPhone: '',
    budget: 0,
    actualCost: 0,
    notes: ''
  });
  
  const [loading, setLoading] = useState(isEditMode);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    if (isEditMode) {
      fetchJob();
    }
  }, [id]);
  
  const fetchJob = async () => {
    try {
      setLoading(true);
      const job = await apiService.jobs.getById(id);
      
      // Format dates for the form
      const formattedJob = {
        ...job,
        startDate: job.startDate ? new Date(job.startDate).toISOString().split('T')[0] : '',
        targetCompletionDate: job.targetCompletionDate 
          ? new Date(job.targetCompletionDate).toISOString().split('T')[0] 
          : '',
        actualCompletionDate: job.actualCompletionDate 
          ? new Date(job.actualCompletionDate).toISOString().split('T')[0] 
          : ''
      };
      
      setFormData(formattedJob);
      setError(null);
    } catch (err) {
      setError('Failed to load job. Please try again later.');
      console.error('Error fetching job:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const handleChange = (e) => {
    const { name, value, type } = e.target;
    
    // Handle number inputs
    if (type === 'number') {
      setFormData({
        ...formData,
        [name]: parseFloat(value) || 0
      });
      return;
    }
    
    setFormData({
      ...formData,
      [name]: value
    });
  };
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    
    try {
      setSaving(true);
      
      // Create a copy of the form data to send to the API
      const jobData = { ...formData };
      
      // Convert empty strings to null for date fields
      if (jobData.targetCompletionDate === '') {
        jobData.targetCompletionDate = null;
      }
      
      if (jobData.actualCompletionDate === '') {
        jobData.actualCompletionDate = null;
      }
      
      let savedJob;
      
      if (isEditMode) {
        savedJob = await apiService.jobs.update(id, jobData);
      } else {
        savedJob = await apiService.jobs.create(jobData);
      }
      
      // Redirect to the job detail page
      navigate(`/jobs/${savedJob.id}`);
    } catch (err) {
      setError('Failed to save job. Please check your inputs and try again.');
      console.error('Error saving job:', err);
    } finally {
      setSaving(false);
    }
  };
  
  if (loading) {
    return <div>Loading job...</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">{isEditMode ? 'Edit Job' : 'Create New Job'}</h2>
      </div>
      
      <div className="card">
        {error && <div className="alert alert-danger">{error}</div>}
        
        <form onSubmit={handleSubmit}>
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="name">Job Name</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  className="form-control"
                  value={formData.name}
                  onChange={handleChange}
                  required
                  maxLength="100"
                />
              </div>
            </div>
            
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="jobNumber">Job Number</label>
                <input
                  type="text"
                  id="jobNumber"
                  name="jobNumber"
                  className="form-control"
                  value={formData.jobNumber}
                  onChange={handleChange}
                  required
                  maxLength="50"
                />
              </div>
            </div>
          </div>
          
          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              name="description"
              className="form-control"
              value={formData.description}
              onChange={handleChange}
              required
              maxLength="500"
              rows="3"
            ></textarea>
          </div>
          
          <div className="form-group">
            <label htmlFor="location">Location</label>
            <input
              type="text"
              id="location"
              name="location"
              className="form-control"
              value={formData.location}
              onChange={handleChange}
              required
              maxLength="200"
            />
          </div>
          
          <div className="row">
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="startDate">Start Date</label>
                <input
                  type="date"
                  id="startDate"
                  name="startDate"
                  className="form-control"
                  value={formData.startDate}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>
            
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="targetCompletionDate">Target Completion Date</label>
                <input
                  type="date"
                  id="targetCompletionDate"
                  name="targetCompletionDate"
                  className="form-control"
                  value={formData.targetCompletionDate || ''}
                  onChange={handleChange}
                />
              </div>
            </div>
            
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="status">Status</label>
                <select
                  id="status"
                  name="status"
                  className="form-control"
                  value={formData.status}
                  onChange={handleChange}
                  required
                >
                  <option value="Not Started">Not Started</option>
                  <option value="In Progress">In Progress</option>
                  <option value="Completed">Completed</option>
                  <option value="Delayed">Delayed</option>
                  <option value="Cancelled">Cancelled</option>
                </select>
              </div>
            </div>
          </div>
          
          <h3 className="mt-4">Client Information</h3>
          
          <div className="row">
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientName">Client Name</label>
                <input
                  type="text"
                  id="clientName"
                  name="clientName"
                  className="form-control"
                  value={formData.clientName}
                  onChange={handleChange}
                  required
                  maxLength="100"
                />
              </div>
            </div>
            
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientEmail">Client Email</label>
                <input
                  type="email"
                  id="clientEmail"
                  name="clientEmail"
                  className="form-control"
                  value={formData.clientEmail}
                  onChange={handleChange}
                  required
                  maxLength="100"
                />
              </div>
            </div>
            
            <div className="col-md-4">
              <div className="form-group">
                <label htmlFor="clientPhone">Client Phone</label>
                <input
                  type="tel"
                  id="clientPhone"
                  name="clientPhone"
                  className="form-control"
                  value={formData.clientPhone}
                  onChange={handleChange}
                  required
                  maxLength="20"
                />
              </div>
            </div>
          </div>
          
          <h3 className="mt-4">Financial Information</h3>
          
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="budget">Budget ($)</label>
                <input
                  type="number"
                  id="budget"
                  name="budget"
                  className="form-control"
                  value={formData.budget}
                  onChange={handleChange}
                  required
                  min="0"
                  step="0.01"
                />
              </div>
            </div>
            
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="actualCost">Actual Cost ($)</label>
                <input
                  type="number"
                  id="actualCost"
                  name="actualCost"
                  className="form-control"
                  value={formData.actualCost}
                  onChange={handleChange}
                  required
                  min="0"
                  step="0.01"
                />
              </div>
            </div>
          </div>
          
          <div className="form-group">
            <label htmlFor="notes">Notes</label>
            <textarea
              id="notes"
              name="notes"
              className="form-control"
              value={formData.notes || ''}
              onChange={handleChange}
              rows="3"
            ></textarea>
          </div>
          
          <div className="form-group mt-4">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving...' : 'Save Job'}
            </button>
            <button
              type="button"
              className="btn btn-secondary ms-2"
              onClick={() => navigate(-1)}
              disabled={saving}
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default JobForm;