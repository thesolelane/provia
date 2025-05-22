import React, { useEffect, useState } from 'react';
import { 
  Container, 
  Row, 
  Col, 
  Card, 
  CardHeader, 
  CardBody, 
  Table, 
  Badge, 
  Button, 
  Input,
  FormGroup,
  Label,
  Form,
  Collapse
} from 'reactstrap';
import { Link } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchJobs, searchJobs, filterJobs } from '../../store/jobSlice';
import { formatDate } from '../../services/utils';
import './JobList.css';

const statusColors = {
  Planned: 'secondary',
  InProgress: 'primary',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'danger'
};

const JobList = () => {
  const dispatch = useDispatch();
  const { jobs, loading, error } = useSelector(state => state.jobs);
  const { currentUser } = useSelector(state => state.users);
  
  const [searchQuery, setSearchQuery] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [filters, setFilters] = useState({
    status: '',
    startDate: '',
    endDate: ''
  });
  
  useEffect(() => {
    dispatch(fetchJobs());
  }, [dispatch]);
  
  const handleSearch = (e) => {
    e.preventDefault();
    dispatch(searchJobs(searchQuery));
  };
  
  const handleFilter = (e) => {
    e.preventDefault();
    dispatch(filterJobs(filters));
  };
  
  const handleFilterChange = (e) => {
    const { name, value } = e.target;
    setFilters({
      ...filters,
      [name]: value
    });
  };
  
  const resetFilters = () => {
    setFilters({
      status: '',
      startDate: '',
      endDate: ''
    });
    dispatch(fetchJobs());
  };
  
  const isAdmin = currentUser && (currentUser.role === 'Administrator' || currentUser.role === 'ProjectManager');
  
  if (loading && !jobs.length) {
    return <div className="loading-spinner">Loading jobs...</div>;
  }
  
  return (
    <Container className="job-list-container">
      <div className="page-header">
        <h1>Jobs</h1>
        <div className="actions">
          {isAdmin && (
            <Button color="primary" tag={Link} to="/jobs/new">
              <i className="fas fa-plus"></i> New Job
            </Button>
          )}
        </div>
      </div>
      
      <Row className="mb-4">
        <Col md={6}>
          <Form onSubmit={handleSearch} className="search-form">
            <div className="input-group">
              <Input
                type="text"
                placeholder="Search jobs..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
              <div className="input-group-append">
                <Button color="primary" type="submit">
                  <i className="fas fa-search"></i>
                </Button>
              </div>
            </div>
          </Form>
        </Col>
        <Col md={6} className="text-right">
          <Button color="secondary" onClick={() => setShowFilters(!showFilters)}>
            <i className="fas fa-filter"></i> {showFilters ? 'Hide Filters' : 'Show Filters'}
          </Button>
        </Col>
      </Row>
      
      <Collapse isOpen={showFilters}>
        <Card className="mb-4 filter-card">
          <CardBody>
            <Form onSubmit={handleFilter}>
              <Row>
                <Col md={4}>
                  <FormGroup>
                    <Label for="status">Status</Label>
                    <Input
                      type="select"
                      name="status"
                      id="status"
                      value={filters.status}
                      onChange={handleFilterChange}
                    >
                      <option value="">All Statuses</option>
                      <option value="Planned">Planned</option>
                      <option value="InProgress">In Progress</option>
                      <option value="OnHold">On Hold</option>
                      <option value="Completed">Completed</option>
                      <option value="Cancelled">Cancelled</option>
                    </Input>
                  </FormGroup>
                </Col>
                <Col md={4}>
                  <FormGroup>
                    <Label for="startDate">Start Date (After)</Label>
                    <Input
                      type="date"
                      name="startDate"
                      id="startDate"
                      value={filters.startDate}
                      onChange={handleFilterChange}
                    />
                  </FormGroup>
                </Col>
                <Col md={4}>
                  <FormGroup>
                    <Label for="endDate">End Date (Before)</Label>
                    <Input
                      type="date"
                      name="endDate"
                      id="endDate"
                      value={filters.endDate}
                      onChange={handleFilterChange}
                    />
                  </FormGroup>
                </Col>
              </Row>
              <div className="text-right">
                <Button color="secondary" onClick={resetFilters} className="mr-2">
                  Reset
                </Button>
                <Button color="primary" type="submit">
                  Apply Filters
                </Button>
              </div>
            </Form>
          </CardBody>
        </Card>
      </Collapse>
      
      {error && (
        <div className="alert alert-danger" role="alert">
          Error: {error}
        </div>
      )}
      
      <Card>
        <CardBody>
          {jobs.length > 0 ? (
            <div className="table-responsive">
              <Table striped bordered hover>
                <thead>
                  <tr>
                    <th>Job Name</th>
                    <th>Location</th>
                    <th>Status</th>
                    <th>Start Date</th>
                    <th>Expected Completion</th>
                    <th>Project Manager</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {jobs.map(job => (
                    <tr key={job.jobId}>
                      <td>
                        <Link to={`/jobs/${job.jobId}`} className="job-name-link">
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
                      <td>{formatDate(job.expectedCompletionDate)}</td>
                      <td>{job.projectManager}</td>
                      <td>
                        <div className="action-buttons">
                          <Button color="info" size="sm" tag={Link} to={`/jobs/${job.jobId}`}>
                            <i className="fas fa-eye"></i>
                          </Button>
                          {isAdmin && (
                            <Button color="primary" size="sm" tag={Link} to={`/jobs/${job.jobId}/edit`} className="ml-1">
                              <i className="fas fa-edit"></i>
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </div>
          ) : (
            <div className="no-jobs-message">
              <p>No jobs found. {isAdmin && 'Create a new job to get started.'}</p>
              {isAdmin && (
                <Button color="primary" tag={Link} to="/jobs/new">
                  Create New Job
                </Button>
              )}
            </div>
          )}
        </CardBody>
      </Card>
    </Container>
  );
};

export default JobList;
