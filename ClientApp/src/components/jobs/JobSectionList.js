import React, { useEffect, useState } from 'react';
import { 
  Card, 
  CardHeader, 
  CardBody, 
  Table, 
  Badge, 
  Button, 
  Progress, 
  Alert,
  Row,
  Col,
  Collapse
} from 'reactstrap';
import { Link } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchJobSections } from '../../store/jobSlice';
import { formatDate } from '../../services/utils';
import './JobSectionList.css';

const sectionStatusColors = {
  NotStarted: 'secondary',
  InProgress: 'primary',
  Completed: 'success',
  Failed: 'danger',
  OnHold: 'warning',
  WaitingForInspection: 'info',
  WaitingForMaterials: 'dark'
};

const getProgressPercentage = (sections) => {
  if (!sections || sections.length === 0) return 0;
  
  const completedCount = sections.filter(s => s.status === 'Completed').length;
  return Math.round((completedCount / sections.length) * 100);
};

const JobSectionList = ({ jobId }) => {
  const dispatch = useDispatch();
  const { jobSections, loading, error } = useSelector(state => state.jobs);
  const { currentUser } = useSelector(state => state.users);
  const [expandedSections, setExpandedSections] = useState({});
  
  useEffect(() => {
    if (jobId) {
      dispatch(fetchJobSections(jobId));
    }
  }, [dispatch, jobId]);
  
  const toggleSection = (sectionId) => {
    setExpandedSections(prevState => ({
      ...prevState,
      [sectionId]: !prevState[sectionId]
    }));
  };
  
  if (loading && !jobSections.length) {
    return <div className="loading-spinner">Loading job sections...</div>;
  }
  
  if (error) {
    return (
      <Alert color="danger">
        Error: {error}
      </Alert>
    );
  }
  
  // Group sections by status for better visualization
  const sections = {
    inProgress: jobSections.filter(s => s.status === 'InProgress'),
    waitingForInspection: jobSections.filter(s => s.status === 'WaitingForInspection'),
    waitingForMaterials: jobSections.filter(s => s.status === 'WaitingForMaterials'),
    notStarted: jobSections.filter(s => s.status === 'NotStarted'),
    completed: jobSections.filter(s => s.status === 'Completed'),
    failed: jobSections.filter(s => s.status === 'Failed'),
    onHold: jobSections.filter(s => s.status === 'OnHold')
  };
  
  const progressPercentage = getProgressPercentage(jobSections);
  
  return (
    <div className="job-sections-container">
      <div className="progress-overview mb-4">
        <div className="progress-info">
          <h3>Overall Progress</h3>
          <span className="progress-percentage">{progressPercentage}%</span>
        </div>
        <Progress value={progressPercentage} className="section-progress" />
      </div>
      
      {/* Active Sections */}
      {sections.inProgress.length > 0 && (
        <Card className="mb-4 section-group">
          <CardHeader className="section-group-header">
            <h3>
              <Badge color="primary" className="status-indicator">
                {sections.inProgress.length}
              </Badge>
              In Progress
            </h3>
          </CardHeader>
          <CardBody>
            {sections.inProgress.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
          </CardBody>
        </Card>
      )}
      
      {/* Waiting Sections */}
      {(sections.waitingForInspection.length > 0 || sections.waitingForMaterials.length > 0) && (
        <Card className="mb-4 section-group">
          <CardHeader className="section-group-header">
            <h3>
              <Badge color="info" className="status-indicator">
                {sections.waitingForInspection.length + sections.waitingForMaterials.length}
              </Badge>
              Waiting
            </h3>
          </CardHeader>
          <CardBody>
            {sections.waitingForInspection.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
            {sections.waitingForMaterials.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
          </CardBody>
        </Card>
      )}
      
      {/* Not Started Sections */}
      {sections.notStarted.length > 0 && (
        <Card className="mb-4 section-group">
          <CardHeader className="section-group-header">
            <h3>
              <Badge color="secondary" className="status-indicator">
                {sections.notStarted.length}
              </Badge>
              Not Started
            </h3>
          </CardHeader>
          <CardBody>
            {sections.notStarted.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
          </CardBody>
        </Card>
      )}
      
      {/* Completed Sections */}
      {sections.completed.length > 0 && (
        <Card className="mb-4 section-group">
          <CardHeader className="section-group-header">
            <h3>
              <Badge color="success" className="status-indicator">
                {sections.completed.length}
              </Badge>
              Completed
            </h3>
          </CardHeader>
          <CardBody>
            {sections.completed.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
          </CardBody>
        </Card>
      )}
      
      {/* Problem Sections */}
      {(sections.failed.length > 0 || sections.onHold.length > 0) && (
        <Card className="mb-4 section-group">
          <CardHeader className="section-group-header">
            <h3>
              <Badge color="danger" className="status-indicator">
                {sections.failed.length + sections.onHold.length}
              </Badge>
              Issues
            </h3>
          </CardHeader>
          <CardBody>
            {sections.failed.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
            {sections.onHold.map(section => (
              <SectionCard 
                key={section.sectionId} 
                section={section} 
                expanded={expandedSections[section.sectionId]}
                toggleSection={toggleSection}
                currentUser={currentUser}
              />
            ))}
          </CardBody>
        </Card>
      )}
      
      {jobSections.length === 0 && (
        <Alert color="info">
          No job sections found for this job.
        </Alert>
      )}
    </div>
  );
};

// Individual section card component
const SectionCard = ({ section, expanded, toggleSection, currentUser }) => {
  const isSubcontracted = section.isSubcontracted;
  
  return (
    <Card className="section-card mb-3">
      <CardHeader className="section-card-header" onClick={() => toggleSection(section.sectionId)}>
        <div className="section-header-content">
          <Badge color={sectionStatusColors[section.status] || 'secondary'} className="section-status">
            {section.status}
          </Badge>
          <h4>{section.type}</h4>
          {isSubcontracted && (
            <Badge color="dark" className="ml-2">Subcontracted</Badge>
          )}
        </div>
        <div className="section-header-actions">
          <Button color="link" className="expand-btn">
            <i className={`fas fa-chevron-${expanded ? 'up' : 'down'}`}></i>
          </Button>
        </div>
      </CardHeader>
      <Collapse isOpen={expanded}>
        <CardBody>
          <Row>
            <Col md={isSubcontracted ? 6 : 4}>
              <div className="detail-item">
                <span className="detail-label">Start Date:</span>
                <span className="detail-value">{formatDate(section.startDate) || 'Not set'}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Expected Completion:</span>
                <span className="detail-value">{formatDate(section.expectedCompletionDate) || 'Not set'}</span>
              </div>
              <div className="detail-item">
                <span className="detail-label">Actual Completion:</span>
                <span className="detail-value">{formatDate(section.actualCompletionDate) || 'Not completed'}</span>
              </div>
              {section.inspectionDate && (
                <div className="detail-item">
                  <span className="detail-label">Inspection Date:</span>
                  <span className="detail-value">{formatDate(section.inspectionDate)}</span>
                </div>
              )}
            </Col>
            
            {isSubcontracted ? (
              <Col md={6}>
                <div className="detail-item">
                  <span className="detail-label">Subcontractor:</span>
                  <span className="detail-value">{section.subcontractor?.companyName || 'Not assigned'}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Contact:</span>
                  <span className="detail-value">{section.subcontractor?.contactName || 'Not specified'}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Phone:</span>
                  <span className="detail-value">{section.subcontractor?.phone || 'Not provided'}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Contract Reference:</span>
                  <span className="detail-value">{section.contractReference || 'Not specified'}</span>
                </div>
              </Col>
            ) : (
              <>
                <Col md={4}>
                  <div className="detail-item">
                    <span className="detail-label">Permit Number:</span>
                    <span className="detail-value">{section.permitNumber || 'N/A'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Responsible Party:</span>
                    <span className="detail-value">{section.responsibleParty || 'Not assigned'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Materials Needed:</span>
                    <span className="detail-value">{section.materialsNeeded || 'None specified'}</span>
                  </div>
                </Col>
                <Col md={4}>
                  <div className="detail-item">
                    <span className="detail-label">Special Requirements:</span>
                    <span className="detail-value">{section.specialRequirements || 'None'}</span>
                  </div>
                </Col>
              </>
            )}
          </Row>
          
          {section.notes && (
            <Row className="mt-3">
              <Col md={12}>
                <div className="detail-item">
                  <span className="detail-label">Notes:</span>
                  <p className="notes-content">{section.notes}</p>
                </div>
              </Col>
            </Row>
          )}
          
          <div className="section-actions mt-3">
            <Button color="primary" tag={Link} to={`/sections/${section.sectionId}`}>
              View Details
            </Button>
          </div>
        </CardBody>
      </Collapse>
    </Card>
  );
};

export default JobSectionList;
