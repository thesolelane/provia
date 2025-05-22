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
  Form,
  FormGroup,
  Label,
  Input,
  Alert,
  Modal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Nav,
  NavItem,
  NavLink,
  TabContent,
  TabPane
} from 'reactstrap';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { 
  fetchJobSectionDetail, 
  updateJobSection, 
  updateSectionStatus,
  assignSubcontractor
} from '../../store/jobSlice';
import { fetchSubcontractors } from '../../store/subcontractorSlice';
import { formatDate } from '../../services/utils';
import BuildingCodeHelper from '../codeHelper/BuildingCodeHelper';
import AIAssistant from '../codeHelper/AIAssistant';
import TimeSheet from '../time/TimeSheet';
import classnames from 'classnames';
import './JobSectionDetail.css';

const sectionStatusColors = {
  NotStarted: 'secondary',
  InProgress: 'primary',
  Completed: 'success',
  Failed: 'danger',
  OnHold: 'warning',
  WaitingForInspection: 'info',
  WaitingForMaterials: 'dark'
};

const JobSectionDetail = () => {
  const { id } = useParams();
  const dispatch = useDispatch();
  const navigate = useNavigate();
  
  const { selectedSection, loading, error } = useSelector(state => state.jobs);
  const { subcontractors } = useSelector(state => state.subcontractors);
  const { currentUser } = useSelector(state => state.users);
  
  const [activeTab, setActiveTab] = useState('details');
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({});
  const [statusModal, setStatusModal] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState('');
  const [subcontractorModal, setSubcontractorModal] = useState(false);
  const [selectedSubcontractor, setSelectedSubcontractor] = useState('');
  const [formErrors, setFormErrors] = useState({});
  
  useEffect(() => {
    if (id) {
      dispatch(fetchJobSectionDetail(id));
      dispatch(fetchSubcontractors());
    }
  }, [dispatch, id]);
  
  useEffect(() => {
    if (selectedSection) {
      // Format dates for input fields
      const formatDateForInput = (dateString) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toISOString().split('T')[0];
      };
      
      setFormData({
        sectionId: selectedSection.sectionId,
        jobId: selectedSection.jobId,
        type: selectedSection.type,
        status: selectedSection.status,
        isSubcontracted: selectedSection.isSubcontracted,
        subcontractorId: selectedSection.subcontractorId || '',
        contractReference: selectedSection.contractReference || '',
        startDate: formatDateForInput(selectedSection.startDate),
        expectedCompletionDate: formatDateForInput(selectedSection.expectedCompletionDate),
        actualCompletionDate: formatDateForInput(selectedSection.actualCompletionDate),
        inspectionDate: formatDateForInput(selectedSection.inspectionDate),
        reinspectionDate: formatDateForInput(selectedSection.reinspectionDate),
        inspectionResult: selectedSection.inspectionResult || '',
        permitNumber: selectedSection.permitNumber || '',
        responsibleParty: selectedSection.responsibleParty || '',
        materialsNeeded: selectedSection.materialsNeeded || '',
        specialRequirements: selectedSection.specialRequirements || '',
        notes: selectedSection.notes || ''
      });
      
      setSelectedSubcontractor(selectedSection.subcontractorId || '');
    }
  }, [selectedSection]);
  
  const toggleTab = (tab) => {
    if (activeTab !== tab) {
      setActiveTab(tab);
    }
  };
  
  const toggleStatusModal = () => {
    setStatusModal(!statusModal);
    if (!statusModal) {
      setSelectedStatus(selectedSection.status);
    }
  };
  
  const toggleSubcontractorModal = () => {
    setSubcontractorModal(!subcontractorModal);
  };
  
  const toggleEditMode = () => {
    setIsEditing(!isEditing);
    // Reset form data when canceling edit
    if (isEditing) {
      setFormData({
        ...formData,
        // Reset to original values
      });
    }
  };
  
  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : value
    });
    
    // Clear validation errors when field is changed
    if (formErrors[name]) {
      setFormErrors({
        ...formErrors,
        [name]: undefined
      });
    }
  };
  
  const validateForm = () => {
    const errors = {};
    
    if (formData.expectedCompletionDate && formData.startDate && 
        new Date(formData.expectedCompletionDate) < new Date(formData.startDate)) {
      errors.expectedCompletionDate = 'Expected completion date cannot be before start date';
    }
    
    if (formData.isSubcontracted && !formData.subcontractorId) {
      errors.subcontractorId = 'Please select a subcontractor';
    }
    
    return errors;
  };
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    
    const errors = validateForm();
    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      return;
    }
    
    try {
      await dispatch(updateJobSection(formData)).unwrap();
      setIsEditing(false);
    } catch (err) {
      console.error('Failed to update section:', err);
    }
  };
  
  const handleStatusUpdate = async () => {
    if (selectedStatus !== selectedSection.status) {
      try {
        await dispatch(updateSectionStatus({ id, status: selectedStatus })).unwrap();
        setStatusModal(false);
      } catch (err) {
        console.error('Failed to update status:', err);
      }
    } else {
      setStatusModal(false);
    }
  };
  
  const handleSubcontractorAssign = async () => {
    try {
      await dispatch(assignSubcontractor({ 
        id, 
        subcontractorId: selectedSubcontractor ? parseInt(selectedSubcontractor) : null 
      })).unwrap();
      setSubcontractorModal(false);
    } catch (err) {
      console.error('Failed to assign subcontractor:', err);
    }
  };
  
  if (loading && !selectedSection) {
    return <div className="loading-spinner">Loading section details...</div>;
  }
  
  if (error) {
    return (
      <Alert color="danger">
        Error loading section details: {error}
      </Alert>
    );
  }
  
  if (!selectedSection) {
    return <div>No section found with ID {id}</div>;
  }
  
  const isManager = currentUser && ['Administrator', 'ProjectManager', 'Supervisor'].includes(currentUser.role);
  
  return (
    <Container className="section-detail-container">
      <div className="page-header">
        <div>
          <h1>{selectedSection.type} Section</h1>
          <div className="section-badges">
            <Badge color={sectionStatusColors[selectedSection.status] || 'secondary'} className="status-badge">
              {selectedSection.status}
            </Badge>
            {selectedSection.isSubcontracted && (
              <Badge color="dark" className="ml-2">Subcontracted</Badge>
            )}
          </div>
        </div>
        <div className="actions">
          <Button color="info" onClick={toggleStatusModal} className="mr-2">
            Update Status
          </Button>
          {isManager && !isEditing && (
            <Button color="primary" onClick={toggleEditMode}>
              <i className="fas fa-edit"></i> Edit
            </Button>
          )}
        </div>
      </div>
      
      <div className="job-info-banner">
        <Link to={`/jobs/${selectedSection.jobId}`}>
          <i className="fas fa-arrow-left"></i> Back to {selectedSection.job?.jobName || 'Job'}
        </Link>
      </div>
      
      <Nav tabs className="section-detail-tabs">
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'details' })}
            onClick={() => toggleTab('details')}
          >
            Details
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
            className={classnames({ active: activeTab === 'codes' })}
            onClick={() => toggleTab('codes')}
          >
            Building Codes
          </NavLink>
        </NavItem>
        <NavItem>
          <NavLink
            className={classnames({ active: activeTab === 'assistant' })}
            onClick={() => toggleTab('assistant')}
          >
            AI Assistant
          </NavLink>
        </NavItem>
      </Nav>
      
      <TabContent activeTab={activeTab}>
        <TabPane tabId="details">
          {isEditing ? (
            <Form onSubmit={handleSubmit}>
              <Card className="mb-4">
                <CardHeader>
                  <h3>Basic Information</h3>
                </CardHeader>
                <CardBody>
                  <Row>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="startDate">Start Date</Label>
                        <Input
                          type="date"
                          name="startDate"
                          id="startDate"
                          value={formData.startDate}
                          onChange={handleChange}
                        />
                      </FormGroup>
                    </Col>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="expectedCompletionDate">Expected Completion Date</Label>
                        <Input
                          type="date"
                          name="expectedCompletionDate"
                          id="expectedCompletionDate"
                          value={formData.expectedCompletionDate}
                          onChange={handleChange}
                          invalid={!!formErrors.expectedCompletionDate}
                        />
                        {formErrors.expectedCompletionDate && (
                          <div className="text-danger">{formErrors.expectedCompletionDate}</div>
                        )}
                      </FormGroup>
                    </Col>
                  </Row>
                  <Row>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="actualCompletionDate">Actual Completion Date</Label>
                        <Input
                          type="date"
                          name="actualCompletionDate"
                          id="actualCompletionDate"
                          value={formData.actualCompletionDate}
                          onChange={handleChange}
                        />
                      </FormGroup>
                    </Col>
                    <Col md={6}>
                      <FormGroup check className="mb-3">
                        <Label check>
                          <Input
                            type="checkbox"
                            name="isSubcontracted"
                            checked={formData.isSubcontracted}
                            onChange={handleChange}
                          />{' '}
                          This work is subcontracted
                        </Label>
                      </FormGroup>
                      
                      {formData.isSubcontracted && (
                        <FormGroup>
                          <Label for="subcontractorId">Subcontractor</Label>
                          <Input
                            type="select"
                            name="subcontractorId"
                            id="subcontractorId"
                            value={formData.subcontractorId}
                            onChange={handleChange}
                            invalid={!!formErrors.subcontractorId}
                          >
                            <option value="">Select a subcontractor</option>
                            {subcontractors.map(sub => (
                              <option key={sub.subcontractorId} value={sub.subcontractorId}>
                                {sub.companyName}
                              </option>
                            ))}
                          </Input>
                          {formErrors.subcontractorId && (
                            <div className="text-danger">{formErrors.subcontractorId}</div>
                          )}
                        </FormGroup>
                      )}
                    </Col>
                  </Row>
                </CardBody>
              </Card>
              
              <Card className="mb-4">
                <CardHeader>
                  <h3>Inspection Details</h3>
                </CardHeader>
                <CardBody>
                  <Row>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="inspectionDate">Inspection Date</Label>
                        <Input
                          type="date"
                          name="inspectionDate"
                          id="inspectionDate"
                          value={formData.inspectionDate}
                          onChange={handleChange}
                        />
                      </FormGroup>
                    </Col>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="reinspectionDate">Re-inspection Date</Label>
                        <Input
                          type="date"
                          name="reinspectionDate"
                          id="reinspectionDate"
                          value={formData.reinspectionDate}
                          onChange={handleChange}
                        />
                      </FormGroup>
                    </Col>
                  </Row>
                  <Row>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="inspectionResult">Inspection Result</Label>
                        <Input
                          type="select"
                          name="inspectionResult"
                          id="inspectionResult"
                          value={formData.inspectionResult}
                          onChange={handleChange}
                        >
                          <option value="">Select result</option>
                          <option value="Pass">Pass</option>
                          <option value="Fail">Fail</option>
                          <option value="Conditional Pass">Conditional Pass</option>
                          <option value="Not Performed">Not Performed</option>
                        </Input>
                      </FormGroup>
                    </Col>
                    <Col md={6}>
                      <FormGroup>
                        <Label for="permitNumber">Permit Number</Label>
                        <Input
                          type="text"
                          name="permitNumber"
                          id="permitNumber"
                          value={formData.permitNumber}
                          onChange={handleChange}
                        />
                      </FormGroup>
                    </Col>
                  </Row>
                </CardBody>
              </Card>
              
              {!formData.isSubcontracted && (
                <Card className="mb-4">
                  <CardHeader>
                    <h3>Work Details</h3>
                  </CardHeader>
                  <CardBody>
                    <Row>
                      <Col md={12}>
                        <FormGroup>
                          <Label for="responsibleParty">Responsible Party</Label>
                          <Input
                            type="text"
                            name="responsibleParty"
                            id="responsibleParty"
                            value={formData.responsibleParty}
                            onChange={handleChange}
                          />
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md={6}>
                        <FormGroup>
                          <Label for="materialsNeeded">Materials Needed</Label>
                          <Input
                            type="textarea"
                            name="materialsNeeded"
                            id="materialsNeeded"
                            value={formData.materialsNeeded}
                            onChange={handleChange}
                            rows={3}
                          />
                        </FormGroup>
                      </Col>
                      <Col md={6}>
                        <FormGroup>
                          <Label for="specialRequirements">Special Requirements</Label>
                          <Input
                            type="textarea"
                            name="specialRequirements"
                            id="specialRequirements"
                            value={formData.specialRequirements}
                            onChange={handleChange}
                            rows={3}
                          />
                        </FormGroup>
                      </Col>
                    </Row>
                  </CardBody>
                </Card>
              )}
              
              {formData.isSubcontracted && (
                <Card className="mb-4">
                  <CardHeader>
                    <h3>Subcontractor Details</h3>
                  </CardHeader>
                  <CardBody>
                    <FormGroup>
                      <Label for="contractReference">Contract Reference</Label>
                      <Input
                        type="text"
                        name="contractReference"
                        id="contractReference"
                        value={formData.contractReference}
                        onChange={handleChange}
                      />
                    </FormGroup>
                  </CardBody>
                </Card>
              )}
              
              <Card className="mb-4">
                <CardHeader>
                  <h3>Notes</h3>
                </CardHeader>
                <CardBody>
                  <FormGroup>
                    <Input
                      type="textarea"
                      name="notes"
                      id="notes"
                      value={formData.notes}
                      onChange={handleChange}
                      rows={5}
                    />
                  </FormGroup>
                </CardBody>
              </Card>
              
              <div className="form-actions">
                <Button color="secondary" onClick={toggleEditMode} className="mr-2">
                  Cancel
                </Button>
                <Button color="primary" type="submit">
                  Save Changes
                </Button>
              </div>
            </Form>
          ) : (
            <>
              <Row>
                <Col md={6}>
                  <Card className="mb-4">
                    <CardHeader>
                      <h3>Basic Information</h3>
                    </CardHeader>
                    <CardBody>
                      <div className="detail-item">
                        <span className="detail-label">Status:</span>
                        <span className="detail-value">
                          <Badge color={sectionStatusColors[selectedSection.status] || 'secondary'}>
                            {selectedSection.status}
                          </Badge>
                        </span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Start Date:</span>
                        <span className="detail-value">{formatDate(selectedSection.startDate) || 'Not set'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Expected Completion:</span>
                        <span className="detail-value">{formatDate(selectedSection.expectedCompletionDate) || 'Not set'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Actual Completion:</span>
                        <span className="detail-value">{formatDate(selectedSection.actualCompletionDate) || 'Not completed'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Work Type:</span>
                        <span className="detail-value">
                          {selectedSection.isSubcontracted ? 'Subcontracted' : 'In-house'}
                          {isManager && (
                            <Button color="link" className="p-0 ml-2" onClick={toggleSubcontractorModal}>
                              <i className="fas fa-exchange-alt"></i> Change
                            </Button>
                          )}
                        </span>
                      </div>
                    </CardBody>
                  </Card>
                  
                  <Card className="mb-4">
                    <CardHeader>
                      <h3>Inspection Details</h3>
                    </CardHeader>
                    <CardBody>
                      <div className="detail-item">
                        <span className="detail-label">Permit Number:</span>
                        <span className="detail-value">{selectedSection.permitNumber || 'N/A'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Inspection Date:</span>
                        <span className="detail-value">{formatDate(selectedSection.inspectionDate) || 'Not scheduled'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Re-inspection Date:</span>
                        <span className="detail-value">{formatDate(selectedSection.reinspectionDate) || 'N/A'}</span>
                      </div>
                      <div className="detail-item">
                        <span className="detail-label">Inspection Result:</span>
                        <span className="detail-value">
                          {selectedSection.inspectionResult ? (
                            <Badge color={selectedSection.inspectionResult === 'Pass' ? 'success' : 'danger'}>
                              {selectedSection.inspectionResult}
                            </Badge>
                          ) : 'Not inspected'}
                        </span>
                      </div>
                    </CardBody>
                  </Card>
                </Col>
                
                <Col md={6}>
                  {selectedSection.isSubcontracted ? (
                    <Card className="mb-4">
                      <CardHeader>
                        <h3>Subcontractor Information</h3>
                      </CardHeader>
                      <CardBody>
                        <div className="detail-item">
                          <span className="detail-label">Company:</span>
                          <span className="detail-value">{selectedSection.subcontractor?.companyName || 'Not assigned'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Contact Name:</span>
                          <span className="detail-value">{selectedSection.subcontractor?.contactName || 'Not specified'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Phone:</span>
                          <span className="detail-value">{selectedSection.subcontractor?.phone || 'Not provided'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Email:</span>
                          <span className="detail-value">{selectedSection.subcontractor?.email || 'Not provided'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Contract Reference:</span>
                          <span className="detail-value">{selectedSection.contractReference || 'Not specified'}</span>
                        </div>
                      </CardBody>
                    </Card>
                  ) : (
                    <Card className="mb-4">
                      <CardHeader>
                        <h3>Work Details</h3>
                      </CardHeader>
                      <CardBody>
                        <div className="detail-item">
                          <span className="detail-label">Responsible Party:</span>
                          <span className="detail-value">{selectedSection.responsibleParty || 'Not assigned'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Materials Needed:</span>
                          <span className="detail-value">{selectedSection.materialsNeeded || 'None specified'}</span>
                        </div>
                        <div className="detail-item">
                          <span className="detail-label">Special Requirements:</span>
                          <span className="detail-value">{selectedSection.specialRequirements || 'None'}</span>
                        </div>
                      </CardBody>
                    </Card>
                  )}
                  
                  <Card>
                    <CardHeader>
                      <h3>Notes</h3>
                    </CardHeader>
                    <CardBody>
                      <p className="notes-content">{selectedSection.notes || 'No notes available for this section.'}</p>
                    </CardBody>
                  </Card>
                </Col>
              </Row>
            </>
          )}
        </TabPane>
        
        <TabPane tabId="time">
          <TimeSheet sectionId={id} />
        </TabPane>
        
        <TabPane tabId="codes">
          <BuildingCodeHelper sectionType={selectedSection.type} />
        </TabPane>
        
        <TabPane tabId="assistant">
          <AIAssistant sectionId={id} />
        </TabPane>
      </TabContent>
      
      {/* Status Update Modal */}
      <Modal isOpen={statusModal} toggle={toggleStatusModal}>
        <ModalHeader toggle={toggleStatusModal}>Update Section Status</ModalHeader>
        <ModalBody>
          <FormGroup>
            <Label for="statusSelect">Select New Status</Label>
            <Input
              type="select"
              name="statusSelect"
              id="statusSelect"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
            >
              <option value="NotStarted">Not Started</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
              <option value="Failed">Failed</option>
              <option value="OnHold">On Hold</option>
              <option value="WaitingForInspection">Waiting For Inspection</option>
              <option value="WaitingForMaterials">Waiting For Materials</option>
            </Input>
          </FormGroup>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={toggleStatusModal}>Cancel</Button>
          <Button color="primary" onClick={handleStatusUpdate}>Update Status</Button>
        </ModalFooter>
      </Modal>
      
      {/* Subcontractor Assignment Modal */}
      <Modal isOpen={subcontractorModal} toggle={toggleSubcontractorModal}>
        <ModalHeader toggle={toggleSubcontractorModal}>
          {selectedSection.isSubcontracted ? 'Change Subcontractor' : 'Assign to Subcontractor'}
        </ModalHeader>
        <ModalBody>
          <FormGroup>
            <Label for="subcontractorSelect">
              {selectedSection.isSubcontracted ? 'Select New Subcontractor' : 'Select Subcontractor'}
            </Label>
            <Input
              type="select"
              name="subcontractorSelect"
              id="subcontractorSelect"
              value={selectedSubcontractor}
              onChange={(e) => setSelectedSubcontractor(e.target.value)}
            >
              <option value="">None (In-house Work)</option>
              {subcontractors.map(sub => (
                <option key={sub.subcontractorId} value={sub.subcontractorId}>
                  {sub.companyName}
                </option>
              ))}
            </Input>
          </FormGroup>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={toggleSubcontractorModal}>Cancel</Button>
          <Button color="primary" onClick={handleSubcontractorAssign}>
            {selectedSection.isSubcontracted ? 'Update Assignment' : 'Assign'}
          </Button>
        </ModalFooter>
      </Modal>
    </Container>
  );
};

export default JobSectionDetail;
