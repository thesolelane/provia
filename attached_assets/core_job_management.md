# Core Job Management Implementation

## Overview

This document outlines the implementation plan for the core job management functionality of the Job Tracker application. This functionality will serve as the foundation for the entire system, allowing users to create, view, edit, and manage jobs and their associated sections.

## Components

### 1. Backend API Components

#### Job Controller

```csharp
// JobController.cs
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    
    public JobController(IJobService jobService)
    {
        _jobService = jobService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobs([FromQuery] JobFilterDto filter)
    {
        var jobs = await _jobService.GetJobsAsync(filter);
        return Ok(jobs);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDetailDto>> GetJob(int id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
            return NotFound();
            
        return Ok(job);
    }
    
    [HttpPost]
    public async Task<ActionResult<JobDetailDto>> CreateJob(JobCreateDto jobDto)
    {
        var createdJob = await _jobService.CreateJobAsync(jobDto);
        return CreatedAtAction(nameof(GetJob), new { id = createdJob.JobID }, createdJob);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(int id, JobUpdateDto jobDto)
    {
        if (id != jobDto.JobID)
            return BadRequest();
            
        var success = await _jobService.UpdateJobAsync(jobDto);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var success = await _jobService.DeleteJobAsync(id);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
}
```

#### Job Section Controller

```csharp
// JobSectionController.cs
[Authorize]
[ApiController]
[Route("api/jobs/{jobId}/sections")]
public class JobSectionController : ControllerBase
{
    private readonly IJobSectionService _sectionService;
    
    public JobSectionController(IJobSectionService sectionService)
    {
        _sectionService = sectionService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobSectionDto>>> GetSections(int jobId)
    {
        var sections = await _sectionService.GetSectionsByJobIdAsync(jobId);
        return Ok(sections);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<JobSectionDetailDto>> GetSection(int jobId, int id)
    {
        var section = await _sectionService.GetSectionByIdAsync(id);
        if (section == null || section.JobID != jobId)
            return NotFound();
            
        return Ok(section);
    }
    
    [HttpPost]
    public async Task<ActionResult<JobSectionDetailDto>> CreateSection(int jobId, JobSectionCreateDto sectionDto)
    {
        if (jobId != sectionDto.JobID)
            return BadRequest();
            
        var createdSection = await _sectionService.CreateSectionAsync(sectionDto);
        return CreatedAtAction(nameof(GetSection), new { jobId, id = createdSection.SectionID }, createdSection);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSection(int jobId, int id, JobSectionUpdateDto sectionDto)
    {
        if (id != sectionDto.SectionID || jobId != sectionDto.JobID)
            return BadRequest();
            
        var success = await _sectionService.UpdateSectionAsync(sectionDto);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSection(int jobId, int id)
    {
        var success = await _sectionService.DeleteSectionAsync(id);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateSectionStatus(int jobId, int id, [FromBody] StatusUpdateDto statusDto)
    {
        var success = await _sectionService.UpdateSectionStatusAsync(id, statusDto.Status);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
    
    [HttpPut("{id}/subcontractor")]
    public async Task<IActionResult> UpdateSectionSubcontractor(int jobId, int id, [FromBody] SubcontractorAssignmentDto assignmentDto)
    {
        var success = await _sectionService.UpdateSectionSubcontractorAsync(id, assignmentDto);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
}
```

#### Customer Controller

```csharp
// CustomerController.cs
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    
    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        var customers = await _customerService.GetCustomersAsync();
        return Ok(customers);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomer(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound();
            
        return Ok(customer);
    }
    
    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> CreateCustomer(CustomerCreateDto customerDto)
    {
        var createdCustomer = await _customerService.CreateCustomerAsync(customerDto);
        return CreatedAtAction(nameof(GetCustomer), new { id = createdCustomer.CustomerID }, createdCustomer);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, CustomerUpdateDto customerDto)
    {
        if (id != customerDto.CustomerID)
            return BadRequest();
            
        var success = await _customerService.UpdateCustomerAsync(customerDto);
        if (!success)
            return NotFound();
            
        return NoContent();
    }
}
```

### 2. Service Layer

#### Job Service

