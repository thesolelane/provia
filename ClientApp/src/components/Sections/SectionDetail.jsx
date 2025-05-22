import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Button,
  Divider,
  Chip,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Switch,
  FormControlLabel,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  CircularProgress,
  Alert,
  IconButton,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Tab,
  Tabs
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import {
  Edit as EditIcon,
  ExpandMore as ExpandMoreIcon,
  SaveAlt as SaveIcon,
  Event as EventIcon,
  Person as PersonIcon,
  Business as BusinessIcon,
  WarningAmber as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Description as DescriptionIcon,
  Psychology as AiIcon
} from '@mui/icons-material';
import { sectionService } from '../../services/sectionService';
import { jobService } from '../../services/jobService';
import { employeeService } from '../../services/employeeService';
import { subcontractorService } from '../../services/subcontractorService';
import { aiAssistantService } from '../../services/aiAssistantService';
import { authService } from '../../services/authService';

function SectionDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  
  const [section, setSection] = useState(null);
  const [job, setJob] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [employees, setEmployees] = useState([]);
  const [subcontractors, setSubcontractors] = useState([]);
  const [editMode, setEditMode] = useState(false);
  const [inspectionDialogOpen, setInspectionDialogOpen] = useState(false);
  const [inspectionDate, setInspectionDate] = useState(null);
  const [inspectionResultDialogOpen, setInspectionResultDialogOpen] = useState(false);
  const [inspectionResult, setInspectionResult] = useState({ result: 'pass', notes: '', reinspectionDate: null });
  const [activeTab, setActiveTab] = useState(0);
  const [aiGuidance, setAiGuidance] = useState(null);
  const [loadingAi, setLoadingAi] = useState(false);

  const canEdit = authService.hasRole(['Admin', 'ProjectManager', 'Employee']);
  
  // Create a copy of the section data for editing
  const [editFormData, setEditFormData] = useState(null);
  
  // For subcontractor toggle
  const [isSubcontracted, setIsSubcontracted] = useState(false);
  const [selectedSubcontractorId, setSelectedSubcontractorId] = useState(null);
  const [contractReference, setContractReference] = useState('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        // Fetch section data
        const sectionData = await sectionService.getSectionById(id);
        setSection(sectionData);
        setEditFormData({ ...sectionData });
        setIsSubcontracted(sectionData.isSubcontracted);
        setSelectedSubcontractorId(sectionData.subcontractorId);
        setContractReference(sectionData.contractReference || '');
        
        // Fetch job data
        if (sectionData.jobId) {
          const jobData = await jobService.getJobById(sectionData.jobId);
          setJob(jobData);
        }
        
        // Fetch employees for responsible person dropdown
        const employeesData = await employeeService.getEmployees(true);
        setEmployees(employeesData);
        
        // Fetch subcontractors for dropdown
        const subcontractorsData = await subcontractorService.getSubcontractors(true);
        setSubcontractors(subcontractorsData);
        
        setError('');
      } catch (error) {
        console.error(`Error fetching section data for ID ${id}:`, error);
        setError('Failed to load section details. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  const fetchAiGuidance = async () => {
    try {
      setLoadingAi(true);
      const response = await aiAssistantService.getSectionGuidance(id);
      setAiGuidance(response);
    } catch (error) {
      console.error(`Error fetching AI guidance for section ID ${id}:`, error);
      setError('Failed to load AI guidance. Please try again later.');
    } finally {
      setLoadingAi(false);
    }
  };

  const handleEditFormChange = (field, value) => {
    setEditFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      await sectionService.updateSection(id, editFormData);
      
      // Refresh section data
      const updatedSection = await sectionService.getSectionById(id);
      setSection(updatedSection);
      setEditFormData({ ...updatedSection });
      
      setEditMode(false);
      setError('');
    } catch (error) {
      console.error(`Error updating section with ID ${id}:`, error);
      setError('Failed to update section. Please try again later.');
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setEditFormData({ ...section });
    setEditMode(false);
  };

  const handleOpenInspectionDialog = () => {
    setInspectionDate(section.inspectionDate ? new Date(section.inspectionDate) : new Date());
    setInspectionDialogOpen(true);
  };

  const handleScheduleInspection = async () => {
    try {
      setSaving(true);
      await sectionService.scheduleInspection(id, inspectionDate);
      
      // Refresh section data
      const updatedSection = await sectionService.getSectionById(id);
      setSection(updatedSection);
      setEditFormData({ ...updatedSection });
      
      setInspectionDialogOpen(false);
      setError('');
    } catch (error) {
      console.error(`Error scheduling inspection for section ID ${id}:`, error);
      setError('Failed to schedule inspection. Please try again later.');
    } finally {
      setSaving(false);
    }
  };

  const handleOpenInspectionResultDialog = () => {
    setInspectionResult({
      result: 'pass',
      notes: '',
      reinspectionDate: null
    });
    setInspectionResultDialogOpen(true);
  };

  const handleRecordInspection = async () => {
    try {
      setSaving(true);
      await sectionService.recordInspection(id, inspectionResult);
      
      // Refresh section data
      const updatedSection = await sectionService.getSectionById(id);
      setSection(updatedSection);
      setEditFormData({ ...updatedSection });
      
      setInspectionResultDialogOpen(false);
      setError('');
    } catch (error) {
      console.error(`Error recording inspection result for section ID ${id}:`, error);
      setError('Failed to record inspection result. Please try again later.');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleSubcontractor = async () => {
    try {
      setSaving(true);
      
      await sectionService.toggleSubcontractor(id, {
        isSubcontracted,
        subcontractorId: selectedSubcontractorId,
        contractReference
      });
      
      // Refresh section data
      const updatedSection = await sectionService.getSectionById(id);
      setSection(updatedSection);
      setEditFormData({ ...updatedSection });
      
      setError('');
    } catch (error) {
      console.error(`Error toggling subcontractor for section ID ${id}:`, error);
      setError('Failed to update subcontractor information. Please try again later.');
    } finally {
      setSaving(false);
    }
  };

  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
    
    // Load AI guidance when switching to the AI tab
    if (newValue === 3 && !aiGuidance) {
      fetchAiGuidance();
    }
  };

  const getSectionTypeName = (type) => {
    switch (type) {
      case 1: return 'Permit';
      case 2: return 'Demolition';
      case 3: return 'Rough Plumbing';
      case 4: return 'Rough Electrical';
      case 5: return 'Framing';
      case 6: return 'Insulation';
      case 7: return 'Sheetrock';
      case 8: return 'Paint Prep';
      case 9: return 'Finish Install';
      case 10: return 'Finish Paint/Flooring';
      case 11: return 'Kitchen & Bath Fixtures';
      case 12: return 'Miscellaneous';
      default: return `Unknown (${type})`;
    }
  };

  const getStatusName = (status) => {
    switch (status) {
      case 1: return 'Not Started';
      case 2: return 'In Progress';
      case 3: return 'Completed';
      case 4: return 'Denied';
      case 5: return 'On Hold';
      case 6: return 'Needs Inspection';
      case 7: return 'Passed Inspection';
      case 8: return 'Failed Inspection';
      default: return `Unknown (${status})`;
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 3: // Completed
      case 7: // Passed Inspection
        return 'success';
      case 2: // In Progress
        return 'primary';
      case 6: // Needs Inspection
        return 'info';
      case 5: // On Hold
        return 'warning';
      case 4: // Denied
      case 8: // Failed Inspection
        return 'error';
      default:
        return 'default';
    }
  };

  const statuses = [
    { id: 1, name: 'Not Started' },
    { id: 2, name: 'In Progress' },
    { id: 3, name: 'Completed' },
    { id: 4, name: 'Denied' },
    { id: 5, name: 'On Hold' },
    { id: 6, name: 'Needs Inspection' },
    { id: 7, name: 'Passed Inspection' },
    { id: 8, name: 'Failed Inspection' }
  ];

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error">{error}</Alert>
        <Box sx={{ mt: 2 }}>
          <Button component={RouterLink} to="/sections" variant="contained">
            Back to Sections
          </Button>
        </Box>
      </Container>
    );
  }

  if (!section) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="info">Section not found</Alert>
        <Box sx={{ mt: 2 }}>
          <Button component={RouterLink} to="/sections" variant="contained">
            Back to Sections
          </Button>
        </Box>
      </Container>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Paper sx={{ p: 2, position: 'relative' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
            <Typography variant="h4" component="h1">
              {getSectionTypeName(section.sectionType)}
            </Typography>
            
            <Box>
              {canEdit && !editMode && (
                <Button
                  variant="contained"
                  color="primary"
                  startIcon={<EditIcon />}
                  onClick={() => setEditMode(true)}
                >
                  Edit
                </Button>
              )}
              
              {editMode && (
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Button
                    variant="outlined"
                    onClick={handleCancel}
                    disabled={saving}
                  >
                    Cancel
                  </Button>
                  <Button
                    variant="contained"
                    color="primary"
                    startIcon={<SaveIcon />}
                    onClick={handleSave}
                    disabled={saving}
                  >
                    {saving ? <CircularProgress size={24} /> : 'Save'}
                  </Button>
                </Box>
              )}
            </Box>
          </Box>
          
          <Box sx={{ mb: 3 }}>
            <Chip 
              label={getStatusName(section.status)} 
              color={getStatusColor(section.status)} 
              sx={{ mr: 1 }}
            />
            {job && (
              <Chip 
                label={`Job: ${job.name}`} 
                component={RouterLink}
                to={`/jobs/${job.id}`}
                clickable
                variant="outlined"
                sx={{ mr: 1 }}
              />
            )}
            {section.isSubcontracted && (
              <Chip 
                label={`Subcontracted: ${section.subcontractor?.companyName || 'Yes'}`} 
                color="secondary"
                sx={{ mr: 1 }}
              />
            )}
          </Box>
          
          <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 2 }}>
            <Tab label="Details" />
            <Tab label="Inspection" />
            <Tab label="Subcontractor" />
            <Tab label="AI Guidance" />
          </Tabs>
          
          {activeTab === 0 && (
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardHeader title="Section Information" />
                  <CardContent>
                    {!editMode ? (
                      <Grid container spacing={2}>
                        <Grid item xs={12}>
                          <Typography variant="subtitle2">Description</Typography>
                          <Typography>{section.description || 'No description provided'}</Typography>
                        </Grid>
                        
                        <Grid item xs={12}>
                          <Typography variant="subtitle2">Status</Typography>
                          <Chip 
                            label={getStatusName(section.status)} 
                            color={getStatusColor(section.status)} 
                            size="small"
                          />
                        </Grid>
                        
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2">Start Date</Typography>
                          <Typography>
                            {section.startDate 
                              ? new Date(section.startDate).toLocaleDateString() 
                              : 'Not started'}
                          </Typography>
                        </Grid>
                        
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2">Completion Date</Typography>
                          <Typography>
                            {section.completionDate 
                              ? new Date(section.completionDate).toLocaleDateString() 
                              : 'Not completed'}
                          </Typography>
                        </Grid>
                        
                        <Grid item xs={12}>
                          <Typography variant="subtitle2">Responsible Employee</Typography>
                          <Typography>
                            {section.responsibleEmployee 
                              ? `${section.responsibleEmployee.firstName} ${section.responsibleEmployee.lastName}` 
                              : 'Not assigned'}
                          </Typography>
                        </Grid>
                        
                        <Grid item xs={12}>
                          <Box sx={{ mt: 2 }}>
                            <Typography variant="subtitle2">Materials</Typography>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                              <Chip 
                                label={section.materialsOrdered ? 'Materials Ordered' : 'Materials Not Ordered'} 
                                color={section.materialsOrdered ? 'success' : 'default'} 
                                size="small"
                                sx={{ mr: 1 }}
                              />
                              <Chip 
                                label={section.materialsReceived ? 'Materials Received' : 'Materials Not Received'} 
                                color={section.materialsReceived ? 'success' : 'default'} 
                                size="small"
                              />
                            </Box>
                          </Box>
                        </Grid>
                        
                        {section.materialsRequired && (
                          <Grid item xs={12}>
                            <Typography variant="subtitle2">Materials Required</Typography>
                            <Typography>{section.materialsRequired}</Typography>
                          </Grid>
                        )}
                      </Grid>
                    ) : (
                      <Grid container spacing={2}>
                        <Grid item xs={12}>
                          <TextField
                            fullWidth
                            label="Description"
                            value={editFormData.description || ''}
                            onChange={(e) => handleEditFormChange('description', e.target.value)}
                            multiline
                            rows={2}
                          />
                        </Grid>
                        
                        <Grid item xs={12}>
                          <FormControl fullWidth>
                            <InputLabel>Status</InputLabel>
                            <Select
                              value={editFormData.status}
                              onChange={(e) => handleEditFormChange('status', e.target.value)}
                              label="Status"
                            >
                              {statuses.map(status => (
                                <MenuItem key={status.id} value={status.id}>
                                  {status.name}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </Grid>
                        
                        <Grid item xs={12} sm={6}>
                          <DatePicker
                            label="Start Date"
                            value={editFormData.startDate ? new Date(editFormData.startDate) : null}
                            onChange={(date) => handleEditFormChange('startDate', date)}
                            slotProps={{
                              textField: {
                                fullWidth: true
                              }
                            }}
                          />
                        </Grid>
                        
                        <Grid item xs={12} sm={6}>
                          <DatePicker
                            label="Completion Date"
                            value={editFormData.completionDate ? new Date(editFormData.completionDate) : null}
                            onChange={(date) => handleEditFormChange('completionDate', date)}
                            slotProps={{
                              textField: {
                                fullWidth: true
                              }
                            }}
                          />
                        </Grid>
                        
                        <Grid item xs={12}>
                          <FormControl fullWidth>
                            <InputLabel>Responsible Employee</InputLabel>
                            <Select
                              value={editFormData.responsibleEmployeeId || ''}
                              onChange={(e) => handleEditFormChange('responsibleEmployeeId', e.target.value)}
                              label="Responsible Employee"
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
                        
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" gutterBottom>
                            Materials
                          </Typography>
                          <Grid container spacing={2}>
                            <Grid item xs={12} sm={6}>
                              <FormControlLabel
                                control={
                                  <Switch
                                    checked={editFormData.materialsOrdered || false}
                                    onChange={(e) => handleEditFormChange('materialsOrdered', e.target.checked)}
                                  />
                                }
                                label="Materials Ordered"
                              />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                              <FormControlLabel
                                control={
                                  <Switch
                                    checked={editFormData.materialsReceived || false}
                                    onChange={(e) => handleEditFormChange('materialsReceived', e.target.checked)}
                                  />
                                }
                                label="Materials Received"
                              />
                            </Grid>
                          </Grid>
                        </Grid>
                        
                        <Grid item xs={12}>
                          <TextField
                            fullWidth
                            label="Materials Required"
                            value={editFormData.materialsRequired || ''}
                            onChange={(e) => handleEditFormChange('materialsRequired', e.target.value)}
                            multiline
                            rows={2}
                          />
                        </Grid>
                      </Grid>
                    )}
                  </CardContent>
                </Card>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardHeader title="Additional Information" />
                  <CardContent>
                    {!editMode ? (
                      <Grid container spacing={2}>
                        {section.sectionType === 1 && ( // Permit section
                          <>
                            <Grid item xs={12}>
                              <Typography variant="subtitle2">Permit Number</Typography>
                              <Typography>{section.permitNumber || 'Not available'}</Typography>
                            </Grid>
                            
                            <Grid item xs={12} sm={6}>
                              <Typography variant="subtitle2">Permit Issue Date</Typography>
                              <Typography>
                                {section.permitIssueDate 
                                  ? new Date(section.permitIssueDate).toLocaleDateString() 
                                  : 'Not issued'}
                              </Typography>
                            </Grid>
                            
                            <Grid item xs={12} sm={6}>
                              <Typography variant="subtitle2">Permit Expiration Date</Typography>
                              <Typography>
                                {section.permitExpirationDate 
                                  ? new Date(section.permitExpirationDate).toLocaleDateString() 
                                  : 'Not specified'}
                              </Typography>
                            </Grid>
                          </>
                        )}
                        
                        <Grid item xs={12}>
                          <Typography variant="subtitle2">Additional Notes</Typography>
                          <Typography>{section.additionalNotes || 'No additional notes'}</Typography>
                        </Grid>
                      </Grid>
                    ) : (
                      <Grid container spacing={2}>
                        {section.sectionType === 1 && ( // Permit section
                          <>
                            <Grid item xs={12}>
                              <TextField
                                fullWidth
                                label="Permit Number"
                                value={editFormData.permitNumber || ''}
                                onChange={(e) => handleEditFormChange('permitNumber', e.target.value)}
                              />
                            </Grid>
                            
                            <Grid item xs={12} sm={6}>
                              <DatePicker
                                label="Permit Issue Date"
                                value={editFormData.permitIssueDate ? new Date(editFormData.permitIssueDate) : null}
                                onChange={(date) => handleEditFormChange('permitIssueDate', date)}
                                slotProps={{
                                  textField: {
                                    fullWidth: true
                                  }
                                }}
                              />
                            </Grid>
                            
                            <Grid item xs={12} sm={6}>
                              <DatePicker
                                label="Permit Expiration Date"
                                value={editFormData.permitExpirationDate ? new Date(editFormData.permitExpirationDate) : null}
                                onChange={(date) => handleEditFormChange('permitExpirationDate', date)}
                                slotProps={{
                                  textField: {
                                    fullWidth: true
                                  }
                                }}
                              />
                            </Grid>
                          </>
                        )}
                        
                        <Grid item xs={12}>
                          <TextField
                            fullWidth
                            label="Additional Notes"
                            value={editFormData.additionalNotes || ''}
                            onChange={(e) => handleEditFormChange('additionalNotes', e.target.value)}
                            multiline
                            rows={4}
                          />
                        </Grid>
                      </Grid>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          )}
          
          {activeTab === 1 && (
            <Box>
              <Card variant="outlined">
                <CardHeader 
                  title="Inspection Information"
                  action={
                    canEdit && (
                      <Box>
                        <Button
                          variant="outlined"
                          startIcon={<EventIcon />}
                          onClick={handleOpenInspectionDialog}
                          sx={{ mr: 1 }}
                        >
                          Schedule Inspection
                        </Button>
                        <Button
                          variant="outlined"
                          startIcon={<CheckCircleIcon />}
                          onClick={handleOpenInspectionResultDialog}
                          disabled={!section.inspectionDate}
                        >
                          Record Result
                        </Button>
                      </Box>
                    )
                  }
                />
                <CardContent>
                  <Grid container spacing={3}>
                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle2">Inspection Date</Typography>
                      <Typography>
                        {section.inspectionDate 
                          ? new Date(section.inspectionDate).toLocaleDateString() 
                          : 'Not scheduled'}
                      </Typography>
                    </Grid>
                    
                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle2">Reinspection Date</Typography>
                      <Typography>
                        {section.reinspectionDate 
                          ? new Date(section.reinspectionDate).toLocaleDateString() 
                          : 'Not applicable'}
                      </Typography>
                    </Grid>
                    
                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle2">Inspection Result</Typography>
                      <Typography>
                        {section.inspectionResult 
                          ? section.inspectionResult 
                          : 'No result recorded'}
                      </Typography>
                    </Grid>
                    
                    <Grid item xs={12}>
                      <Typography variant="subtitle2">Inspection Notes</Typography>
                      <Typography>
                        {section.inspectionNotes || 'No inspection notes'}
                      </Typography>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
              
              <Box sx={{ mt: 3 }}>
                <Accordion>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Typography variant="h6">Inspection Requirements</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Typography variant="body1" paragraph>
                      Below are inspection requirements based on the section type:
                    </Typography>
                    
                    {section.sectionType === 1 && (
                      <Box>
                        <Typography variant="subtitle1">Permit Inspection Requirements:</Typography>
                        <ul>
                          <li>Verify permit is displayed at jobsite</li>
                          <li>Confirm all required documentation is available</li>
                          <li>Check that permit details match actual work being performed</li>
                          <li>Ensure proper signatures are present on all documentation</li>
                        </ul>
                      </Box>
                    )}
                    
                    {section.sectionType === 2 && (
                      <Box>
                        <Typography variant="subtitle1">Demolition Inspection Requirements:</Typography>
                        <ul>
                          <li>Verify proper disconnection of utilities</li>
                          <li>Confirm safety barriers and signage are in place</li>
                          <li>Check for proper disposal of hazardous materials</li>
                          <li>Ensure structural supports are maintained where required</li>
                        </ul>
                      </Box>
                    )}
                    
                    {section.sectionType === 3 && (
                      <Box>
                        <Typography variant="subtitle1">Rough Plumbing Inspection Requirements:</Typography>
                        <ul>
                          <li>Verify proper pipe sizing and materials</li>
                          <li>Check for proper slope on drainage pipes</li>
                          <li>Confirm proper venting installation</li>
                          <li>Pressure test results for water supply lines</li>
                        </ul>
                      </Box>
                    )}
                    
                    {/* Add more section types as needed */}
                    
                    <Typography variant="body2" color="textSecondary" sx={{ mt: 2 }}>
                      Note: Always refer to Massachusetts state building codes and local requirements for complete inspection criteria.
                    </Typography>
                  </AccordionDetails>
                </Accordion>
              </Box>
            </Box>
          )}
          
          {activeTab === 2 && (
            <Box>
              <Card variant="outlined">
                <CardHeader 
                  title="Subcontractor Management"
                />
                <CardContent>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={isSubcontracted}
                        onChange={(e) => setIsSubcontracted(e.target.checked)}
                        disabled={saving}
                      />
                    }
                    label={`This section is ${isSubcontracted ? 'subcontracted' : 'handled in-house'}`}
                  />
                  
                  {isSubcontracted && (
                    <Box sx={{ mt: 2 }}>
                      <Grid container spacing={2}>
                        <Grid item xs={12} md={6}>
                          <FormControl fullWidth>
                            <InputLabel>Subcontractor</InputLabel>
                            <Select
                              value={selectedSubcontractorId || ''}
                              onChange={(e) => setSelectedSubcontractorId(e.target.value)}
                              label="Subcontractor"
                              disabled={saving}
                            >
                              <MenuItem value="">
                                <em>Select a subcontractor</em>
                              </MenuItem>
                              {subcontractors.map(subcontractor => (
                                <MenuItem key={subcontractor.id} value={subcontractor.id}>
                                  {subcontractor.companyName}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </Grid>
                        
                        <Grid item xs={12} md={6}>
                          <TextField
                            fullWidth
                            label="Contract Reference"
                            value={contractReference}
                            onChange={(e) => setContractReference(e.target.value)}
                            disabled={saving}
                          />
                        </Grid>
                      </Grid>
                    </Box>
                  )}
                  
                  <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
                    <Button
                      variant="contained"
                      color="primary"
                      onClick={handleToggleSubcontractor}
                      disabled={saving || (isSubcontracted && !selectedSubcontractorId)}
                    >
                      {saving ? <CircularProgress size={24} /> : 'Save Subcontractor Settings'}
                    </Button>
                  </Box>
                  
                  {section.isSubcontracted && section.subcontractor && (
                    <Box sx={{ mt: 4 }}>
                      <Divider sx={{ mb: 2 }} />
                      <Typography variant="h6" gutterBottom>
                        Current Subcontractor Information
                      </Typography>
                      
                      <Grid container spacing={2}>
                        <Grid item xs={12} md={6}>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <BusinessIcon sx={{ mr: 1 }} color="primary" />
                            <Box>
                              <Typography variant="subtitle2">Company Name</Typography>
                              <Typography>{section.subcontractor.companyName}</Typography>
                            </Box>
                          </Box>
                        </Grid>
                        
                        <Grid item xs={12} md={6}>
                          <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            <PersonIcon sx={{ mr: 1 }} color="primary" />
                            <Box>
                              <Typography variant="subtitle2">Contact Person</Typography>
                              <Typography>{section.subcontractor.contactName || 'Not specified'}</Typography>
                            </Box>
                          </Box>
                        </Grid>
                        
                        <Grid item xs={12} md={6}>
                          <Typography variant="subtitle2">Contact Information</Typography>
                          <Typography>
                            {section.subcontractor.phone && `Phone: ${section.subcontractor.phone}`}
                          </Typography>
                          <Typography>
                            {section.subcontractor.email && `Email: ${section.subcontractor.email}`}
                          </Typography>
                        </Grid>
                        
                        <Grid item xs={12} md={6}>
                          <Typography variant="subtitle2">License & Insurance</Typography>
                          <Typography>
                            {section.subcontractor.licenseNumber && `License: ${section.subcontractor.licenseNumber}`}
                          </Typography>
                          <Typography>
                            {section.subcontractor.insuranceInfo && `Insurance: ${section.subcontractor.insuranceInfo}`}
                          </Typography>
                        </Grid>
                        
                        <Grid item xs={12}>
                          <Button
                            variant="outlined"
                            color="primary"
                            component={RouterLink}
                            to={`/subcontractors/${section.subcontractor.id}`}
                          >
                            View Full Subcontractor Details
                          </Button>
                        </Grid>
                      </Grid>
                    </Box>
                  )}
                </CardContent>
              </Card>
            </Box>
          )}
          
          {activeTab === 3 && (
            <Box>
              <Card variant="outlined">
                <CardHeader 
                  title="AI Building Code Assistant"
                  subheader="Get expert guidance on building codes for this section"
                  avatar={<AiIcon color="primary" />}
                />
                <CardContent>
                  {loadingAi ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                      <CircularProgress />
                    </Box>
                  ) : aiGuidance ? (
                    <Box>
                      <Typography variant="body1" gutterBottom>
                        {aiGuidance.guidance || aiGuidance.response}
                      </Typography>
                      
                      <Button
                        variant="outlined"
                        onClick={fetchAiGuidance}
                        startIcon={<AiIcon />}
                        sx={{ mt: 2 }}
                      >
                        Refresh Guidance
                      </Button>
                    </Box>
                  ) : (
                    <Box sx={{ textAlign: 'center', py: 3 }}>
                      <Typography variant="body1" gutterBottom>
                        Click the button below to get AI-powered guidance on building code requirements 
                        for {getSectionTypeName(section.sectionType)} sections.
                      </Typography>
                      
                      <Button
                        variant="contained"
                        color="primary"
                        onClick={fetchAiGuidance}
                        startIcon={<AiIcon />}
                        sx={{ mt: 2 }}
                      >
                        Generate Guidance
                      </Button>
                    </Box>
                  )}
                </CardContent>
              </Card>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="outlined"
                  color="primary"
                  component={RouterLink}
                  to="/aiassistant"
                  startIcon={<AiIcon />}
                >
                  Open Full AI Assistant
                </Button>
              </Box>
            </Box>
          )}
        </Paper>
        
        <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between' }}>
          <Button component={RouterLink} to={`/jobs/${section.jobId}`} variant="outlined">
            Back to Job
          </Button>
          
          <Button component={RouterLink} to="/sections" variant="outlined">
            Back to Sections
          </Button>
        </Box>
        
        {/* Schedule Inspection Dialog */}
        <Dialog open={inspectionDialogOpen} onClose={() => setInspectionDialogOpen(false)}>
          <DialogTitle>Schedule Inspection</DialogTitle>
          <DialogContent>
            <DialogContentText sx={{ mb: 2 }}>
              Please select a date for the inspection:
            </DialogContentText>
            
            <DatePicker
              label="Inspection Date"
              value={inspectionDate}
              onChange={(date) => setInspectionDate(date)}
              slotProps={{
                textField: {
                  fullWidth: true,
                  margin: "normal"
                }
              }}
              minDate={new Date()}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setInspectionDialogOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button 
              onClick={handleScheduleInspection} 
              color="primary" 
              disabled={saving || !inspectionDate}
            >
              {saving ? <CircularProgress size={24} /> : 'Schedule'}
            </Button>
          </DialogActions>
        </Dialog>
        
        {/* Record Inspection Result Dialog */}
        <Dialog 
          open={inspectionResultDialogOpen} 
          onClose={() => setInspectionResultDialogOpen(false)}
          maxWidth="sm"
          fullWidth
        >
          <DialogTitle>Record Inspection Result</DialogTitle>
          <DialogContent>
            <FormControl fullWidth sx={{ mt: 2 }}>
              <InputLabel>Inspection Result</InputLabel>
              <Select
                value={inspectionResult.result}
                onChange={(e) => setInspectionResult({...inspectionResult, result: e.target.value})}
                label="Inspection Result"
              >
                <MenuItem value="pass">Pass</MenuItem>
                <MenuItem value="fail">Fail</MenuItem>
              </Select>
            </FormControl>
            
            <TextField
              margin="normal"
              fullWidth
              label="Inspection Notes"
              multiline
              rows={3}
              value={inspectionResult.notes}
              onChange={(e) => setInspectionResult({...inspectionResult, notes: e.target.value})}
            />
            
            {inspectionResult.result === 'fail' && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Schedule Reinspection Date
                </Typography>
                <DatePicker
                  label="Reinspection Date"
                  value={inspectionResult.reinspectionDate}
                  onChange={(date) => setInspectionResult({...inspectionResult, reinspectionDate: date})}
                  slotProps={{
                    textField: {
                      fullWidth: true
                    }
                  }}
                  minDate={new Date()}
                />
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setInspectionResultDialogOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button 
              onClick={handleRecordInspection} 
              color="primary" 
              disabled={saving}
            >
              {saving ? <CircularProgress size={24} /> : 'Record Result'}
            </Button>
          </DialogActions>
        </Dialog>
      </Container>
    </LocalizationProvider>
  );
}

export default SectionDetail;
