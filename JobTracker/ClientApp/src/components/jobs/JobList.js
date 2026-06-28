import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/apiService';

function JobList() {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    fetchJobs();
  }, []);
  
  const fetchJobs = async () => {
    try {
      setLoading(true);
      const data = await apiService.jobs.getAll();
      setJobs(data);
      setError(null);
    } catch (err) {
      setError('Failed to load jobs. Please try again later.');
      console.error('Error fetching jobs:', err);
    } finally {
      setLoading(false);
    }
  };
  
  const getStatusClass = (status) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'badge badge-success';
      case 'in progress':
        return 'badge badge-info';
      case 'delayed':
        return 'badge badge-warning';
      case 'cancelled':
        return 'badge badge-danger';
      default:
        return 'badge';
    }
  };
  
  if (loading) {
    return <div>Loading jobs...</div>;
  }
  
  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }
  
  return (
    <div>
      <div className="card-header">
        <h2 className="card-title">Jobs</h2>
        <Link to="/jobs/create" className="btn btn-primary">Create New Job</Link>
      </div>
      
      {jobs.length === 0 ? (
        <div className="card">
          <p>No jobs found. Get started by creating your first job.</p>
        </div>
      ) : (
        <div className="card">
          <table className="table">
            <thead>
              <tr>
                <th>Job Number</th>
                <th>Name</th>
                <th>Location</th>
                <th>Status</th>
                <th>Start Date</th>
                <th>Client</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map(job => (
                <tr key={job.id}>
                  <td>{job.jobNumber}</td>
                  <td>
                    <Link to={`/jobs/${job.id}`}>{job.name}</Link>
                  </td>
                  <td>{job.location}</td>
                  <td>
                    <span className={getStatusClass(job.status)}>
                      {job.status}
                    </span>
                  </td>
                  <td>{new Date(job.startDate).toLocaleDateString()}</td>
                  <td>{job.clientName}</td>
                  <td>
                    <Link to={`/jobs/${job.id}`} className="btn btn-secondary btn-sm">
                      View
                    </Link>
                    <Link to={`/jobs/${job.id}/edit`} className="btn btn-secondary btn-sm ms-2">
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

export default JobList;