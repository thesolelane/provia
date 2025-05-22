import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

function JobSectionList() {
  const [sections, setSections] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    fetchSections();
  }, []);
  
  const fetchSections = async () => {
    try {
      setLoading(true);
      const data = await apiService.jobSections.getAll();
      setSections(data);
      setError(null);
    } catch (err) {
      setError('Failed to load sections. Please try again later.');
      console.error('Error fetching sections:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const getSectionStatus = (status) => {
    switch (status) {
      case 0: return 'Not Started';
      case 1: return 'In Progress';
      case 2: return 'Completed';
      case 3: return 'Delayed';
      case 4: return 'Cancelled';
      default: return 'Unknown';
    }
  };
  
  const getSectionName = (sectionType) => {
    switch (sectionType) {
      case 0: return 'Demolition';
      case 1: return 'Foundation';
      case 2: return 'Framing';
      case 3: return 'Electrical';
      case 4: return 'Plumbing';
      case 5: return 'HVAC';
      case 6: return 'Drywall';
      case 7: return 'Painting';
      case 8: return 'Flooring';
      case 9: return 'Finishing';
      case 10: return 'Inspection';
      default: return 'Unknown';
    }
  };
  
  const getStatusClass = (status) => {
    switch (status) {
      case 0: return 'badge';
      case 1: return 'badge badge-info';
      case 2: return 'badge badge-success';
      case 3: return 'badge badge-warning';
      case 4: return 'badge badge-danger';
      default: return 'badge';
    }
  };
  
  if (loading) {
    return <div>Loading sections...</div>;
  }
  
  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">All Job Sections</h2>
      </div>
      
      {sections.length === 0 ? (
        <div className="card">
          <p>No sections found. Create a job and add sections to it.</p>
        </div>
      ) : (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Section Type</th>
                <th>Job</th>
                <th>Status</th>
                <th>Subcontracted</th>
                <th>Start Date</th>
                <th>Completion Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {sections.map(section => (
                <tr key={section.id}>
                  <td>{getSectionName(section.sectionType)}</td>
                  <td>
                    <Link to={`/jobs/${section.jobId}`}>
                      {section.jobName || `Job #${section.jobId}`}
                    </Link>
                  </td>
                  <td>
                    <span className={getStatusClass(section.status)}>
                      {getSectionStatus(section.status)}
                    </span>
                  </td>
                  <td>{section.isSubcontracted ? 'Yes' : 'No'}</td>
                  <td>
                    {section.startDate 
                      ? new Date(section.startDate).toLocaleDateString() 
                      : 'Not started'}
                  </td>
                  <td>
                    {section.completionDate 
                      ? new Date(section.completionDate).toLocaleDateString() 
                      : 'Not completed'}
                  </td>
                  <td>
                    <Link to={`/sections/${section.id}`} className="btn btn-secondary btn-sm">
                      View
                    </Link>
                    <Link to={`/sections/${section.id}/edit`} className="btn btn-secondary btn-sm ms-2">
                      Edit
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default JobSectionList;