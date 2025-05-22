import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';

function JobDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [job, setJob] = useState(null);
  const [sections, setSections] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    fetchJob();
    fetchJobSections();
  }, [id]);
  
  const fetchJob = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/jobs/${id}`);
      
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error('Job not found');
        }
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      const data = await response.json();
      setJob(data);
      setError(null);
    } catch (err) {
      setError('Failed to load job details. Please try again later.');
      console.error('Error fetching job:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const fetchJobSections = async () => {
    try {
      const response = await fetch(`/api/jobsections/job/${id}`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      const data = await response.json();
      setSections(data);
    } catch (err) {
      console.error('Error fetching job sections:', err);
    }
  };
  
  const deleteJob = async () => {
    if (!window.confirm('Are you sure you want to delete this job? This action cannot be undone.')) {
      return;
    }
    
    try {
      const response = await fetch(`/api/jobs/${id}`, {
        method: 'DELETE',
      });
      
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      navigate('/jobs');
    } catch (err) {
      setError('Failed to delete job. Please try again later.');
      console.error('Error deleting job:', err);
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
  
  if (loading) {
    return <div>Loading job details...</div>;
  }
  
  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }
  
  if (!job) {
    return <div className="alert alert-warning">Job not found</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">Job Details: {job.name}</h2>
        <div>
          <Link to={`/jobs/${id}/edit`} className="btn btn-primary">Edit Job</Link>
          <button onClick={deleteJob} className="btn btn-danger ms-2">Delete Job</button>
        </div>
      </div>
      
      <div className="card">
        <div className="row">
          <div className="col">
            <h3>Basic Information</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Job Number</th>
                  <td>{job.jobNumber}</td>
                </tr>
                <tr>
                  <th>Description</th>
                  <td>{job.description}</td>
                </tr>
                <tr>
                  <th>Location</th>
                  <td>{job.location}</td>
                </tr>
                <tr>
                  <th>Status</th>
                  <td>{job.status}</td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <div className="col">
            <h3>Dates</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Start Date</th>
                  <td>{new Date(job.startDate).toLocaleDateString()}</td>
                </tr>
                <tr>
                  <th>Target Completion</th>
                  <td>{job.targetCompletionDate ? new Date(job.targetCompletionDate).toLocaleDateString() : 'Not set'}</td>
                </tr>
                <tr>
                  <th>Actual Completion</th>
                  <td>{job.actualCompletionDate ? new Date(job.actualCompletionDate).toLocaleDateString() : 'Not completed'}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        
        <div className="row">
          <div className="col">
            <h3>Client Information</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Name</th>
                  <td>{job.clientName}</td>
                </tr>
                <tr>
                  <th>Email</th>
                  <td>{job.clientEmail}</td>
                </tr>
                <tr>
                  <th>Phone</th>
                  <td>{job.clientPhone}</td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <div className="col">
            <h3>Financial</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Budget</th>
                  <td>${job.budget.toFixed(2)}</td>
                </tr>
                <tr>
                  <th>Actual Cost</th>
                  <td>${job.actualCost.toFixed(2)}</td>
                </tr>
                <tr>
                  <th>Variance</th>
                  <td>${(job.budget - job.actualCost).toFixed(2)}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        
        {job.notes && (
          <div className="row">
            <div className="col">
              <h3>Notes</h3>
              <p>{job.notes}</p>
            </div>
          </div>
        )}
      </div>
      
      {/* Job Sections */}
      <div className="card mt-4">
        <div className="card-header">
          <h3 className="card-title">Job Sections</h3>
          <Link to={`/jobs/${id}/sections/create`} className="btn btn-primary">Add Section</Link>
        </div>
        
        {sections.length === 0 ? (
          <p>No sections found for this job.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Section Type</th>
                <th>Status</th>
                <th>Start Date</th>
                <th>Completion Date</th>
                <th>Subcontracted</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {sections.map(section => (
                <tr key={section.id}>
                  <td>{getSectionName(section.sectionType)}</td>
                  <td>{getSectionStatus(section.status)}</td>
                  <td>{section.startDate ? new Date(section.startDate).toLocaleDateString() : 'Not started'}</td>
                  <td>{section.completionDate ? new Date(section.completionDate).toLocaleDateString() : 'Not completed'}</td>
                  <td>{section.isSubcontracted ? 'Yes' : 'No'}</td>
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
        )}
      </div>
    </div>
  );
}

export default JobDetail;