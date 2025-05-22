import React, { useEffect, useState } from 'react';
import { 
  Container, 
  Row, 
  Col, 
  Card, 
  CardHeader, 
  CardBody, 
  Button, 
  Badge,
  Nav,
  NavItem,
  NavLink,
  TabContent,
  TabPane,
  Alert,
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter
} from 'reactstrap';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchJobDetail, deleteJob } from '../../store/jobSlice';
import { formatDate, formatCurrency } from '../../services/utils';
import JobSectionList from './JobSectionList';
import TimeSheet from '../time/TimeSheet';
import classnames from 'classnames';
import './JobDetail.css';

const statusColors = {
  Planned: 'secondary',
  InProgress: 'primary',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'danger'
};

const JobDetail = () => {
  const { id } = useParams();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { selectedJob, loading, error } = useSelector(state => state.jobs);
  const { currentUser } = useSelector(state => state.users);
  
  const [activeTab, setActiveTab] = useState('overview');
  const [deleteModal, setDeleteModal] = useState(false);
  
  useEffect(() => {
    if (id) {
      dispatch(fetchJobDetail(id));
    }
  }, [dispatch, id]);
  
  const toggleTab = (tab) => {
    if (activeTab !== tab) {
      setActiveTab(tab);
    }
  };
  
  const toggleDeleteModal = () => {
    setDeleteModal(!deleteModal);
  };
  
  const handleDeleteJob = async () => {
    try {
      await dispatch(deleteJob(id)).unwrap();
      navigate('/jobs');
    } catch (err) {
      console.error('Failed to delete job:', err);
    }
  };
  
  const exportToExcel = () => {
    // This will be implemented in the integration controller
    window.location.href = `/api/integrations/exportjob/${id}/excel`;
  };
  
  const exportToWord = () => {
    // This will be implemented in the integration controller
    window.location.href = `/api/integrations/exportjob/${id}/word`;
  };
  
  if (loading && !selectedJob) {
    return <div className="loading-spinner">Loading job details...</div>;
  }
  
  if (error) {
    return (
      <Alert color="danger">
        Error loading job details: {error}
      </Alert>
    );
  }
  
  if (!selectedJob) {
    return <div>No job found with ID {id}</div>;
  }
  
  const isAdmin = currentUser && (currentUser.role === 'Administrator' || currentUser.role === 'ProjectManager');
  
  return (
    <Container className="job-detail-container">
      <div className="page-header">
        <div>
          <h1>{selectedJob.jobName}</h1>
          <Badge color={statusColors[selectedJob.status] || 'secondary'} className="status-badge">
            {selectedJob.status}
          </Badge>
        </div>
        <div className="actions">
          {isAdmin && (
            <>
              <Button color="primary" tag={Link} to={`/jobs/${id}/edit`} className="mr-2">
                <i className="fas fa-edit"></i> Edit
              </Button>
              <Button color="danger" onClick={toggleDeleteModal}>
                <i className="fas fa-trash-alt"></i> Delete
              </Button>
            </>
          )}
        </div>
      </div>
      
      <Nav tabs className="job-detail-tabs">
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'overview' })}
            onClick={() => toggleTab('overview')}
          >
            Overview
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'sections' })}
            onClick={() => toggleTab('sections')}
          >
            Job Sections
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'time' })}
            onClick={() => toggleTab('time')}
          >
            Time Entries
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'export' })}
            onClick={() => toggleTab('export')}
          >
            Export
          </NavLink>
        </NavItem>
      </Nav>
      
      <TabContent activeTab={activeTab}>
        <TabPane tabId="overview">
          <Row>
            <Col md={6}>
              <Card className="mb-4">
                <CardHeader>
                  <h3>Job Information</h3>
                </CardHeader>
                <CardBody>
                  <div className="detail-item">
                    <span className="detail-label">Location:</span>
                    <span className="detail-value">{selectedJob.location}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Description:</span>
                    <span className="detail-value">{selectedJob.description || 'No description provided'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Project Manager:</span>
                    <span className="detail-value">{selectedJob.projectManager || 'Not assigned'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Budget:</span>
                    <span className="detail-value">{formatCurrency(selectedJob.budget)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Start Date:</span>
                    <span className="detail-value">{formatDate(selectedJob.startDate)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Expected Completion:</span>
                    <span className="detail-value">{formatDate(selectedJob.expectedCompletionDate)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Actual Completion:</span>
                    <span className="detail-value">
                      {selectedJob.actualCompletionDate ? formatDate(selectedJob.actualCompletionDate) : 'Not completed'}
                    </span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Created:</span>
                    <span className="detail-value">{formatDate(selectedJob.createdDate)} by {selectedJob.createdBy}</span>
                  </div>
                </CardBody>
              </Card>
            </Col>
            
            <Col md={6}>
              <Card className="mb-4">
                <CardHeader>
                  <h3>Client Information</h3>
                </CardHeader>
                <CardBody>
                  <div className="detail-item">
                    <span className="detail-label">Client Name:</span>
                    <span className="detail-value">{selectedJob.clientName || 'Not specified'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Phone:</span>
                    <span className="detail-value">{selectedJob.clientPhone || 'Not provided'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Email:</span>
                    <span className="detail-value">{selectedJob.clientEmail || 'Not provided'}</span>
                  </div>
                </CardBody>
              </Card>
              
              <Card>
                <CardHeader>
                  <h3>Notes</h3>
                </CardHeader>
                <CardBody>
                  <p className="notes-content">{selectedJob.notes || 'No notes available for this job.'}</p>
                </CardBody>
              </Card>
            </Col>
          </Row>
        </TabPane>
        
        <TabPane tabId="sections">
          <JobSectionList jobId={id} />
        </TabPane>
        
        <TabPane tabId="time">
          <TimeSheet jobId={id} />
        </TabPane>
        
        <TabPane tabId="export">
          <Card>
            <CardHeader>
              <h3>Export Options</h3>
            </CardHeader>
            <CardBody>
              <Row>
                <Col md={6}>
                  <Card className="export-card">
                    <CardBody>
                      <div className="export-icon">
                        <i className="fas fa-file-excel"></i>
                      </div>
                      <h4>Export to Excel</h4>
                      <p>Export job details and sections to Microsoft Excel spreadsheet</p>
                      <Button color="success" onClick={exportToExcel}>
                        Export to Excel
                      </Button>
                    </CardBody>
                  </Card>
                </Col>
                <Col md={6}>
                  <Card className="export-card">
                    <CardBody>
                      <div className="export-icon">
                        <i className="fas fa-file-word"></i>
                      </div>
                      <h4>Export to Word</h4>
                      <p>Generate a detailed report in Microsoft Word format</p>
                      <Button color="primary" onClick={exportToWord}>
                        Export to Word
                      </Button>
                    </CardBody>
                  </Card>
                </Col>
              </Row>
            </CardBody>
          </Card>
        </TabPane>
      </TabContent>
      
      {/* Delete confirmation modal */}
      <Modal isOpen={deleteModal} toggle={toggleDeleteModal}>
        <ModalHeader toggle={toggleDeleteModal}>Confirm Deletion</ModalHeader>
        <ModalBody>
          Are you sure you want to delete the job "{selectedJob.jobName}"? This action cannot be undone.
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={toggleDeleteModal}>Cancel</Button>
          <Button color="danger" onClick={handleDeleteJob}>Delete Job</Button>
        </ModalFooter>
      </Modal>
    </Container>
  );
};

export default JobDetail;
