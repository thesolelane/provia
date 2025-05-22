import React, { useEffect, useState } from 'react';
import { 
  Container, 
  Form, 
  FormGroup, 
  Label, 
  Input, 
  Button, 
  Row, 
  Col, 
  Card, 
  CardHeader, 
  CardBody,
  Alert
} from 'reactstrap';
import { useParams, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchJobDetail, createJob, updateJob } from '../../store/jobSlice';
import './JobForm.css';

const JobForm = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { selectedJob, loading, error } = useSelector(state => state.jobs);
  const isEditing = !!id;
  
  const [formData, setFormData] = useState({
    jobName: '',
    description: '',
    location: '',
    clientName: '',
    clientPhone: '',
    clientEmail: '',
    startDate: '',
    expectedCompletionDate: '',
    budget: '',
    status: 'Planned',
    projectManager: '',
    notes: ''
  });
  
  const [formErrors, setFormErrors] = useState({});
  
  useEffect(() => {
    if (isEditing && id) {
      dispatch(fetchJobDetail(id));
    }
  }, [dispatch, id, isEditing]);
  
  useEffect(() => {
    if (isEditing && selectedJob) {
      // Format dates for input fields
      const formatDateForInput = (dateString) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toISOString().split('T')[0];
      };
      
      setFormData({
        jobName: selectedJob.jobName || '',
        description: selectedJob.description || '',
        location: selectedJob.location || '',
        clientName: selectedJob.clientName || '',
        clientPhone: selectedJob.clientPhone || '',
        clientEmail: selectedJob.clientEmail || '',
        startDate: formatDateForInput(selectedJob.startDate),
        expectedCompletionDate: formatDateForInput(selectedJob.expectedCompletionDate),
        budget: selectedJob.budget || '',
        status: selectedJob.status || 'Planned',
        projectManager: selectedJob.projectManager || '',
        notes: selectedJob.notes || ''
      });
    }
  }, [isEditing, selectedJob]);
  
  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value
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
    
    if (!formData.jobName.trim()) {
      errors.jobName = 'Job name is required';
    }
    
    if (!formData.location.trim()) {
      errors.location = 'Location is required';
    }
    
    if (!formData.startDate) {
      errors.startDate = 'Start date is required';
    }
    
    if (formData.budget && isNaN(parseFloat(formData.budget))) {
      errors.budget = 'Budget must be a valid number';
    }
    
    if (formData.expectedCompletionDate && new Date(formData.expectedCompletionDate) < new Date(formData.startDate)) {
      errors.expectedCompletionDate = 'Expected completion date cannot be before start date';
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
    
    // Convert budget to number
    const processedData = {
      ...formData,
      budget: formData.budget ? parseFloat(formData.budget) : 0
    };
    
    try {
      if (isEditing) {
        await dispatch(updateJob({ id, job: { ...processedData, jobId: id } })).unwrap();
      } else {
        const result = await dispatch(createJob(processedData)).unwrap();
        // Navigate to the newly created job
        navigate(`/jobs/${result.jobId}`);
        return;
      }
      
      // Navigate back to job detail page
      navigate(`/jobs/${id}`);
    } catch (err) {
      console.error('Failed to save job:', err);
    }
  };
  
  if (isEditing && loading && !selectedJob) {
    return <div className="loading-spinner">Loading job details...</div>;
  }
  
  return (
    <Container className="job-form-container">
      <div className="page-header">
        <h1>{isEditing ? 'Edit Job' : 'Create New Job'}</h1>
      </div>
      
      {error && (
        <Alert color="danger">
          Error: {error}
        </Alert>
      )}
      
      <Form onSubmit={handleSubmit}>
        <Card className="mb-4">
          <CardHeader>
            <h3>Basic Information</h3>
          </CardHeader>
          <CardBody>
            <Row>
              <Col md={6}>
                <FormGroup>
                  <Label for="jobName">Job Name *</Label>
                  <Input
                    type="text"
                    name="jobName"
                    id="jobName"
                    value={formData.jobName}
                    onChange={handleChange}
                    invalid={!!formErrors.jobName}
                  />
                  {formErrors.jobName && (
                    <div className="text-danger">{formErrors.jobName}</div>
                  )}
                </FormGroup>
              </Col>
              <Col md={6}>
                <FormGroup>
                  <Label for="location">Location *</Label>
                  <Input
                    type="text"
                    name="location"
                    id="location"
                    value={formData.location}
                    onChange={handleChange}
                    invalid={!!formErrors.location}
                  />
                  {formErrors.location && (
                    <div className="text-danger">{formErrors.location}</div>
                  )}
                </FormGroup>
              </Col>
            </Row>
            
            <Row>
              <Col md={12}>
                <FormGroup>
                  <Label for="description">Description</Label>
                  <Input
                    type="textarea"
                    name="description"
                    id="description"
                    value={formData.description}
                    onChange={handleChange}
                    rows={3}
                  />
                </FormGroup>
              </Col>
            </Row>
            
            <Row>
              <Col md={6}>
                <FormGroup>
                  <Label for="startDate">Start Date *</Label>
                  <Input
                    type="date"
                    name="startDate"
                    id="startDate"
                    value={formData.startDate}
                    onChange={handleChange}
                    invalid={!!formErrors.startDate}
                  />
                  {formErrors.startDate && (
                    <div className="text-danger">{formErrors.startDate}</div>
                  )}
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
                  <Label for="budget">Budget ($)</Label>
                  <Input
                    type="number"
                    name="budget"
                    id="budget"
                    value={formData.budget}
                    onChange={handleChange}
                    step="0.01"
                    min="0"
                    invalid={!!formErrors.budget}
                  />
                  {formErrors.budget && (
                    <div className="text-danger">{formErrors.budget}</div>
                  )}
                </FormGroup>
              </Col>
              <Col md={6}>
                <FormGroup>
                  <Label for="status">Status</Label>
                  <Input
                    type="select"
                    name="status"
                    id="status"
                    value={formData.status}
                    onChange={handleChange}
                  >
                    <option value="Planned">Planned</option>
                    <option value="InProgress">In Progress</option>
                    <option value="OnHold">On Hold</option>
                    <option value="Completed">Completed</option>
                    <option value="Cancelled">Cancelled</option>
                  </Input>
                </FormGroup>
              </Col>
            </Row>
            
            <Row>
              <Col md={12}>
                <FormGroup>
                  <Label for="projectManager">Project Manager</Label>
                  <Input
                    type="text"
                    name="projectManager"
                    id="projectManager"
                    value={formData.projectManager}
                    onChange={handleChange}
                  />
                </FormGroup>
              </Col>
            </Row>
          </CardBody>
        </Card>
        
        <Card className="mb-4">
          <CardHeader>
            <h3>Client Information</h3>
          </CardHeader>
          <CardBody>
            <Row>
              <Col md={12}>
                <FormGroup>
                  <Label for="clientName">Client Name</Label>
                  <Input
                    type="text"
                    name="clientName"
                    id="clientName"
                    value={formData.clientName}
                    onChange={handleChange}
                  />
                </FormGroup>
              </Col>
            </Row>
            
            <Row>
              <Col md={6}>
                <FormGroup>
                  <Label for="clientPhone">Client Phone</Label>
                  <Input
                    type="text"
                    name="clientPhone"
                    id="clientPhone"
                    value={formData.clientPhone}
                    onChange={handleChange}
                  />
                </FormGroup>
              </Col>
              <Col md={6}>
                <FormGroup>
                  <Label for="clientEmail">Client Email</Label>
                  <Input
                    type="email"
                    name="clientEmail"
                    id="clientEmail"
                    value={formData.clientEmail}
                    onChange={handleChange}
                  />
                </FormGroup>
              </Col>
            </Row>
          </CardBody>
        </Card>
        
        <Card className="mb-4">
          <CardHeader>
            <h3>Additional Information</h3>
          </CardHeader>
          <CardBody>
            <Row>
              <Col md={12}>
                <FormGroup>
                  <Label for="notes">Notes</Label>
                  <Input
                    type="textarea"
                    name="notes"
                    id="notes"
                    value={formData.notes}
                    onChange={handleChange}
                    rows={5}
                  />
                </FormGroup>
              </Col>
            </Row>
          </CardBody>
        </Card>
        
        <div className="form-actions">
          <Button color="secondary" onClick={() => navigate(isEditing ? `/jobs/${id}` : '/jobs')}>
            Cancel
          </Button>
          <Button color="primary" type="submit" disabled={loading}>
            {loading ? 'Saving...' : (isEditing ? 'Update Job' : 'Create Job')}
          </Button>
        </div>
      </Form>
    </Container>
  );
};

export default JobForm;
