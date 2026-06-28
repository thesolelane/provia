import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { apiService } from '../../services/apiService';

function JobSectionForm() {
  const { id, sectionId } = useParams();
  const navigate = useNavigate();
  const isEditMode = !!sectionId;
  
  const [formData, setFormData] = useState({
    jobId: id ? parseInt(id) : 0,
    sectionType: 0,
    description: '',
    status: 0,
    startDate: '',
    completionDate: '',
    isSubcontracted: false,
    subcontractorId: null,
    contractReference: '',
    responsibleEmployeeId: null,
    materialsOrdered: false,
    materialsDelivered: false,
    inspectionDate: '',
    inspectionNotes: ''
  });
  
  const [loading, setLoading] = useState(isEditMode);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  
  const sectionTypes = [
    { id: 0, name: 'Demolition' },
    { id: 1, name: 'Foundation' },
    { id: 2, name: 'Framing' },
    { id: 3, name: 'Electrical' },
    { id: 4, name: 'Plumbing' },
    { id: 5, name: 'HVAC' },
    { id: 6, name: 'Drywall' },
    { id: 7, name: 'Painting' },
    { id: 8, name: 'Flooring' },
    { id: 9, name: 'Finishing' },
    { id: 10, name: 'Inspection' }
  ];
  
  const statusOptions = [
    { id: 0, name: 'Not Started' },
    { id: 1, name: 'In Progress' },
    { id: 2, name: 'Completed' },
    { id: 3, name: 'Delayed' },
    { id: 4, name: 'Cancelled' }
  ];
  
  useEffect(() => {
    if (isEditMode && sectionId) {
      fetchSection();
    }
  }, [sectionId]);
  
  const fetchSection = async () => {
    try {
      setLoading(true);
      const section = await apiService.jobSections.getById(sectionId);
      
      // Format dates for the form
      const formattedSection = {
        ...section,
        startDate: section.startDate ? new Date(section.startDate).toISOString().split('T')[0] : '',
        completionDate: section.completionDate ? new Date(section.completionDate).toISOString().split('T')[0] : '',
        inspectionDate: section.inspectionDate ? new Date(section.inspectionDate).toISOString().split('T')[0] : ''
      };
      
      setFormData(formattedSection);
      setError(null);
    } catch (err) {
      setError('Failed to load section. Please try again later.');
      console.error('Error fetching section:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    
    // Handle checkboxes
    if (type === 'checkbox') {
      setFormData({
        ...formData,
        [name]: checked
      });
      return;
    }
    
    // Handle number inputs
    if (type === 'number' || name === 'sectionType' || name === 'status') {
      setFormData({
        ...formData,
        [name]: parseInt(value)
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
      const sectionData = { ...formData };
      
      // Convert empty strings to null for date fields
      if (sectionData.startDate === '') {
        sectionData.startDate = null;
      }
      
      if (sectionData.completionDate === '') {
        sectionData.completionDate = null;
      }
      
      if (sectionData.inspectionDate === '') {
        sectionData.inspectionDate = null;
      }
      
      let savedSection;
      
      if (isEditMode) {
        savedSection = await apiService.jobSections.update(sectionId, sectionData);
      } else {
        savedSection = await apiService.jobSections.create(sectionData);
      }
      
      // Redirect to the job detail page
      navigate(`/jobs/${formData.jobId}`);
    } catch (err) {
      setError('Failed to save section. Please check your inputs and try again.');
      console.error('Error saving section:', err);
    } finally {
      setSaving(false);
    }
  };
  
  if (loading) {
    return <div>Loading section...</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">
          {isEditMode ? 'Edit Job Section' : 'Add New Job Section'}
        </h2>
      </div>
      
      <div className="card">
        {error && <div className="alert alert-danger">{error}</div>}
        
        <form onSubmit={handleSubmit}>
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="sectionType">Section Type</label>
                <select
                  id="sectionType"
                  name="sectionType"
                  className="form-control"
                  value={formData.sectionType}
                  onChange={handleChange}
                  required
                >
                  {sectionTypes.map(type => (
                    <option key={type.id} value={type.id}>
                      {type.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            
            <div className="col-md-6">
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
                  {statusOptions.map(status => (
                    <option key={status.id} value={status.id}>
                      {status.name}
                    </option>
                  ))}
                </select>
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
          
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="startDate">Start Date</label>
                <input
                  type="date"
                  id="startDate"
                  name="startDate"
                  className="form-control"
                  value={formData.startDate || ''}
                  onChange={handleChange}
                />
              </div>
            </div>
            
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="completionDate">Completion Date</label>
                <input
                  type="date"
                  id="completionDate"
                  name="completionDate"
                  className="form-control"
                  value={formData.completionDate || ''}
                  onChange={handleChange}
                />
              </div>
            </div>
          </div>
          
          <div className="form-group form-check mt-3">
            <input
              type="checkbox"
              id="isSubcontracted"
              name="isSubcontracted"
              className="form-check-input"
              checked={formData.isSubcontracted}
              onChange={handleChange}
            />
            <label className="form-check-label" htmlFor="isSubcontracted">
              Is Subcontracted
            </label>
          </div>
          
          {formData.isSubcontracted && (
            <div className="row mt-3">
              <div className="col-md-6">
                <div className="form-group">
                  <label htmlFor="subcontractorId">Subcontractor</label>
                  <select
                    id="subcontractorId"
                    name="subcontractorId"
                    className="form-control"
                    value={formData.subcontractorId || ''}
                    onChange={handleChange}
                  >
                    <option value="">Select Subcontractor</option>
                    {/* Subcontractor options would be fetched from API */}
                    <option value="1">Sample Subcontractor (Placeholder)</option>
                  </select>
                </div>
              </div>
              
              <div className="col-md-6">
                <div className="form-group">
                  <label htmlFor="contractReference">Contract Reference</label>
                  <input
                    type="text"
                    id="contractReference"
                    name="contractReference"
                    className="form-control"
                    value={formData.contractReference || ''}
                    onChange={handleChange}
                    maxLength="100"
                  />
                </div>
              </div>
            </div>
          )}
          
          <div className="row mt-3">
            <div className="col-md-6">
              <div className="form-group form-check">
                <input
                  type="checkbox"
                  id="materialsOrdered"
                  name="materialsOrdered"
                  className="form-check-input"
                  checked={formData.materialsOrdered}
                  onChange={handleChange}
                />
                <label className="form-check-label" htmlFor="materialsOrdered">
                  Materials Ordered
                </label>
              </div>
            </div>
            
            <div className="col-md-6">
              <div className="form-group form-check">
                <input
                  type="checkbox"
                  id="materialsDelivered"
                  name="materialsDelivered"
                  className="form-check-input"
                  checked={formData.materialsDelivered}
                  onChange={handleChange}
                />
                <label className="form-check-label" htmlFor="materialsDelivered">
                  Materials Delivered
                </label>
              </div>
            </div>
          </div>
          
          <h3 className="mt-4">Inspection Details</h3>
          
          <div className="row">
            <div className="col-md-6">
              <div className="form-group">
                <label htmlFor="inspectionDate">Inspection Date</label>
                <input
                  type="date"
                  id="inspectionDate"
                  name="inspectionDate"
                  className="form-control"
                  value={formData.inspectionDate || ''}
                  onChange={handleChange}
                />
              </div>
            </div>
          </div>
          
          <div className="form-group">
            <label htmlFor="inspectionNotes">Inspection Notes</label>
            <textarea
              id="inspectionNotes"
              name="inspectionNotes"
              className="form-control"
              value={formData.inspectionNotes || ''}
              onChange={handleChange}
              rows="3"
            ></textarea>
          </div>
          
          <div className="form-group mt-4">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving...' : 'Save Section'}
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

export default JobSectionForm;