import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';

function JobSectionDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [section, setSection] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    fetchSection();
  }, [id]);
  
  const fetchSection = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/jobsections/${id}`);
      
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error('Section not found');
        }
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      const data = await response.json();
      setSection(data);
      setError(null);
    } catch (err) {
      setError('Failed to load section details. Please try again later.');
      console.error('Error fetching section:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const deleteSection = async () => {
    if (!window.confirm('Are you sure you want to delete this section? This action cannot be undone.')) {
      return;
    }
    
    try {
      const response = await fetch(`/api/jobsections/${id}`, {
        method: 'DELETE',
      });
      
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      navigate(`/jobs/${section.jobId}`);
    } catch (err) {
      setError('Failed to delete section. Please try again later.');
      console.error('Error deleting section:', err);
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
    return <div>Loading section details...</div>;
  }
  
  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }
  
  if (!section) {
    return <div className="alert alert-warning">Section not found</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">
          Section Details: {getSectionName(section.sectionType)}
        </h2>
        <div>
          <Link to={`/sections/${id}/edit`} className="btn btn-primary">
            Edit Section
          </Link>
          <button onClick={deleteSection} className="btn btn-danger ms-2">
            Delete Section
          </button>
          <Link to={`/jobs/${section.jobId}`} className="btn btn-secondary ms-2">
            Back to Job
          </Link>
        </div>
      </div>
      
      <div className="card">
        <div className="row">
          <div className="col-md-6">
            <h3>Basic Information</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Section Type</th>
                  <td>{getSectionName(section.sectionType)}</td>
                </tr>
                <tr>
                  <th>Description</th>
                  <td>{section.description}</td>
                </tr>
                <tr>
                  <th>Status</th>
                  <td>{getSectionStatus(section.status)}</td>
                </tr>
                <tr>
                  <th>Job</th>
                  <td>
                    <Link to={`/jobs/${section.jobId}`}>
                      Go to Job
                    </Link>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          
          <div className="col-md-6">
            <h3>Dates</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Start Date</th>
                  <td>
                    {section.startDate 
                      ? new Date(section.startDate).toLocaleDateString() 
                      : 'Not started'}
                  </td>
                </tr>
                <tr>
                  <th>Completion Date</th>
                  <td>
                    {section.completionDate 
                      ? new Date(section.completionDate).toLocaleDateString() 
                      : 'Not completed'}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        
        <div className="row mt-4">
          <div className="col-md-6">
            <h3>Subcontractor Information</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Is Subcontracted</th>
                  <td>{section.isSubcontracted ? 'Yes' : 'No'}</td>
                </tr>
                {section.isSubcontracted && (
                  <>
                    <tr>
                      <th>Subcontractor</th>
                      <td>
                        {section.subcontractorId 
                          ? `Subcontractor ID: ${section.subcontractorId}` 
                          : 'Not assigned'}
                      </td>
                    </tr>
                    <tr>
                      <th>Contract Reference</th>
                      <td>{section.contractReference || 'Not specified'}</td>
                    </tr>
                  </>
                )}
              </tbody>
            </table>
          </div>
          
          <div className="col-md-6">
            <h3>Materials Status</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Materials Ordered</th>
                  <td>{section.materialsOrdered ? 'Yes' : 'No'}</td>
                </tr>
                <tr>
                  <th>Materials Delivered</th>
                  <td>{section.materialsDelivered ? 'Yes' : 'No'}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        
        <div className="row mt-4">
          <div className="col">
            <h3>Inspection Details</h3>
            <table className="table">
              <tbody>
                <tr>
                  <th>Inspection Date</th>
                  <td>
                    {section.inspectionDate 
                      ? new Date(section.inspectionDate).toLocaleDateString() 
                      : 'Not scheduled'}
                  </td>
                </tr>
                {section.inspectionNotes && (
                  <tr>
                    <th>Inspection Notes</th>
                    <td>{section.inspectionNotes}</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

export default JobSectionDetail;