```csharp
// IJobService.cs
public interface IJobService
{
    Task<IEnumerable<JobDto>> GetJobsAsync(JobFilterDto filter);
    Task<JobDetailDto> GetJobByIdAsync(int id);
    Task<JobDetailDto> CreateJobAsync(JobCreateDto jobDto);
    Task<bool> UpdateJobAsync(JobUpdateDto jobDto);
    Task<bool> DeleteJobAsync(int id);
}

// JobService.cs
public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    
    public JobService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<JobDto>> GetJobsAsync(JobFilterDto filter)
    {
        var query = _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.ProjectManager)
            .AsQueryable();
            
        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(j => j.Status == filter.Status);
            
        if (filter.ProjectManagerId.HasValue)
            query = query.Where(j => j.ProjectManagerID == filter.ProjectManagerId);
            
        // Apply sorting
        query = filter.SortBy switch
        {
            "jobNumber" => filter.SortDirection == "desc" ? query.OrderByDescending(j => j.JobNumber) : query.OrderBy(j => j.JobNumber),
            "startDate" => filter.SortDirection == "desc" ? query.OrderByDescending(j => j.StartDate) : query.OrderBy(j => j.StartDate),
            "targetDate" => filter.SortDirection == "desc" ? query.OrderByDescending(j => j.TargetCompletionDate) : query.OrderBy(j => j.TargetCompletionDate),
            _ => filter.SortDirection == "desc" ? query.OrderByDescending(j => j.CreatedDate) : query.OrderBy(j => j.CreatedDate)
        };
        
        // Apply pagination
        var jobs = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
            
        return _mapper.Map<IEnumerable<JobDto>>(jobs);
    }
    
    public async Task<JobDetailDto> GetJobByIdAsync(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.ProjectManager)
            .Include(j => j.JobSections)
            .FirstOrDefaultAsync(j => j.JobID == id);
            
        return _mapper.Map<JobDetailDto>(job);
    }
    
    public async Task<JobDetailDto> CreateJobAsync(JobCreateDto jobDto)
    {
        var job = _mapper.Map<Job>(jobDto);
        job.CreatedDate = DateTime.UtcNow;
        job.ModifiedDate = DateTime.UtcNow;
        
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        
        return await GetJobByIdAsync(job.JobID);
    }
    
    public async Task<bool> UpdateJobAsync(JobUpdateDto jobDto)
    {
        var job = await _context.Jobs.FindAsync(jobDto.JobID);
        if (job == null)
            return false;
            
        _mapper.Map(jobDto, job);
        job.ModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteJobAsync(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
            return false;
            
        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }
}
```

#### Job Section Service

```csharp
// IJobSectionService.cs
public interface IJobSectionService
{
    Task<IEnumerable<JobSectionDto>> GetSectionsByJobIdAsync(int jobId);
    Task<JobSectionDetailDto> GetSectionByIdAsync(int id);
    Task<JobSectionDetailDto> CreateSectionAsync(JobSectionCreateDto sectionDto);
    Task<bool> UpdateSectionAsync(JobSectionUpdateDto sectionDto);
    Task<bool> DeleteSectionAsync(int id);
    Task<bool> UpdateSectionStatusAsync(int id, string status);
    Task<bool> UpdateSectionSubcontractorAsync(int id, SubcontractorAssignmentDto assignmentDto);
}

// JobSectionService.cs
public class JobSectionService : IJobSectionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    
    public JobSectionService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<JobSectionDto>> GetSectionsByJobIdAsync(int jobId)
    {
        var sections = await _context.JobSections
            .Include(s => s.ResponsibleParty)
            .Include(s => s.Subcontractor)
            .Where(s => s.JobID == jobId)
            .ToListAsync();
            
        return _mapper.Map<IEnumerable<JobSectionDto>>(sections);
    }
    
    public async Task<JobSectionDetailDto> GetSectionByIdAsync(int id)
    {
        var section = await _context.JobSections
            .Include(s => s.ResponsibleParty)
            .Include(s => s.Subcontractor)
            .Include(s => s.SectionSubsections)
            .FirstOrDefaultAsync(s => s.SectionID == id);
            
        return _mapper.Map<JobSectionDetailDto>(section);
    }
    
    public async Task<JobSectionDetailDto> CreateSectionAsync(JobSectionCreateDto sectionDto)
    {
        var section = _mapper.Map<JobSection>(sectionDto);
        section.CreatedDate = DateTime.UtcNow;
        section.ModifiedDate = DateTime.UtcNow;
        
        _context.JobSections.Add(section);
        await _context.SaveChangesAsync();
        
        return await GetSectionByIdAsync(section.SectionID);
    }
    
    public async Task<bool> UpdateSectionAsync(JobSectionUpdateDto sectionDto)
    {
        var section = await _context.JobSections.FindAsync(sectionDto.SectionID);
        if (section == null)
            return false;
            
        _mapper.Map(sectionDto, section);
        section.ModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteSectionAsync(int id)
    {
        var section = await _context.JobSections.FindAsync(id);
        if (section == null)
            return false;
            
        _context.JobSections.Remove(section);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> UpdateSectionStatusAsync(int id, string status)
    {
        var section = await _context.JobSections.FindAsync(id);
        if (section == null)
            return false;
            
        section.Status = status;
        section.ModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> UpdateSectionSubcontractorAsync(int id, SubcontractorAssignmentDto assignmentDto)
    {
        var section = await _context.JobSections.FindAsync(id);
        if (section == null)
            return false;
            
        section.IsSubcontracted = assignmentDto.IsSubcontracted;
        section.SubcontractorID = assignmentDto.IsSubcontracted ? assignmentDto.SubcontractorID : null;
        section.ModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
}
```

