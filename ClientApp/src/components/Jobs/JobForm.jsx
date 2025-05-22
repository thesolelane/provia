import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Grid,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  CircularProgress,
  Alert,
  FormHelperText
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { jobService } from '../../services/jobService';
import { employeeService } from '../../services/employeeService';

function JobForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEditMode = !!id;
  
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    location: '',
    jobNumber: '',
    startDate: new Date(),
    targetCompletionDate: null,
    actualCompletionDate: null,
    status: 'Pending',
    clientName: '',
    clientEmail: '',
    clientPhone: '',
    budget: 0,
    actualCost: 0,
    projectManagerId: null,
    notes: ''
  });
  
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingForm, setLoadingForm] = useState(isEditMode);
  const [error, setError] = useState('');
  const [validationErrors, setValidationErrors] = useState({});

  useEffect(() => {
    const fetchEmployees = async () => {
      try {
        const data = await employeeService.getEmployees(true);
        setEmployees(data.filter(emp => emp.role === 'ProjectManager' || emp.role === 'Admin'));
      } catch (error) {
        console.error('Error fetching employees:', error);
        setError('Failed to load project managers. Please try again later.');
      }
    };

    const fetchJobDetails = async () => {
      if (isEditMode) {
        try {
          setLoadingForm(true);
          const job = await jobService.getJobById(id);
          
          // Convert string dates to Date objects for date pickers
          const formattedJob = {
            ...job,
            startDate: job.startDate ? new Date(job.startDate) : null,
            targetCompletionDate: job.targetCompletionDate ? new Date(job.targetCompletionDate) : null,
            actualCompletionDate: job.actualCompletionDate ? new Date(job.actualCompletionDate) : null
          };
          
          setFormData(formattedJob);
        } catch (error) {
          console.error(`Error fetching job with ID ${id}:`, error);
          setError('Failed to load job details. Please try again later.');
        } finally {
          setLoadingForm(false);
        }
      }
    };

    fetchEmployees();
    fetchJobDetails();
  }, [id, isEditMode]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    
    // Clear validation error for this field
    if (validationErrors[name]) {
      setValidationErrors(prev => ({
        ...prev,
        [name]: null
      }));
    }
  };

  const handleDateChange = (name, date) => {
    setFormData(prev => ({
      ...prev,
      [name]: date
    }));
    
    // Clear validation error for this field
    if (validationErrors[name]) {
      setValidationErrors(prev => ({
        ...prev,
        [name]: null
      }));
    }
  };

  const validateForm = () => {
    const errors = {};
    
    if (!formData.name.trim()) {
      errors.name = 'Job name is required';
    }
    
    if (!formData.location.trim()) {
      errors.location = 'Location is required';
    }
    
    if (!formData.jobNumber.trim()) {
      errors.jobNumber = 'Job number is required';
    }
    
    if (!formData.startDate) {
      errors.startDate = 'Start date is required';
    }
    
    if (formData.clientEmail && !/\S+@\S+\.\S+/.test(formData.clientEmail)) {
      errors.clientEmail = 'Please enter a valid email address';
    }
    
    if (formData.budget < 0) {
      errors.budget = 'Budget cannot be negative';
    }
    
    if (formData.actualCost < 0) {
      errors.actualCost = 'Actual cost cannot be negative';
    }
    
    return errors;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    // Validate form
    const errors = validateForm();
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }
    
    setLoading(true);
    setError('');
    
    try {
      if (isEditMode) {
        await jobService.updateJob(id, formData);
      } else {
        await jobService.createJob(formData);
      }
      
      navigate('/jobs');
    } catch (error) {
      console.error('Error saving job:', error);
      setError('Failed to save job. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  if (loadingForm) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h4" gutterBottom>
            {isEditMode ? 'Edit Job' : 'Create New Job'}
          </Typography>
          
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          
          <Box component="form" onSubmit={handleSubmit}>
            <Grid container spacing={3}>
              <Grid item xs={12} sm={6}>
                <TextField
                  required
                  fullWidth
                  label="Job Name"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  error={!!validationErrors.name}
                  helperText={validationErrors.name}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  required
                  fullWidth
                  label="Job Number"
                  name="jobNumber"
                  value={formData.jobNumber}
                  onChange={handleInputChange}
                  error={!!validationErrors.jobNumber}
                  helperText={validationErrors.jobNumber}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  multiline
                  rows={2}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextField
                  required
                  fullWidth
                  label="Location"
                  name="location"
                  value={formData.location}
                  onChange={handleInputChange}
                  error={!!validationErrors.location}
                  helperText={validationErrors.location}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="Start Date*"
                  value={formData.startDate}
                  onChange={(date) => handleDateChange('startDate', date)}
                  slotProps={{
                    textField: {
                      fullWidth: true,
                      error: !!validationErrors.startDate,
                      helperText: validationErrors.startDate
                    }
                  }}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="Target Completion Date"
                  value={formData.targetCompletionDate}
                  onChange={(date) => handleDateChange('targetCompletionDate', date)}
                  slotProps={{
                    textField: {
                      fullWidth: true
                    }
                  }}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>Status</InputLabel>
                  <Select
                    name="status"
                    value={formData.status}
                    onChange={handleInputChange}
                    label="Status"
                  >
                    <MenuItem value="Pending">Pending</MenuItem>
                    <MenuItem value="In Progress">In Progress</MenuItem>
                    <MenuItem value="Completed">Completed</MenuItem>
                    <MenuItem value="Delayed">Delayed</MenuItem>
                    <MenuItem value="Cancelled">Cancelled</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="Actual Completion Date"
                  value={formData.actualCompletionDate}
                  onChange={(date) => handleDateChange('actualCompletionDate', date)}
                  slotProps={{
                    textField: {
                      fullWidth: true
                    }
                  }}
                />
              </Grid>
              
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom>
                  Client Information
                </Typography>
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Client Name"
                  name="clientName"
                  value={formData.clientName}
                  onChange={handleInputChange}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Client Email"
                  name="clientEmail"
                  type="email"
                  value={formData.clientEmail}
                  onChange={handleInputChange}
                  error={!!validationErrors.clientEmail}
                  helperText={validationErrors.clientEmail}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Client Phone"
                  name="clientPhone"
                  value={formData.clientPhone}
                  onChange={handleInputChange}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>Project Manager</InputLabel>
                  <Select
                    name="projectManagerId"
                    value={formData.projectManagerId || ''}
                    onChange={handleInputChange}
                    label="Project Manager"
                  >
                    <MenuItem value="">
                      <em>None</em>
                    </MenuItem>
                    {employees.map(employee => (
                      <MenuItem key={employee.id} value={employee.id}>
                        {employee.firstName} {employee.lastName}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Budget"
                  name="budget"
                  type="number"
                  value={formData.budget}
                  onChange={handleInputChange}
                  InputProps={{ startAdornment: '$' }}
                  error={!!validationErrors.budget}
                  helperText={validationErrors.budget}
                />
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Actual Cost"
                  name="actualCost"
                  type="number"
                  value={formData.actualCost}
                  onChange={handleInputChange}
                  InputProps={{ startAdornment: '$' }}
                  error={!!validationErrors.actualCost}
                  helperText={validationErrors.actualCost}
                />
              </Grid>
              
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Notes"
                  name="notes"
                  multiline
                  rows={4}
                  value={formData.notes}
                  onChange={handleInputChange}
                />
              </Grid>
            </Grid>
            
            <Box sx={{ mt: 3, display: 'flex', justifyContent: 'space-between' }}>
              <Button 
                component={RouterLink} 
                to={isEditMode ? `/jobs/${id}` : '/jobs'} 
                variant="outlined"
              >
                Cancel
              </Button>
              <Button 
                type="submit" 
                variant="contained" 
                color="primary" 
                disabled={loading}
              >
                {loading ? <CircularProgress size={24} /> : isEditMode ? 'Update Job' : 'Create Job'}
              </Button>
            </Box>
          </Box>
        </Paper>
      </Container>
    </LocalizationProvider>
  );
}

export default JobForm;
