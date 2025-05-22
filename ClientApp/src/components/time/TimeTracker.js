import React, { useEffect, useState } from 'react';
import { 
  Container, 
  Row, 
  Col, 
  Card, 
  CardHeader, 
  CardBody, 
  Button, 
  Form, 
  FormGroup, 
  Label, 
  Input,
  Alert,
  Badge,
  ListGroup,
  ListGroupItem
} from 'reactstrap';
import { useDispatch, useSelector } from 'react-redux';
import { 
  fetchActiveTimeEntry, 
  clockIn, 
  clockOut,
  fetchJobs
} from '../../store/timeSlice';
import { formatDateTime, formatDuration } from '../../services/utils';
import { Link } from 'react-router-dom';
import './TimeTracker.css';

const TimeTracker = () => {
  const dispatch = useDispatch();
  const { 
    activeTimeEntry, 
    jobs, 
    loading, 
    clockInLoading, 
    clockOutLoading, 
    error 
  } = useSelector(state => state.time);
  const { currentUser } = useSelector(state => state.users);
  
  const [clockInData, setClockInData] = useState({
    jobId: '',
    jobSectionId: '',
    notes: ''
  });
  
  const [clockOutData, setClockOutData] = useState({
    notes: ''
  });
  
  const [timer, setTimer] = useState(null);
  const [elapsedTime, setElapsedTime] = useState(0);
  const [availableSections, setAvailableSections] = useState([]);
  
  useEffect(() => {
    // Fetch active time entry on component mount
    dispatch(fetchActiveTimeEntry());
    
    // Fetch available jobs
    dispatch(fetchJobs());
    
    // Cleanup timer on component unmount
    return () => {
      if (timer) {
        clearInterval(timer);
      }
    };
  }, [dispatch]);
  
  // Update elapsed time when active time entry changes
  useEffect(() => {
    if (activeTimeEntry) {
      const clockInTime = new Date(activeTimeEntry.clockInTime).getTime();
      
      // Calculate initial elapsed time
      const initialElapsed = Math.floor((Date.now() - clockInTime) / 1000);
      setElapsedTime(initialElapsed);
      
      // Set up interval to update elapsed time every second
      const interval = setInterval(() => {
        setElapsedTime(prev => prev + 1);
      }, 1000);
      
      setTimer(interval);
      
      return () => clearInterval(interval);
    } else {
      // Clear timer if no active time entry
      if (timer) {
        clearInterval(timer);
        setTimer(null);
      }
      setElapsedTime(0);
    }
  }, [activeTimeEntry]);
  
  // Update available sections when job selection changes
  useEffect(() => {
    if (clockInData.jobId) {
      const selectedJob = jobs.find(job => job.jobId === parseInt(clockInData.jobId));
      if (selectedJob && selectedJob.sections) {
        setAvailableSections(selectedJob.sections);
      } else {
        setAvailableSections([]);
      }
    } else {
      setAvailableSections([]);
    }
  }, [clockInData.jobId, jobs]);
  
  const handleClockInChange = (e) => {
    const { name, value } = e.target;
    setClockInData({
      ...clockInData,
      [name]: value
    });
  };
  
  const handleClockOutChange = (e) => {
    const { name, value } = e.target;
    setClockOutData({
      ...clockOutData,
      [name]: value
    });
  };
  
  const handleClockIn = (e) => {
    e.preventDefault();
    
    const data = {
      userId: currentUser.userId,
      jobId: clockInData.jobId ? parseInt(clockInData.jobId) : null,
      jobSectionId: clockInData.jobSectionId ? parseInt(clockInData.jobSectionId) : null,
      notes: clockInData.notes,
      clockInTime: new Date().toISOString()
    };
    
    dispatch(clockIn(data));
  };
  
  const handleClockOut = (e) => {
    e.preventDefault();
    
    if (!activeTimeEntry) return;
    
    const data = {
      id: activeTimeEntry.timeEntryId,
      notes: clockOutData.notes
    };
    
    dispatch(clockOut(data));
  };
  
  const formatElapsedTime = (seconds) => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };
  
  const getJobName = (jobId) => {
    if (!jobId) return 'No Job Selected';
    const job = jobs.find(j => j.jobId === jobId);
    return job ? job.jobName : 'Unknown Job';
  };
  
  const getSectionName = (sectionId) => {
    if (!sectionId) return 'No Section Selected';
    const job = jobs.find(j => j.sections && j.sections.some(s => s.sectionId === sectionId));
    if (!job) return 'Unknown Section';
    
    const section = job.sections.find(s => s.sectionId === sectionId);
    return section ? section.type : 'Unknown Section';
  };
  
  return (
    <Container className="time-tracker-container">
      <h1 className="page-title">Time Tracker</h1>
      
      {error && (
        <Alert color="danger">
          Error: {error}
        </Alert>
      )}
      
      <Row>
        <Col lg={6}>
          {activeTimeEntry ? (
            <Card className="active-time-card">
              <CardHeader>
                <h3>Currently Clocked In</h3>
              </CardHeader>
              <CardBody>
                <div className="time-display">
                  <div className="elapsed-time">{formatElapsedTime(elapsedTime)}</div>
                  <div className="clock-in-time">
                    Clocked in at: {formatDateTime(activeTimeEntry.clockInTime)}
                  </div>
                </div>
                
                <div className="active-job-info">
                  <div className="detail-item">
                    <span className="detail-label">Job:</span>
                    <span className="detail-value">
                      {activeTimeEntry.jobId ? (
                        <Link to={`/jobs/${activeTimeEntry.jobId}`}>
                          {getJobName(activeTimeEntry.jobId)}
                        </Link>
                      ) : 'Not assigned'}
                    </span>
                  </div>
                  
                  <div className="detail-item">
                    <span className="detail-label">Section:</span>
                    <span className="detail-value">
                      {activeTimeEntry.jobSectionId ? (
                        <Link to={`/sections/${activeTimeEntry.jobSectionId}`}>
                          {getSectionName(activeTimeEntry.jobSectionId)}
                        </Link>
                      ) : 'Not assigned'}
                    </span>
                  </div>
                  
                  {activeTimeEntry.notes && (
                    <div className="detail-item">
                      <span className="detail-label">Notes:</span>
                      <span className="detail-value">{activeTimeEntry.notes}</span>
                    </div>
                  )}
                </div>
                
                <Form onSubmit={handleClockOut} className="clock-out-form">
                  <FormGroup>
                    <Label for="notes">Additional Notes</Label>
                    <Input
                      type="textarea"
                      name="notes"
                      id="notes"
                      value={clockOutData.notes}
                      onChange={handleClockOutChange}
                      placeholder="Enter any notes about your work"
                      rows={3}
                    />
                  </FormGroup>
                  
                  <Button 
                    color="danger" 
                    type="submit" 
                    size="lg" 
                    block 
                    disabled={clockOutLoading}
                    className="clock-out-btn"
                  >
                    {clockOutLoading ? 'Processing...' : 'Clock Out'}
                  </Button>
                </Form>
              </CardBody>
            </Card>
          ) : (
            <Card className="clock-in-card">
              <CardHeader>
                <h3>Clock In</h3>
              </CardHeader>
              <CardBody>
                <Form onSubmit={handleClockIn}>
                  <FormGroup>
                    <Label for="jobId">Select Job</Label>
                    <Input
                      type="select"
                      name="jobId"
                      id="jobId"
                      value={clockInData.jobId}
                      onChange={handleClockInChange}
                    >
                      <option value="">-- Select a Job --</option>
                      {jobs.filter(j => j.status !== 'Completed' && j.status !== 'Cancelled').map(job => (
                        <option key={job.jobId} value={job.jobId}>
                          {job.jobName} - {job.location}
                        </option>
                      ))}
                    </Input>
                  </FormGroup>
                  
                  <FormGroup>
                    <Label for="jobSectionId">Select Section (Optional)</Label>
                    <Input
                      type="select"
                      name="jobSectionId"
                      id="jobSectionId"
                      value={clockInData.jobSectionId}
                      onChange={handleClockInChange}
                      disabled={!clockInData.jobId}
                    >
                      <option value="">-- Select a Section --</option>
                      {availableSections.map(section => (
                        <option key={section.sectionId} value={section.sectionId}>
                          {section.type} - {section.status}
                        </option>
                      ))}
                    </Input>
                  </FormGroup>
                  
                  <FormGroup>
                    <Label for="notes">Notes</Label>
                    <Input
                      type="textarea"
                      name="notes"
                      id="notes"
                      value={clockInData.notes}
                      onChange={handleClockInChange}
                      placeholder="Enter any notes about your work"
                      rows={3}
                    />
                  </FormGroup>
                  
                  <Button 
                    color="primary" 
                    type="submit" 
                    size="lg" 
                    block 
                    disabled={clockInLoading}
                    className="clock-in-btn"
                  >
                    {clockInLoading ? 'Processing...' : 'Clock In'}
                  </Button>
                </Form>
              </CardBody>
            </Card>
          )}
        </Col>
        
        <Col lg={6}>
          <Card className="recent-time-entries-card">
            <CardHeader>
              <h3>Recent Time Entries</h3>
            </CardHeader>
            <CardBody>
              <Link to="/timesheet" className="view-all-link">View Complete Timesheet</Link>
              
              {loading ? (
                <div className="loading-spinner">Loading recent time entries...</div>
              ) : (
                <ListGroup className="recent-entries-list">
                  {currentUser?.timeEntries && currentUser.timeEntries.length > 0 ? (
                    currentUser.timeEntries
                      .filter(entry => entry.clockOutTime) // Only show completed entries
                      .slice(0, 5) // Take only the 5 most recent
                      .map(entry => (
                        <ListGroupItem key={entry.timeEntryId} className="time-entry-item">
                          <div className="entry-header">
                            <span className="entry-date">{formatDateTime(entry.clockInTime)}</span>
                            <Badge color="primary" className="duration-badge">
                              {formatDuration(entry.totalHours)}
                            </Badge>
                          </div>
                          <div className="entry-job">
                            {entry.job ? entry.job.jobName : 'No Job'} 
                            {entry.jobSection && ` - ${entry.jobSection.type}`}
                          </div>
                          {entry.notes && <div className="entry-notes">{entry.notes}</div>}
                        </ListGroupItem>
                      ))
                  ) : (
                    <div className="no-entries-message">No recent time entries found</div>
                  )}
                </ListGroup>
              )}
            </CardBody>
          </Card>
          
          <Card className="time-info-card mt-4">
            <CardHeader>
              <h3>Today's Summary</h3>
            </CardHeader>
            <CardBody>
              <div className="summary-item">
                <i className="fas fa-clock summary-icon"></i>
                <div className="summary-content">
                  <div className="summary-label">Total Hours Today</div>
                  <div className="summary-value">
                    {loading ? '...' : formatDuration(
                      currentUser?.timeEntries
                        ?.filter(entry => {
                          const entryDate = new Date(entry.clockInTime).toDateString();
                          const today = new Date().toDateString();
                          return entryDate === today && entry.totalHours;
                        })
                        ?.reduce((total, entry) => total + (entry.totalHours || 0), 0)
                    )}
                  </div>
                </div>
              </div>
              
              <div className="summary-item">
                <i className="fas fa-calendar-alt summary-icon"></i>
                <div className="summary-content">
                  <div className="summary-label">This Week</div>
                  <div className="summary-value">
                    {loading ? '...' : formatDuration(
                      currentUser?.timeEntries
                        ?.filter(entry => {
                          const entryDate = new Date(entry.clockInTime);
                          const now = new Date();
                          const firstDay = new Date(now.setDate(now.getDate() - now.getDay()));
                          firstDay.setHours(0, 0, 0, 0);
                          return entryDate >= firstDay && entry.totalHours;
                        })
                        ?.reduce((total, entry) => total + (entry.totalHours || 0), 0)
                    )}
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default TimeTracker;