### 3. Data Models

#### DTOs (Data Transfer Objects)

```csharp
// JobDto.cs
public class JobDto
{
    public int JobID { get; set; }
    public string JobNumber { get; set; }
    public string JobName { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string CustomerName { get; set; }
    public string ProjectManagerName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
}

// JobDetailDto.cs
public class JobDetailDto : JobDto
{
    public string Description { get; set; }
    public CustomerDto Customer { get; set; }
    public UserDto ProjectManager { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public IEnumerable<JobSectionDto> Sections { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

// JobCreateDto.cs
public class JobCreateDto
{
    [Required]
    [StringLength(20)]
    public string JobNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public string JobName { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Address { get; set; }
    
    [Required]
    [StringLength(50)]
    public string City { get; set; }
    
    [Required]
    [StringLength(2)]
    public string State { get; set; }
    
    [Required]
    [StringLength(10)]
    public string ZipCode { get; set; }
    
    public int? CustomerID { get; set; }
    public int? ProjectManagerID { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; }
    
    [StringLength(20)]
    public string Priority { get; set; }
    
    public string Description { get; set; }
}

// JobUpdateDto.cs
public class JobUpdateDto
{
    public int JobID { get; set; }
    
    [StringLength(100)]
    public string JobName { get; set; }
    
    [StringLength(200)]
    public string Address { get; set; }
    
    [StringLength(50)]
    public string City { get; set; }
    
    [StringLength(2)]
    public string State { get; set; }
    
    [StringLength(10)]
    public string ZipCode { get; set; }
    
    public int? CustomerID { get; set; }
    public int? ProjectManagerID { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    
    [StringLength(20)]
    public string Status { get; set; }
    
    [StringLength(20)]
    public string Priority { get; set; }
    
    public string Description { get; set; }
}

// JobFilterDto.cs
public class JobFilterDto
{
    public string Status { get; set; }
    public int? ProjectManagerId { get; set; }
    public string SortBy { get; set; } = "createdDate";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// JobSectionDto.cs
public class JobSectionDto
{
    public int SectionID { get; set; }
    public int JobID { get; set; }
    public string SectionType { get; set; }
    public string SectionName { get; set; }
    public string Status { get; set; }
    public bool IsSubcontracted { get; set; }
    public string SubcontractorName { get; set; }
    public string ResponsiblePartyName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public DateTime? InspectionDate { get; set; }
    public string InspectionResult { get; set; }
}

// JobSectionDetailDto.cs
public class JobSectionDetailDto : JobSectionDto
{
    public string Description { get; set; }
    public SubcontractorDto Subcontractor { get; set; }
    public UserDto ResponsibleParty { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public DateTime? ReinspectionDate { get; set; }
    public string Notes { get; set; }
    public IEnumerable<SectionSubsectionDto> Subsections { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

// SubcontractorAssignmentDto.cs
public class SubcontractorAssignmentDto
{
    public bool IsSubcontracted { get; set; }
    public int? SubcontractorID { get; set; }
}

// StatusUpdateDto.cs
public class StatusUpdateDto
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; }
}
```

### 4. Frontend Components

#### Job Dashboard Component

