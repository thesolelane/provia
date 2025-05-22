import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

function Dashboard() {
  const [recentJobs, setRecentJobs] = useState([]);
  const [jobStats, setJobStats] = useState({
    total: 0,
    inProgress: 0,
    completed: 0,
    delayed: 0
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    // In a full implementation, we would fetch dashboard data from the API
    // For now, we'll just fetch jobs and calculate stats from them
    fetchJobs();
  }, []);

  const fetchJobs = async () => {
    try {
      setLoading(true);
      const response = await fetch('/api/jobs');
      
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      
      const data = await response.json();
      
      // Get only 5 most recent jobs
      const sortedJobs = [...data].sort((a, b) => 
        new Date(b.createdAt) - new Date(a.createdAt)
      ).slice(0, 5);
      
      setRecentJobs(sortedJobs);
      
      // Calculate stats
      const stats = {
        total: data.length,
        inProgress: data.filter(job => job.status.toLowerCase() === 'in progress').length,
        completed: data.filter(job => job.status.toLowerCase() === 'completed').length,
        delayed: data.filter(job => job.status.toLowerCase() === 'delayed').length
      };
      
      setJobStats(stats);
      setError(null);
    } catch (err) {
      setError('Failed to load dashboard data. Please try again later.');
      console.error('Error fetching dashboard data:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div>Loading dashboard...</div>;
  }

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  return (
    <div>
      <h2>Dashboard</h2>
      
      {/* Stats Cards */}
      <div className="grid">
        <div className="card">
          <h3>Total Jobs</h3>
          <p className="dashboard-stat">{jobStats.total}</p>
          <Link to="/jobs">View All Jobs</Link>
        </div>
        
        <div className="card">
          <h3>In Progress</h3>
          <p className="dashboard-stat">{jobStats.inProgress}</p>
        </div>
        
        <div className="card">
          <h3>Completed</h3>
          <p className="dashboard-stat">{jobStats.completed}</p>
        </div>
        
        <div className="card">
          <h3>Delayed</h3>
          <p className="dashboard-stat">{jobStats.delayed}</p>
        </div>
      </div>
      
      {/* Recent Jobs */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Recent Jobs</h3>
          <Link to="/jobs" className="btn btn-primary">View All</Link>
        </div>
        
        {recentJobs.length === 0 ? (
          <p>No jobs found. Get started by creating your first job.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Job Number</th>
                <th>Name</th>
                <th>Status</th>
                <th>Client</th>
              </tr>
            </thead>
            <tbody>
              {recentJobs.map(job => (
                <tr key={job.id}>
                  <td>{job.jobNumber}</td>
                  <td>
                    <Link to={`/jobs/${job.id}`}>{job.name}</Link>
                  </td>
                  <td>{job.status}</td>
                  <td>{job.clientName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

export default Dashboard;