import React, { useState, useEffect } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Grid,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TableContainer,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  Chip,
  IconButton,
  CircularProgress,
  Tooltip,
  Alert
} from '@mui/material';
import {
  FilterList as FilterIcon,
  Search as SearchIcon,
  Clear as ClearIcon,
  Visibility as ViewIcon
} from '@mui/icons-material';
import { sectionService } from '../../services/sectionService';
import { jobService } from '../../services/jobService';

function SectionList() {
  const navigate = useNavigate();
  const [sections, setSections] = useState([]);
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  // Filters
  const [filterJobId, setFilterJobId] = useState('');
  const [filterSectionType, setFilterSectionType] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        
        // Fetch all sections
        const sectionsData = await sectionService.getAllSections();
        setSections(sectionsData);
        
        // Fetch all jobs for filter dropdown
        const jobsData = await jobService.getJobs();
        setJobs(jobsData);
        
        setError('');
      } catch (error) {
        console.error('Error fetching data:', error);
        setError('Failed to load sections. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const filteredSections = sections.filter(section => {
    // Apply job filter
    if (filterJobId && section.jobId !== parseInt(filterJobId)) {
      return false;
    }
    
    // Apply section type filter
    if (filterSectionType && section.sectionType !== parseInt(filterSectionType)) {
      return false;
    }
    
    // Apply status filter
    if (filterStatus && section.status !== parseInt(filterStatus)) {
      return false;
    }
    
    // Apply search term
    if (searchTerm) {
      const job = jobs.find(j => j.id === section.jobId);
      const searchFields = [
        job?.name?.toLowerCase() || '',
        job?.jobNumber?.toLowerCase() || '',
        section.description?.toLowerCase() || ''
      ];
      
      return searchFields.some(field => field.includes(searchTerm.toLowerCase()));
    }
    
    return true;
  });

  const handleClearFilters = () => {
    setFilterJobId('');
    setFilterSectionType('');
    setFilterStatus('');
    setSearchTerm('');
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

  const sectionTypes = [
    { id: 1, name: 'Permit' },
    { id: 2, name: 'Demolition' },
    { id: 3, name: 'Rough Plumbing' },
    { id: 4, name: 'Rough Electrical' },
    { id: 5, name: 'Framing' },
    { id: 6, name: 'Insulation' },
    { id: 7, name: 'Sheetrock' },
    { id: 8, name: 'Paint Prep' },
    { id: 9, name: 'Finish Install' },
    { id: 10, name: 'Finish Paint/Flooring' },
    { id: 11, name: 'Kitchen & Bath Fixtures' },
    { id: 12, name: 'Miscellaneous' }
  ];

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

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Job Sections
      </Typography>
      
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      
      <Paper sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} md={3}>
            <TextField
              fullWidth
              variant="outlined"
              label="Search"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              InputProps={{
                startAdornment: <SearchIcon color="action" sx={{ mr: 1 }} />,
                endAdornment: searchTerm && (
                  <IconButton size="small" onClick={() => setSearchTerm('')}>
                    <ClearIcon />
                  </IconButton>
                )
              }}
            />
          </Grid>
          
          <Grid item xs={12} md={3}>
            <FormControl fullWidth variant="outlined">
              <InputLabel>Job</InputLabel>
              <Select
                value={filterJobId}
                onChange={(e) => setFilterJobId(e.target.value)}
                label="Job"
              >
                <MenuItem value="">
                  <em>All Jobs</em>
                </MenuItem>
                {jobs.map(job => (
                  <MenuItem key={job.id} value={job.id}>
                    {job.name} ({job.jobNumber})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={2}>
            <FormControl fullWidth variant="outlined">
              <InputLabel>Section Type</InputLabel>
              <Select
                value={filterSectionType}
                onChange={(e) => setFilterSectionType(e.target.value)}
                label="Section Type"
              >
                <MenuItem value="">
                  <em>All Types</em>
                </MenuItem>
                {sectionTypes.map(type => (
                  <MenuItem key={type.id} value={type.id}>
                    {type.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={2}>
            <FormControl fullWidth variant="outlined">
              <InputLabel>Status</InputLabel>
              <Select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                label="Status"
              >
                <MenuItem value="">
                  <em>All Statuses</em>
                </MenuItem>
                {statuses.map(status => (
                  <MenuItem key={status.id} value={status.id}>
                    {status.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={2}>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<ClearIcon />}
              onClick={handleClearFilters}
              disabled={!filterJobId && !filterSectionType && !filterStatus && !searchTerm}
            >
              Clear Filters
            </Button>
          </Grid>
        </Grid>
      </Paper>
      
      <TableContainer component={Paper} className="responsive-table">
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
            <CircularProgress />
          </Box>
        ) : filteredSections.length === 0 ? (
          <Box sx={{ p: 3, textAlign: 'center' }}>
            <Typography variant="h6" color="textSecondary">
              No sections found
            </Typography>
            {(filterJobId || filterSectionType || filterStatus || searchTerm) && (
              <Button
                variant="text"
                color="primary"
                onClick={handleClearFilters}
                sx={{ mt: 1 }}
              >
                Clear Filters
              </Button>
            )}
          </Box>
        ) : (
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Job</TableCell>
                <TableCell>Section Type</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Responsible Employee</TableCell>
                <TableCell>Inspection Date</TableCell>
                <TableCell>Subcontracted</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredSections.map(section => {
                const job = jobs.find(j => j.id === section.jobId);
                return (
                  <TableRow key={section.id}>
                    <TableCell>
                      {job ? (
                        <RouterLink to={`/jobs/${job.id}`}>
                          {job.name} ({job.jobNumber})
                        </RouterLink>
                      ) : (
                        `Job ID: ${section.jobId}`
                      )}
                    </TableCell>
                    <TableCell>{getSectionTypeName(section.sectionType)}</TableCell>
                    <TableCell>
                      <Chip
                        label={getStatusName(section.status)}
                        color={getStatusColor(section.status)}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      {section.responsibleEmployee ? 
                        `${section.responsibleEmployee.firstName} ${section.responsibleEmployee.lastName}` : 
                        'Unassigned'
                      }
                    </TableCell>
                    <TableCell>
                      {section.inspectionDate ? 
                        new Date(section.inspectionDate).toLocaleDateString() : 
                        'Not scheduled'
                      }
                    </TableCell>
                    <TableCell>
                      {section.isSubcontracted ? (
                        <Chip 
                          label={section.subcontractor?.companyName || 'Yes'} 
                          color="primary" 
                          size="small" 
                        />
                      ) : 'No'}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View Section">
                        <IconButton
                          component={RouterLink}
                          to={`/sections/${section.id}`}
                          size="small"
                        >
                          <ViewIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}
      </TableContainer>
    </Container>
  );
}

export default SectionList;