```jsx
// JobDashboard.jsx
import React, { useState, useEffect } from 'react';
import { 
  Container, Typography, Grid, Paper, Button, 
  Table, TableBody, TableCell, TableContainer, 
  TableHead, TableRow, TablePagination, TextField,
  MenuItem, Select, FormControl, InputLabel
} from '@material-ui/core';
import { Add as AddIcon, FilterList as FilterIcon } from '@material-ui/icons';
import { Link } from 'react-router-dom';
import { jobService } from '../services';

const JobDashboard = () => {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState({
    status: '',
    projectManagerId: '',
    sortBy: 'createdDate',
    sortDirection: 'desc',
    page: 0,
    pageSize: 10
  });
  const [totalJobs, setTotalJobs] = useState(0);
  
  useEffect(() => {
    loadJobs();
  }, [filter]);
  
  const loadJobs = async () => {
    setLoading(true);
    try {
      const data = await jobService.getJobs({
        ...filter,
        page: filter.page + 1 // API uses 1-based indexing
      });
      setJobs(data.items);
      setTotalJobs(data.totalCount);
    } catch (error) {
      console.error('Error loading jobs:', error);
    } finally {
      setLoading(false);
    }
  };
  
  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilter({
      ...filter,
      [name]: value,
      page: 0 // Reset to first page when filter changes
    });
  };
  
  const handlePageChange = (event, newPage) => {
    setFilter({
      ...filter,
      page: newPage
    });
  };
  
  const handlePageSizeChange = (event) => {
    setFilter({
      ...filter,
      pageSize: parseInt(event.target.value, 10),
      page: 0
    });
  };
  
  return (
    <Container maxWidth="lg">
      <Grid container spacing={3} alignItems="center" style={{ marginBottom: 20 }}>
        <Grid item xs>
          <Typography variant="h4">Jobs</Typography>
        </Grid>
        <Grid item>
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            component={Link}
            to="/jobs/new"
          >
            New Job
          </Button>
        </Grid>
      </Grid>
      
      <Paper style={{ marginBottom: 20, padding: 16 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth variant="outlined" size="small">
              <InputLabel>Status</InputLabel>
              <Select
                name="status"
                value={filter.status}
                onChange={handleFilterChange}
                label="Status"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="New">New</MenuItem>
                <MenuItem value="In Progress">In Progress</MenuItem>
                <MenuItem value="On Hold">On Hold</MenuItem>
                <MenuItem value="Completed">Completed</MenuItem>
                <MenuItem value="Cancelled">Cancelled</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth variant="outlined" size="small">
              <InputLabel>Sort By</InputLabel>
              <Select
                name="sortBy"
                value={filter.sortBy}
                onChange={handleFilterChange}
                label="Sort By"
              >
                <MenuItem value="jobNumber">Job Number</MenuItem>
                <MenuItem value="jobName">Job Name</MenuItem>
                <MenuItem value="startDate">Start Date</MenuItem>
                <MenuItem value="targetDate">Target Date</MenuItem>
                <MenuItem value="createdDate">Created Date</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth variant="outlined" size="small">
              <InputLabel>Sort Direction</InputLabel>
              <Select
                name="sortDirection"
                value={filter.sortDirection}
                onChange={handleFilterChange}
                label="Sort Direction"
              >
                <MenuItem value="asc">Ascending</MenuItem>
                <MenuItem value="desc">Descending</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<FilterIcon />}
              onClick={() => setFilter({
                status: '',
                projectManagerId: '',
                sortBy: 'createdDate',
                sortDirection: 'desc',
                page: 0,
                pageSize: 10
              })}
            >
              Reset Filters
            </Button>
          </Grid>
        </Grid>
      </Paper>
      
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Job Number</TableCell>
              <TableCell>Job Name</TableCell>
              <TableCell>Customer</TableCell>
              <TableCell>Address</TableCell>
              <TableCell>Project Manager</TableCell>
              <TableCell>Start Date</TableCell>
              <TableCell>Target Date</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Priority</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={9} align="center">Loading...</TableCell>
              </TableRow>
            ) : jobs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={9} align="center">No jobs found</TableCell>
              </TableRow>
            ) : (
              jobs.map((job) => (
                <TableRow key={job.jobID} hover component={Link} to={`/jobs/${job.jobID}`} style={{ textDecoration: 'none' }}>
                  <TableCell>{job.jobNumber}</TableCell>
                  <TableCell>{job.jobName}</TableCell>
                  <TableCell>{job.customerName}</TableCell>
                  <TableCell>{`${job.address}, ${job.city}, ${job.state}`}</TableCell>
                  <TableCell>{job.projectManagerName}</TableCell>
                  <TableCell>{job.startDate ? new Date(job.startDate).toLocaleDateString() : 'N/A'}</TableCell>
                  <TableCell>{job.targetCompletionDate ? new Date(job.targetCompletionDate).toLocaleDateString() : 'N/A'}</TableCell>
                  <TableCell>{job.status}</TableCell>
                  <TableCell>{job.priority || 'Normal'}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={totalJobs}
          rowsPerPage={filter.pageSize}
          page={filter.page}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handlePageSizeChange}
        />
      </TableContainer>
    </Container>
  );
};

export default JobDashboard;
```

#### Job Detail Component

