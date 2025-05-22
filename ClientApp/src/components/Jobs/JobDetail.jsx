import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Grid,
  Button,
  Divider,
  Chip,
  IconButton,
  Card,
  CardContent,
  CardHeader,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Tab,
  Tabs,
  Alert
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Assignment as SectionIcon,
  Person as PersonIcon,
  Business as ClientIcon,
  CalendarToday as CalendarIcon,
  AttachMoney as MoneyIcon,
  Description as DocIcon,
  CheckCircle as CheckIcon,
  Warning as WarningIcon,
  PendingActions as PendingIcon
} from '@mui/icons-material';
import { jobService } from '../../services/jobService';
import { sectionService } from '../../services/sectionService';
import { authService } from '../../services/authService';

function JobDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [job, setJob] = useState(null);
  const [sections, setSections] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const [exportLoading, setExportLoading] = useState({
    excel: false,
    word: false,
    google: false
  });

  const canEdit = authService.hasRole(['Admin', 'ProjectManager']);
  const canDelete = authService.isAdmin();

  useEffect(() => {
    const fetchJobDetails = async () => {
      try {
        setLoading(true);
        const jobData = await jobService.getJobById(id);
        setJob(jobData);
        
        // Fetch job sections
        const sectionsData = await sectionService.getSectionsByJobId(id);
        setSections(sectionsData);
        
        setError('');
      } catch (error) {
        console.error(`Error fetching job details for ID ${id}:`, error);
        setError('Failed to load job details. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    fetchJobDetails();
  }, [id]);

  const handleDeleteClick = () => {
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    try {
      setDeleteLoading(true);
      await jobService.deleteJob(id);
      setDeleteDialogOpen(false);
      navigate('/jobs');
    } catch (error) {
      console.error(`Error deleting job with ID ${id}:`, error);
      setError('Failed to delete job. Please try again later.');
      setDeleteLoading(false);
      setDeleteDialogOpen(false);
    }
  };

  const handleExportExcel = async () => {
    try {
      setExportLoading(prev => ({ ...prev, excel: true }));
      await jobService.exportJobToExcel(id);
    } catch (error) {
      console.error(`Error exporting job to Excel with ID ${id}:`, error);
      setError('Failed to export job to Excel. Please try again later.');
    } finally {
      setExportLoading(prev => ({ ...prev, excel: false }));
    }
  };

  const handleExportWord = async () => {
    try {
      setExportLoading(prev => ({ ...prev, word: true }));
      await jobService.exportJobToWord(id);
    } catch (error) {
      console.error(`Error exporting job to Word with ID ${id}:`, error);
      setError('Failed to export job to Word. Please try again later.');
    } finally {
      setExportLoading(prev => ({ ...prev, word: false }));
    }
  };

  const handleCreateGoogleDoc = async () => {
    try {
      setExportLoading(prev => ({ ...prev, google: true }));
      const result = await jobService.createGoogleDoc(id);
      
      if (result && result.documentId) {
        alert(`Google Doc created successfully. Document ID: ${result.documentId}`);
      }
    } catch (error) {
      console.error(`Error creating Google Doc for job with ID ${id}:`, error);
      setError('Failed to create Google Doc. Please try again later.');
    } finally {
      setExportLoading(prev => ({ ...prev, google: false }));
    }
  };

  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'In Progress':
        return 'primary';
      case 'Pending':
        return 'info';
      case 'Delayed':
        return 'warning';
      case 'Cancelled':
        return 'error';
      default:
        return 'default';
    }
  };

  const getSectionStatusIcon = (status) => {
    switch (status) {
      case 1: // NotStarted
        return <PendingIcon color="disabled" />;
      case 2: // InProgress
        return <PendingIcon color="primary" />;
      case 3: // Completed
        return <CheckIcon color="success" />;
      case 4: // Denied
        return <WarningIcon color="error" />;
      case 5: // OnHold
        return <WarningIcon color="warning" />;
      case 6: // NeedsInspection
        return <WarningIcon color="info" />;
      case 7: // PassedInspection
        return <CheckIcon color="success" />;
      case 8: // FailedInspection
        return <WarningIcon color="error" />;
      default:
        return <PendingIcon color="disabled" />;
    }
  };

  const getSectionStatusName = (status) => {
    switch (status) {
      case 1: return 'Not Started';
      case 2: return 'In Progress';
      case 3: return 'Completed';
      case 4: return 'Denied';
      case 5: return 'On Hold';
      case 6: return 'Needs Inspection';
      case 7: return 'Passed Inspection';
      case 8: return 'Failed Inspection';
      default: return 'Unknown';
    }
  };

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
          <Button component={RouterLink} to="/jobs" variant="contained">
            Back to Jobs
          </Button>
        </Box>
      </Container>
    );
  }

  if (!job) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="info">Job not found</Alert>
        <Box sx={{ mt: 2 }}>
          <Button component={RouterLink} to="/jobs" variant="contained">
            Back to Jobs
          </Button>
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper sx={{ p: 2, position: 'relative' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Typography variant="h4" component="h1">
            {job.name}
          </Typography>
          
          <Box>
            {canEdit && (
              <IconButton
                component={RouterLink}
                to={`/jobs/${id}/edit`}
                color="primary"
                aria-label="edit job"
                sx={{ mr: 1 }}
              >
                <EditIcon />
              </IconButton>
            )}
            
            {canDelete && (
              <IconButton
                color="error"
                aria-label="delete job"
                onClick={handleDeleteClick}
              >
                <DeleteIcon />
              </IconButton>
            )}
          </Box>
        </Box>
        
        <Box sx={{ mb: 3 }}>
          <Chip 
            label={job.status} 
            color={getStatusColor(job.status)} 
            sx={{ mr: 1 }}
          />
          <Chip 
            label={`Job #: ${job.jobNumber}`} 
            variant="outlined"
            sx={{ mr: 1 }}
          />
        </Box>
        
        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 2 }}>
          <Tab label="Overview" />
          <Tab label="Sections" />
          <Tab label="Assignments" />
          <Tab label="Documents" />
        </Tabs>
        
        {activeTab === 0 && (
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Card variant="outlined">
                <CardHeader title="Job Details" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12}>
                      <Typography variant="subtitle2">Description</Typography>
                      <Typography>{job.description || 'No description provided'}</Typography>
                    </Grid>
                    
                    <Grid item xs={12} sm={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <CalendarIcon sx={{ mr: 1 }} color="primary" />
                        <Box>
                          <Typography variant="subtitle2">Start Date</Typography>
                          <Typography>{new Date(job.startDate).toLocaleDateString()}</Typography>
                        </Box>
                      </Box>
                    </Grid>
                    
                    <Grid item xs={12} sm={6}>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <CalendarIcon sx={{ mr: 1 }} color="primary" />
                        <Box>
                          <Typography variant="subtitle2">Target Completion</Typography>
                          <Typography>
                            {job.targetCompletionDate 
                              ? new Date(job.targetCompletionDate).toLocaleDateString() 
                              : 'Not specified'}
                          </Typography>
                        </Box>
                      </Box>
                    </Grid>
                    
                    <Grid item xs={12}>
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <MoneyIcon sx={{ mr: 1 }} color="primary" />
                        <Box>
                          <Typography variant="subtitle2">Budget</Typography>
                          <Typography>${job.budget.toLocaleString()}</Typography>
                        </Box>
                      </Box>
                    </Grid>
                    
                    <Grid item xs={12}>
                      <Typography variant="subtitle2">Location</Typography>
                      <Typography>{job.location}</Typography>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Card variant="outlined">
                <CardHeader title="Client Information" />
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <ClientIcon sx={{ mr: 1 }} color="primary" />
                    <Box>
                      <Typography variant="subtitle2">Client Name</Typography>
                      <Typography>{job.clientName || 'Not specified'}</Typography>
                    </Box>
                  </Box>
                  
                  {job.clientEmail && (
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Box>
                        <Typography variant="subtitle2">Email</Typography>
                        <Typography>{job.clientEmail}</Typography>
                      </Box>
                    </Box>
                  )}
                  
                  {job.clientPhone && (
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <Box>
                        <Typography variant="subtitle2">Phone</Typography>
                        <Typography>{job.clientPhone}</Typography>
                      </Box>
                    </Box>
                  )}
                  
                  {job.projectManager && (
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <PersonIcon sx={{ mr: 1 }} color="primary" />
                      <Box>
                        <Typography variant="subtitle2">Project Manager</Typography>
                        <Typography>{job.projectManager.firstName} {job.projectManager.lastName}</Typography>
                      </Box>
                    </Box>
                  )}
                </CardContent>
              </Card>
              
              {job.notes && (
                <Card variant="outlined" sx={{ mt: 2 }}>
                  <CardHeader title="Notes" />
                  <CardContent>
                    <Typography>{job.notes}</Typography>
                  </CardContent>
                </Card>
              )}
            </Grid>
          </Grid>
        )}
        
        {activeTab === 1 && (
          <Box>
            <Box sx={{ mb: 2 }}>
              <Typography variant="h6" gutterBottom>
                Job Sections
              </Typography>
              <Typography variant="body2" color="textSecondary" gutterBottom>
                {sections.length} sections in this job
              </Typography>
            </Box>
            
            <Grid container spacing={2}>
              {sections.map(section => (
                <Grid item xs={12} sm={6} md={4} key={section.id}>
                  <Card 
                    variant="outlined" 
                    sx={{ 
                      height: '100%',
                      bgcolor: `section-status-${getSectionStatusName(section.status).toLowerCase().replace(' ', '-')}`
                    }}
                  >
                    <CardHeader
                      title={section.sectionType.getDisplayName?.() || `Section ${section.sectionType}`}
                      subheader={getSectionStatusName(section.status)}
                      avatar={getSectionStatusIcon(section.status)}
                      action={
                        <Button 
                          component={RouterLink} 
                          to={`/sections/${section.id}`}
                          size="small"
                        >
                          View
                        </Button>
                      }
                    />
                    <CardContent>
                      {section.isSubcontracted && (
                        <Chip 
                          label={`Subcontractor: ${section.subcontractor?.companyName || 'Assigned'}`} 
                          size="small" 
                          sx={{ mb: 1 }}
                        />
                      )}
                      
                      {section.responsibleEmployee && (
                        <Typography variant="body2">
                          Responsible: {section.responsibleEmployee.firstName} {section.responsibleEmployee.lastName}
                        </Typography>
                      )}
                      
                      {section.inspectionDate && (
                        <Typography variant="body2">
                          Inspection: {new Date(section.inspectionDate).toLocaleDateString()}
                        </Typography>
                      )}
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </Box>
        )}
        
        {activeTab === 2 && (
          <Box>
            <Typography variant="h6" gutterBottom>
              Job Assignments
            </Typography>
            
            {job.assignments && job.assignments.length > 0 ? (
              <List>
                {job.assignments.map(assignment => (
                  <ListItem key={assignment.id} divider>
                    <ListItemIcon>
                      <PersonIcon />
                    </ListItemIcon>
                    <ListItemText
                      primary={`${assignment.employee.firstName} ${assignment.employee.lastName}`}
                      secondary={`Role: ${assignment.role || 'Not specified'} | Date: ${new Date(assignment.assignmentDate).toLocaleDateString()}`}
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Typography variant="body2" color="textSecondary">
                No assignments found for this job.
              </Typography>
            )}
          </Box>
        )}
        
        {activeTab === 3 && (
          <Box>
            <Typography variant="h6" gutterBottom>
              Export Options
            </Typography>
            
            <Grid container spacing={2}>
              <Grid item xs={12} sm={4}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<DocIcon />}
                  onClick={handleExportExcel}
                  disabled={exportLoading.excel}
                >
                  {exportLoading.excel ? <CircularProgress size={24} /> : 'Export to Excel'}
                </Button>
              </Grid>
              
              <Grid item xs={12} sm={4}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<DocIcon />}
                  onClick={handleExportWord}
                  disabled={exportLoading.word}
                >
                  {exportLoading.word ? <CircularProgress size={24} /> : 'Export to Word'}
                </Button>
              </Grid>
              
              <Grid item xs={12} sm={4}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<DocIcon />}
                  onClick={handleCreateGoogleDoc}
                  disabled={exportLoading.google}
                >
                  {exportLoading.google ? <CircularProgress size={24} /> : 'Create Google Doc'}
                </Button>
              </Grid>
            </Grid>
          </Box>
        )}
      </Paper>
      
      <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between' }}>
        <Button component={RouterLink} to="/jobs" variant="outlined">
          Back to Jobs
        </Button>
        
        {canEdit && (
          <Button 
            component={RouterLink} 
            to={`/jobs/${id}/edit`} 
            variant="contained" 
            color="primary"
            startIcon={<EditIcon />}
          >
            Edit Job
          </Button>
        )}
      </Box>
      
      <Dialog
        open={deleteDialogOpen}
        onClose={() => setDeleteDialogOpen(false)}
      >
        <DialogTitle>Confirm Deletion</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete this job? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleteLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleDeleteConfirm} 
            color="error" 
            autoFocus
            disabled={deleteLoading}
          >
            {deleteLoading ? <CircularProgress size={24} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}

export default JobDetail;
