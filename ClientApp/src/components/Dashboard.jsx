import React, { useState, useEffect } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
  CardActions,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  CircularProgress,
  Box,
  Chip,
  LinearProgress
} from '@mui/material';
import {
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Schedule as ScheduleIcon,
  BusinessCenter as JobIcon,
  Assignment as SectionIcon,
  People as SubcontractorIcon
} from '@mui/icons-material';
import { jobService } from '../services/jobService';
import { sectionService } from '../services/sectionService';
import { timeTrackingService } from '../services/timeTrackingService';

function Dashboard() {
  const [loading, setLoading] = useState(true);
  const [recentJobs, setRecentJobs] = useState([]);
  const [jobStats, setJobStats] = useState({ active: 0, completed: 0, overdue: 0 });
  const [sectionStats, setSectionStats] = useState({ completed: 0, inProgress: 0, needsInspection: 0, failed: 0 });
  const [upcomingInspections, setUpcomingInspections] = useState([]);
  const [clockedInStatus, setClockedInStatus] = useState(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        
        // Get recent jobs
        const jobs = await jobService.getJobs();
        setRecentJobs(jobs.slice(0, 5));
        
        // Calculate job statistics
        const active = jobs.filter(job => job.status !== 'Completed' && job.status !== 'Cancelled').length;
        const completed = jobs.filter(job => job.status === 'Completed').length;
        const overdue = jobs.filter(job => {
          if (!job.targetCompletionDate) return false;
          const targetDate = new Date(job.targetCompletionDate);
          return job.status !== 'Completed' && targetDate < new Date();
        }).length;
        
        setJobStats({ active, completed, overdue });
        
        // Get sections data
        const sections = await sectionService.getAllSections();
        
        // Calculate section statistics
        const sectCompleted = sections.filter(section => 
          section.status === 3 || section.status === 7).length; // Completed or PassedInspection
        const sectInProgress = sections.filter(section => 
          section.status === 2).length; // InProgress
        const sectNeedsInspection = sections.filter(section => 
          section.status === 6).length; // NeedsInspection
        const sectFailed = sections.filter(section => 
          section.status === 4 || section.status === 8).length; // Denied or FailedInspection
        
        setSectionStats({
          completed: sectCompleted,
          inProgress: sectInProgress,
          needsInspection: sectNeedsInspection,
          failed: sectFailed
        });
        
        // Find upcoming inspections
        const currentDate = new Date();
        const oneWeekLater = new Date();
        oneWeekLater.setDate(currentDate.getDate() + 7);
        
        const upcoming = sections
          .filter(section => {
            if (!section.inspectionDate) return false;
            const inspectionDate = new Date(section.inspectionDate);
            return inspectionDate >= currentDate && inspectionDate <= oneWeekLater;
          })
          .map(section => ({
            ...section,
            jobName: jobs.find(job => job.id === section.jobId)?.name || 'Unknown Job'
          }));
        
        setUpcomingInspections(upcoming);
        
        // Check clock in status
        try {
          const employee = JSON.parse(localStorage.getItem('user'))?.employeeId;
          if (employee) {
            const currentTimeEntry = await timeTrackingService.getCurrentTimeEntry(employee);
            setClockedInStatus(currentTimeEntry);
          }
        } catch (error) {
          console.error('Error checking clock status:', error);
        }
        
      } catch (error) {
        console.error('Error fetching dashboard data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>
      
      {clockedInStatus && (
        <Paper sx={{ p: 2, mb: 3, bgcolor: '#e8f5e9' }}>
          <Typography variant="h6">
            You are currently clocked in
            {clockedInStatus.jobId ? ` to job: ${clockedInStatus.job?.name || clockedInStatus.jobId}` : ''}
          </Typography>
          <Typography variant="body2">
            Clocked in at: {new Date(clockedInStatus.clockInTime).toLocaleString()}
          </Typography>
          <Button 
            variant="contained" 
            color="primary" 
            component={RouterLink} 
            to="/clockinout"
            sx={{ mt: 1 }}
          >
            Clock Out
          </Button>
        </Paper>
      )}
      
      <Grid container spacing={3}>
        {/* Summary Stats */}
        <Grid item xs={12} md={4}>
          <Paper elevation={2} sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h6" color="primary" gutterBottom>
              Jobs Overview
            </Typography>
            <List dense>
              <ListItem>
                <ListItemIcon>
                  <JobIcon color="primary" />
                </ListItemIcon>
                <ListItemText primary={`Active Jobs: ${jobStats.active}`} />
              </ListItem>
              <ListItem>
                <ListItemIcon>
                  <CheckCircleIcon style={{ color: 'green' }} />
                </ListItemIcon>
                <ListItemText primary={`Completed Jobs: ${jobStats.completed}`} />
              </ListItem>
              <ListItem>
                <ListItemIcon>
                  <WarningIcon color="error" />
                </ListItemIcon>
                <ListItemText primary={`Overdue Jobs: ${jobStats.overdue}`} />
              </ListItem>
            </List>
          </Paper>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Paper elevation={2} sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h6" color="primary" gutterBottom>
              Sections Status
            </Typography>
            <List dense>
              <ListItem>
                <ListItemIcon>
                  <CheckCircleIcon style={{ color: 'green' }} />
                </ListItemIcon>
                <ListItemText 
                  primary={`Completed: ${sectionStats.completed}`} 
                  secondary={`${Math.round(sectionStats.completed / 
                    (sectionStats.completed + sectionStats.inProgress + 
                      sectionStats.needsInspection + sectionStats.failed) * 100) || 0}%`} 
                />
              </ListItem>
              <ListItem>
                <ListItemIcon>
                  <ScheduleIcon color="primary" />
                </ListItemIcon>
                <ListItemText primary={`In Progress: ${sectionStats.inProgress}`} />
              </ListItem>
              <ListItem>
                <ListItemIcon>
                  <SectionIcon color="secondary" />
                </ListItemIcon>
                <ListItemText primary={`Needs Inspection: ${sectionStats.needsInspection}`} />
              </ListItem>
              <ListItem>
                <ListItemIcon>
                  <WarningIcon color="error" />
                </ListItemIcon>
                <ListItemText primary={`Failed/Denied: ${sectionStats.failed}`} />
              </ListItem>
            </List>
          </Paper>
        </Grid>
        
        <Grid item xs={12} md={4}>
          <Paper elevation={2} sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h6" color="primary" gutterBottom>
              Quick Actions
            </Typography>
            <Button 
              variant="contained" 
              color="primary" 
              component={RouterLink} 
              to="/clockinout"
              fullWidth
              sx={{ mb: 1 }}
            >
              Clock In/Out
            </Button>
            <Button 
              variant="contained" 
              color="primary" 
              component={RouterLink} 
              to="/jobs/new"
              fullWidth
              sx={{ mb: 1 }}
            >
              Create New Job
            </Button>
            <Button 
              variant="contained" 
              color="primary" 
              component={RouterLink} 
              to="/aiassistant"
              fullWidth
              sx={{ mb: 1 }}
            >
              AI Assistant
            </Button>
            <Button 
              variant="contained" 
              color="primary" 
              component={RouterLink} 
              to="/buildingcode"
              fullWidth
            >
              Building Code Reference
            </Button>
          </Paper>
        </Grid>
        
        {/* Recent Jobs */}
        <Grid item xs={12} md={6}>
          <Paper elevation={2} sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h6" color="primary" gutterBottom>
              Recent Jobs
            </Typography>
            {recentJobs.length > 0 ? (
              <List>
                {recentJobs.map((job) => (
                  <React.Fragment key={job.id}>
                    <ListItem>
                      <ListItemIcon>
                        <JobIcon />
                      </ListItemIcon>
                      <ListItemText 
                        primary={job.name} 
                        secondary={`Status: ${job.status} | Location: ${job.location}`} 
                      />
                      <Button 
                        variant="outlined" 
                        size="small" 
                        component={RouterLink} 
                        to={`/jobs/${job.id}`}
                      >
                        View
                      </Button>
                    </ListItem>
                    <Divider />
                  </React.Fragment>
                ))}
              </List>
            ) : (
              <Typography variant="body2" color="textSecondary">
                No jobs found.
              </Typography>
            )}
            <Button 
              color="primary" 
              component={RouterLink} 
              to="/jobs"
              sx={{ alignSelf: 'flex-end', mt: 1 }}
            >
              View All Jobs
            </Button>
          </Paper>
        </Grid>
        
        {/* Upcoming Inspections */}
        <Grid item xs={12} md={6}>
          <Paper elevation={2} sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h6" color="primary" gutterBottom>
              Upcoming Inspections (7 Days)
            </Typography>
            {upcomingInspections.length > 0 ? (
              <List>
                {upcomingInspections.map((section) => (
                  <React.Fragment key={section.id}>
                    <ListItem>
                      <ListItemIcon>
                        <SectionIcon />
                      </ListItemIcon>
                      <ListItemText 
                        primary={`${section.sectionType.getDisplayName()} - ${section.jobName}`} 
                        secondary={`Inspection Date: ${new Date(section.inspectionDate).toLocaleDateString()}`} 
                      />
                      <Button 
                        variant="outlined" 
                        size="small" 
                        component={RouterLink} 
                        to={`/sections/${section.id}`}
                      >
                        View
                      </Button>
                    </ListItem>
                    <Divider />
                  </React.Fragment>
                ))}
              </List>
            ) : (
              <Typography variant="body2" color="textSecondary">
                No upcoming inspections found.
              </Typography>
            )}
            <Button 
              color="primary" 
              component={RouterLink} 
              to="/sections"
              sx={{ alignSelf: 'flex-end', mt: 1 }}
            >
              View All Sections
            </Button>
          </Paper>
        </Grid>
      </Grid>
    </Container>
  );
}

export default Dashboard;