```jsx
// JobDetail.jsx
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  Container, Typography, Grid, Paper, Button, Tabs, Tab,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Chip, Divider, Box, CircularProgress, Dialog, DialogActions,
  DialogContent, DialogContentText, DialogTitle
} from '@material-ui/core';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Add as AddIcon
} from '@material-ui/icons';
import { jobService, sectionService } from '../services';

const JobDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [job, setJob] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(0);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  
  useEffect(() => {
    loadJob();
  }, [id]);
  
  const loadJob = async () => {
    setLoading(true);
    try {
      const data = await jobService.getJobById(id);
      setJob(data);
    } catch (error) {
      console.error('Error loading job:', error);
    } finally {
      setLoading(false);
    }
  };
  
  const handleDeleteJob = async () => {
    try {
      await jobService.deleteJob(id);
      navigate('/jobs');
    } catch (error) {
      console.error('Error deleting job:', error);
    }
  };
  
  const handleTabChange = (event, newValue) => {
    setActiveTab(newValue);
  };
  
  if (loading) {
    return (
      <Container maxWidth="lg" style={{ textAlign: 'center', padding: 50 }}>
        <CircularProgress />
      </Container>
    );
  }
  
  if (!job) {
    return (
      <Container maxWidth="lg">
        <Typography variant="h5">Job not found</Typography>
        <Button component={Link} to="/jobs">Back to Jobs</Button>
      </Container>
    );
  }
  
  return (
    <Container maxWidth="lg">
      <Grid container spacing={3} alignItems="center" style={{ marginBottom: 20 }}>
        <Grid item xs>
          <Typography variant="h4">{job.jobName}</Typography>
          <Typography variant="subtitle1" color="textSecondary">
            Job #{job.jobNumber}
          </Typography>
        </Grid>
        <Grid item>
          <Button
            variant="outlined"
            color="primary"
            startIcon={<EditIcon />}
            component={Link}
            to={`/jobs/${id}/edit`}
            style={{ marginRight: 8 }}
          >
            Edit
          </Button>
          <Button
            variant="outlined"
            color="secondary"
            startIcon={<DeleteIcon />}
            onClick={() => setDeleteDialogOpen(true)}
          >
            Delete
          </Button>
        </Grid>
      </Grid>
      
      <Paper style={{ marginBottom: 20 }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          indicatorColor="primary"
          textColor="primary"
          variant="fullWidth"
        >
          <Tab label="Overview" />
          <Tab label="Sections" />
          <Tab label="Documents" />
          <Tab label="Timeline" />
        </Tabs>
      </Paper>
      
      {activeTab === 0 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Paper style={{ padding: 16 }}>
              <Typography variant="h6" gutterBottom>Job Details</Typography>
              <Divider style={{ marginBottom: 16 }} />
              <Grid container spacing={2}>
                <Grid item xs={4}>
                  <Typography variant="body2" color="textSecondary">Status</Typography>
                  <Chip label={job.status} color={job.status === 'Completed' ? 'primary' : 'default'} size="small" />
                </Grid>
                <Grid item xs={4}>
                  <Typography variant="body2" color="textSecondary">Priority</Typography>
                  <Typography>{job.priority || 'Normal'}</Typography>
                </Grid>
                <Grid item xs={4}>
                  <Typography variant="body2" color="textSecondary">Project Manager</Typography>
                  <Typography>{job.projectManager?.name || 'Unassigned'}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" color="textSecondary">Start Date</Typography>
                  <Typography>{job.startDate ? new Date(job.startDate).toLocaleDateString() : 'Not set'}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" color="textSecondary">Target Completion</Typography>
                  <Typography>{job.targetCompletionDate ? new Date(job.targetCompletionDate).toLocaleDateString() : 'Not set'}</Typography>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" color="textSecondary">Description</Typography>
                  <Typography>{job.description || 'No description provided'}</Typography>
                </Grid>
              </Grid>
            </Paper>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Paper style={{ padding: 16 }}>
              <Typography variant="h6" gutterBottom>Customer Information</Typography>
              <Divider style={{ marginBottom: 16 }} />
              {job.customer ? (
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Typography variant="body2" color="textSecondary">Name</Typography>
                    <Typography>{job.customer.name}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">Contact Person</Typography>
                    <Typography>{job.customer.contactPerson || 'N/A'}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">Phone</Typography>
                    <Typography>{job.customer.phone || 'N/A'}</Typography>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="body2" color="textSecondary">Email</Typography>
                    <Typography>{job.customer.email || 'N/A'}</Typography>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="body2" color="textSecondary">Address</Typography>
                    <Typography>
                      {job.customer.address ? 
                        `${job.customer.address}, ${job.customer.city}, ${job.customer.state} ${job.customer.zipCode}` : 
                        'N/A'}
                    </Typography>
                  </Grid>
                </Grid>
              ) : (
                <Typography>No customer assigned</Typography>
              )}
            </Paper>
          </Grid>
          
          <Grid item xs={12}>
            <Paper style={{ padding: 16 }}>
              <Typography variant="h6" gutterBottom>Job Location</Typography>
              <Divider style={{ marginBottom: 16 }} />
              <Typography>
                {`${job.address}, ${job.city}, ${job.state} ${job.zipCode}`}
              </Typography>
              {/* Map component could be added here */}
            </Paper>
          </Grid>
        </Grid>
      )}
      
      {activeTab === 1 && (
        <Paper>
          <Box display="flex" justifyContent="space-between" alignItems="center" padding={2}>
            <Typography variant="h6">Job Sections</Typography>
            <Button
              variant="contained"
              color="primary"
              startIcon={<AddIcon />}
              component={Link}
              to={`/jobs/${id}/sections/new`}
            >
              Add Section
            </Button>
          </Box>
          <Divider />
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Section</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Responsible Party</TableCell>
                  <TableCell>Subcontracted</TableCell>
                  <TableCell>Start Date</TableCell>
                  <TableCell>Target Date</TableCell>
                  <TableCell>Inspection Date</TableCell>
                  <TableCell>Inspection Result</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {job.sections && job.sections.length > 0 ? (
                  job.sections.map((section) => (
                    <TableRow 
                      key={section.sectionID} 
                      hover 
                      component={Link} 
                      to={`/jobs/${id}/sections/${section.sectionID}`}
                      style={{ textDecoration: 'none' }}
                    >
                      <TableCell>
                        <Typography variant="body1">{section.sectionName}</Typography>
                        <Typography variant="body2" color="textSecondary">{section.sectionType}</Typography>
                      </TableCell>
                      <TableCell>
                        <Chip 
                          label={section.status} 
                          color={
                            section.status === 'Completed' ? 'primary' : 
                            section.status === 'In Progress' ? 'secondary' : 
                            'default'
                          } 
                          size="small" 
                        />
                      </TableCell>
                      <TableCell>{section.responsiblePartyName || 'Unassigned'}</TableCell>
                      <TableCell>
                        {section.isSubcontracted ? (
                          <Chip label={section.subcontractorName || 'Yes'} size="small" color="secondary" />
                        ) : 'No'}
                      </TableCell>
                      <TableCell>{section.startDate ? new Date(section.startDate).toLocaleDateString() : 'Not set'}</TableCell>
                      <TableCell>{section.targetCompletionDate ? new Date(section.targetCompletionDate).toLocaleDateString() : 'Not set'}</TableCell>
                      <TableCell>{section.inspectionDate ? new Date(section.inspectionDate).toLocaleDateString() : 'Not scheduled'}</TableCell>
                      <TableCell>
                        {section.inspectionResult ? (
                          <Chip 
                            label={section.inspectionResult} 
                            color={
                              section.inspectionResult === 'Pass' ? 'primary' : 
                              section.inspectionResult === 'Fail' ? 'secondary' : 
                              'default'
                            } 
                            size="small" 
                          />
                        ) : 'N/A'}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={8} align="center">No sections added yet</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}
      
      {activeTab === 2 && (
        <Paper style={{ padding: 16, minHeight: 300 }}>
          <Typography variant="h6">Documents</Typography>
          <Divider style={{ margin: '16px 0' }} />
          <Typography>Document management will be implemented in a future sprint.</Typography>
        </Paper>
      )}
      
      {activeTab === 3 && (
        <Paper style={{ padding: 16, minHeight: 300 }}>
          <Typography variant="h6">Timeline</Typography>
          <Divider style={{ margin: '16px 0' }} />
          <Typography>Timeline tracking will be implemented in a future sprint.</Typography>
        </Paper>
      )}
      
      <Dialog
        open={deleteDialogOpen}
        onClose={() => setDeleteDialogOpen(false)}
      >
        <DialogTitle>Delete Job</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete this job? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)} color="primary">
            Cancel
          </Button>
          <Button onClick={handleDeleteJob} color="secondary">
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default JobDetail;
```

