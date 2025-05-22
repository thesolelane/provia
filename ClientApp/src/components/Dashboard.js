import React, { useEffect, useState } from 'react';
import { Container, Row, Col, Card, CardHeader, CardBody, Table, Badge, Alert } from 'reactstrap';
import { Link } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchDashboardData } from '../store/jobSlice';
import { formatDate } from '../services/utils';
import './Dashboard.css';

const statusColors = {
  Planned: 'secondary',
  InProgress: 'primary',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'danger'
};

const sectionStatusColors = {
  NotStarted: 'secondary',
  InProgress: 'primary',
  Completed: 'success',
  Failed: 'danger',
  OnHold: 'warning',
  WaitingForInspection: 'info',
  WaitingForMaterials: 'dark'
};

const Dashboard = () => {
  const dispatch = useDispatch();
  const { dashboardData, loading, error } = useSelector(state => state.jobs);
  const { currentUser } = useSelector(state => state.users);
  const [refreshInterval, setRefreshInterval] = useState(null);
  
  useEffect(() => {
    dispatch(fetchDashboardData());
    
    // Set up auto-refresh every 5 minutes
    const interval = setInterval(() => {
      dispatch(fetchDashboardData());
    }, 5 * 60 * 1000);
    
    setRefreshInterval(interval);
    
    return () => {
      if (refreshInterval) {
        clearInterval(refreshInterval);
      }
    };
  }, [dispatch]);
  
  if (loading && !dashboardData) {
    return <div className="loading-spinner">Loading dashboard...</div>;
  }
  
  if (error) {
    return (
      <Alert color="danger">
        Error loading dashboard: {error}
      </Alert>
    );
  }
  
  // If no data yet, show a placeholder
  if (!dashboardData) {
    return <div>No dashboard data available</div>;
  }
  
  const { 
    totalJobs, 
    activeJobs, 
    completedJobs, 
    onHoldJobs, 
    sectionStatus, 
    recentJobs, 
    upcomingInspections 
  } = dashboardData;
  
  const isAdmin = currentUser && (currentUser.role === 'Administrator' || currentUser.role === 'ProjectManager');

  return (
    <Container className="dashboard-container">
      <h1 className="dashboard-title">Dashboard</h1>
      
      <Row className="stats-overview">
        <Col sm={12} md={3}>
          <Card className="stat-card">
            <CardBody>
              <div className="stat-icon">
                <i className="fas fa-clipboard-list"></i>
              </div>
              <div className="stat-content">
                <h2>{totalJobs}</h2>
                <p>Total Jobs</p>
              </div>
            </CardBody>
          </Card>
        </Col>
        <Col sm={12} md={3}>
          <Card className="stat-card">
            <CardBody>
              <div className="stat-icon active">
                <i className="fas fa-hammer"></i>
              </div>
              <div className="stat-content">
                <h2>{activeJobs}</h2>
                <p>Active Jobs</p>
              </div>
            </CardBody>
          </Card>
        </Col>
        <Col sm={12} md={3}>
          <Card className="stat-card">
            <CardBody>
              <div className="stat-icon completed">
                <i className="fas fa-check-circle"></i>
              </div>
              <div className="stat-content">
                <h2>{completedJobs}</h2>
                <p>Completed Jobs</p>
              </div>
            </CardBody>
          </Card>
        </Col>
        <Col sm={12} md={3}>
          <Card className="stat-card">
            <CardBody>
              <div className="stat-icon hold">
                <i className="fas fa-pause-circle"></i>
              </div>
              <div className="stat-content">
                <h2>{onHoldJobs}</h2>
                <p>On Hold</p>
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
      
      <Row className="dashboard-details">
        <Col md={6}>
          <Card className="mb-4">
            <CardHeader>
              <h3>Recent Jobs</h3>
            </CardHeader>
            <CardBody>
              <div className="table-responsive">
                {recentJobs && recentJobs.length > 0 ? (
                  <Table striped bordered hover>
                    <thead>
                      <tr>
                        <th>Job Name</th>
                        <th>Location</th>
                        <th>Status</th>
                        <th>Start Date</th>
                      </tr>
                    </thead>
                    <tbody>
                      {recentJobs.map(job => (
                        <tr key={job.jobId}>
                          <td>
                            <Link to={`/jobs/${job.jobId}`}>
                              {job.jobName}
                            </Link>
                          </td>
                          <td>{job.location}</td>
                          <td>
                            <Badge color={statusColors[job.status] || 'secondary'}>
                              {job.status}
                            </Badge>
                          </td>
                          <td>{formatDate(job.startDate)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                ) : (
                  <div className="no-data-message">No recent jobs</div>
                )}
              </div>
              <div className="text-center mt-3">
                <Link to="/jobs" className="btn btn-outline-primary">View All Jobs</Link>
                {isAdmin && (
                  <Link to="/jobs/new" className="btn btn-primary ml-2">Create New Job</Link>
                )}
              </div>
            </CardBody>
          </Card>
        </Col>
        
        <Col md={6}>
          <Card className="mb-4">
            <CardHeader>
              <h3>Upcoming Inspections</h3>
            </CardHeader>
            <CardBody>
              <div className="table-responsive">
                {upcomingInspections && upcomingInspections.length > 0 ? (
                  <Table striped bordered hover>
                    <thead>
                      <tr>
                        <th>Job</th>
                        <th>Section</th>
                        <th>Inspection Date</th>
                      </tr>
                    </thead>
                    <tbody>
                      {upcomingInspections.map(section => (
                        <tr key={section.sectionId}>
                          <td>
                            <Link to={`/jobs/${section.jobId}`}>
                              {section.job.jobName}
                            </Link>
                          </td>
                          <td>
                            <Link to={`/sections/${section.sectionId}`}>
                              {section.type}
                            </Link>
                          </td>
                          <td>{formatDate(section.inspectionDate)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </Table>
                ) : (
                  <div className="no-data-message">No upcoming inspections</div>
                )}
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
      
      <Row>
        <Col md={12}>
          <Card>
            <CardHeader>
              <h3>Section Status Overview</h3>
            </CardHeader>
            <CardBody>
              <div className="section-status-container">
                {sectionStatus && Object.entries(sectionStatus).map(([status, count]) => (
                  <div key={status} className="section-status-item">
                    <div className="status-badge-wrapper">
                      <Badge color={sectionStatusColors[status] || 'secondary'} className="status-badge">
                        {count}
                      </Badge>
                    </div>
                    <div className="status-label">{status}</div>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default Dashboard;