#### Job Form Component

```jsx
// JobForm.jsx
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Container, Typography, Grid, Paper, Button, TextField,
  FormControl, InputLabel, Select, MenuItem, CircularProgress
} from '@material-ui/core';
import { jobService, customerService, userService } from '../services';

const JobForm = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEditMode = !!id;
  
  const [formData, setFormData] = useState({
    jobNumber: '',
    jobName: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
    customerID: '',
    projectManagerID: '',
    startDate: '',
    targetCompletionDate: '',
    status: 'New',
    priority: 'Normal',
    description: ''
  });
  
  const [customers, setCustomers] = useState([]);
  const [projectManagers, setProjectManagers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(isEditMode);
  const [errors, setErrors] = useState({});
  
  useEffect(() => {
    loadFormDependencies();
    
    if (isEditMode) {
      loadJob();
    }
  }, [id]);
  
  const loadFormDependencies = async () => {
    try {
      const [customersData, usersData] = await Promise.all([
        customerService.getCustomers(),
        userService.getUsers()
      ]);
      
      setCustomers(customersData);
      setProjectManagers(usersData.filter(user => user.role === 'ProjectManager'));
    } catch (error) {
      console.error('Error loading form dependencies:', error);
    }
  };
  
  const loadJob = async () => {
    setInitialLoading(true);
    try {
      const job = await jobService.getJobById(id);
      
      setFormData({
        jobNumber: job.jobNumber,
        jobName: job.jobName,
        address: job.address,
        city: job.city,
        state: job.state,
        zipCode: job.zipCode,
        customerID: job.customer?.customerID || '',
        projectManagerID: job.projectManager?.userID || '',
        startDate: job.startDate ? job.startDate.split('T')[0] : '',
        targetCompletionDate: job.targetCompletionDate ? job.targetCompletionDate.split('T')[0] : '',
        status: job.status,
        priority: job.priority || 'Normal',
        description: job.description || ''
      });
    } catch (error) {
      console.error('Error loading job:', error);
    } finally {
      setInitialLoading(false);
    }
  };
  
  const handleChange = (event) => {
    const { name, value } = event.target;
    setFormData({
      ...formData,
      [name]: value
    });
    
    // Clear error when field is edited
    if (errors[name]) {
      setErrors({
        ...errors,
        [name]: null
      });
    }
  };
  
  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.jobNumber) newErrors.jobNumber = 'Job number is required';
    if (!formData.jobName) newErrors.jobName = 'Job name is required';
    if (!formData.address) newErrors.address = 'Address is required';
    if (!formData.city) newErrors.city = 'City is required';
    if (!formData.state) newErrors.state = 'State is required';
    if (!formData.zipCode) newErrors.zipCode = 'ZIP code is required';
    if (!formData.status) newErrors.status = 'Status is required';
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };
  
  const handleSubmit = async (event) => {
    event.preventDefault();
    
    if (!validateForm()) {
      return;
    }
    
    setLoading(true);
    try {
      if (isEditMode) {
        await jobService.updateJob(id, {
          ...formData,
          jobID: parseInt(id)
        });
      } else {
        const newJob = await jobService.createJob(formData);
        id = newJob.jobID;
      }
      
      navigate(`/jobs/${id}`);
    } catch (error) {
      console.error('Error saving job:', error);
      setErrors({
        ...errors,
        form: 'An error occurred while saving the job. Please try again.'
      });
    } finally {
      setLoading(false);
    }
  };
  
  if (initialLoading) {
    return (
      <Container maxWidth="lg" style={{ textAlign: 'center', padding: 50 }}>
        <CircularProgress />
      </Container>
    );
  }
  
  return (
    <Container maxWidth="lg">
      <Typography variant="h4" gutterBottom>
        {isEditMode ? 'Edit Job' : 'New Job'}
      </Typography>
      
      <form onSubmit={handleSubmit}>
        <Paper style={{ padding: 16 }}>
          <Grid container spacing={3}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Job Number"
                name="jobNumber"
                value={formData.jobNumber}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.jobNumber}
                helperText={errors.jobNumber}
                disabled={isEditMode} // Job number shouldn't be editable after creation
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Job Name"
                name="jobName"
                value={formData.jobName}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.jobName}
                helperText={errors.jobName}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Address"
                name="address"
                value={formData.address}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.address}
                helperText={errors.address}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={4}>
              <TextField
                fullWidth
                label="City"
                name="city"
                value={formData.city}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.city}
                helperText={errors.city}
              />
            </Grid>
            <Grid item xs={12} sm={3} md={4}>
              <TextField
                fullWidth
                label="State"
                name="state"
                value={formData.state}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.state}
                helperText={errors.state}
              />
            </Grid>
            <Grid item xs={12} sm={3} md={4}>
              <TextField
                fullWidth
                label="ZIP Code"
                name="zipCode"
                value={formData.zipCode}
                onChange={handleChange}
                variant="outlined"
                required
                error={!!errors.zipCode}
                helperText={errors.zipCode}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth variant="outlined">
                <InputLabel>Customer</InputLabel>
                <Select
                  name="customerID"
                  value={formData.customerID}
                  onChange={handleChange}
                  label="Customer"
                >
                  <MenuItem value="">
                    <em>None</em>
                  </MenuItem>
                  {customers.map(customer => (
                    <MenuItem key={customer.customerID} value={customer.customerID}>
                      {customer.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth variant="outlined">
                <InputLabel>Project Manager</InputLabel>
                <Select
                  name="projectManagerID"
                  value={formData.projectManagerID}
                  onChange={handleChange}
                  label="Project Manager"
                >
                  <MenuItem value="">
                    <em>None</em>
                  </MenuItem>
                  {projectManagers.map(manager => (
                    <MenuItem key={manager.userID} value={manager.userID}>
                      {`${manager.firstName} ${manager.lastName}`}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Start Date"
                name="startDate"
                type="date"
                value={formData.startDate}
                onChange={handleChange}
                variant="outlined"
                InputLabelProps={{ shrink: true }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Target Completion Date"
                name="targetCompletionDate"
                type="date"
                value={formData.targetCompletionDate}
                onChange={handleChange}
                variant="outlined"
                InputLabelProps={{ shrink: true }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth variant="outlined" required error={!!errors.status}>
                <InputLabel>Status</InputLabel>
                <Select
                  name="status"
                  value={formData.status}
                  onChange={handleChange}
                  label="Status"
                >
                  <MenuItem value="New">New</MenuItem>
                  <MenuItem value="In Progress">In Progress</MenuItem>
                  <MenuItem value="On Hold">On Hold</MenuItem>
                  <MenuItem value="Completed">Completed</MenuItem>
                  <MenuItem value="Cancelled">Cancelled</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth variant="outlined">
                <InputLabel>Priority</InputLabel>
                <Select
                  name="priority"
                  value={formData.priority}
                  onChange={handleChange}
                  label="Priority"
                >
                  <MenuItem value="Low">Low</MenuItem>
                  <MenuItem value="Normal">Normal</MenuItem>
                  <MenuItem value="High">High</MenuItem>
                  <MenuItem value="Urgent">Urgent</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                name="description"
                value={formData.description}
                onChange={handleChange}
                variant="outlined"
                multiline
                rows={4}
              />
            </Grid>
            {errors.form && (
              <Grid item xs={12}>
                <Typography color="error">{errors.form}</Typography>
              </Grid>
            )}
            <Grid item xs={12}>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                disabled={loading}
                style={{ marginRight: 8 }}
              >
                {loading ? <CircularProgress size={24} /> : (isEditMode ? 'Update Job' : 'Create Job')}
              </Button>
              <Button
                variant="outlined"
                onClick={() => navigate(isEditMode ? `/jobs/${id}` : '/jobs')}
                disabled={loading}
              >
                Cancel
              </Button>
            </Grid>
          </Grid>
        </Paper>
      </form>
    </Container>
  );
};

export default JobForm;
```

### 5. API Services

#### Job Service

```javascript
// jobService.js
import axios from 'axios';

const API_URL = '/api/jobs';

export const jobService = {
  getJobs: async (filter) => {
    const response = await axios.get(API_URL, { params: filter });
    return response.data;
  },
  
  getJobById: async (id) => {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  },
  
  createJob: async (job) => {
    const response = await axios.post(API_URL, job);
    return response.data;
  },
  
  updateJob: async (id, job) => {
    const response = await axios.put(`${API_URL}/${id}`, job);
    return response.data;
  },
  
  deleteJob: async (id) => {
    const response = await axios.delete(`${API_URL}/${id}`);
    return response.data;
  }
};
```

#### Job Section Service

```javascript
// sectionService.js
import axios from 'axios';

export const sectionService = {
  getSectionsByJobId: async (jobId) => {
    const response = await axios.get(`/api/jobs/${jobId}/sections`);
    return response.data;
  },
  
  getSectionById: async (jobId, sectionId) => {
    const response = await axios.get(`/api/jobs/${jobId}/sections/${sectionId}`);
    return response.data;
  },
  
  createSection: async (jobId, section) => {
    const response = await axios.post(`/api/jobs/${jobId}/sections`, section);
    return response.data;
  },
  
  updateSection: async (jobId, sectionId, section) => {
    const response = await axios.put(`/api/jobs/${jobId}/sections/${sectionId}`, section);
    return response.data;
  },
  
  deleteSection: async (jobId, sectionId) => {
    const response = await axios.delete(`/api/jobs/${jobId}/sections/${sectionId}`);
    return response.data;
  },
  
  updateSectionStatus: async (jobId, sectionId, status) => {
    const response = await axios.put(`/api/jobs/${jobId}/sections/${sectionId}/status`, { status });
    return response.data;
  },
  
  updateSectionSubcontractor: async (jobId, sectionId, assignment) => {
    const response = await axios.put(`/api/jobs/${jobId}/sections/${sectionId}/subcontractor`, assignment);
    return response.data;
  }
};
```

## Implementation Plan

### Phase 1: Database Setup

1. Create the database schema using Entity Framework Core migrations
2. Set up seed data for testing
3. Implement data access layer with repositories

### Phase 2: Backend API Implementation

1. Create controllers for Jobs, JobSections, and Customers
2. Implement service layer with business logic
3. Set up API endpoints with proper routing and authentication placeholders

### Phase 3: Frontend Implementation

1. Create React components for job management
2. Implement API services for data fetching
3. Build UI for job listing, creation, and editing

### Phase 4: Integration and Testing

1. Connect frontend and backend components
2. Implement error handling and validation
3. Test all CRUD operations for jobs and sections

## Next Steps

After completing the core job management functionality, the following features will be implemented:

1. Active Directory authentication integration
2. Subcontractor management
3. Building code reference integration
4. AI assistant functionality
5. Microsoft Office and Google services integration

These features will build upon the core job management foundation established in this phase.
