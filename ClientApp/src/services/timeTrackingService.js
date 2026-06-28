import axios from 'axios';

const API_URL = '/api/TimeTracking';

export const timeTrackingService = {
  getTimeEntries,
  getTimeEntry,
  clockIn,
  clockOut,
  createManualTimeEntry,
  updateTimeEntry,
  deleteTimeEntry,
  getCurrentTimeEntry,
  getTimeSummary
};

async function getTimeEntries(params = {}) {
  try {
    const queryParams = new URLSearchParams();
    
    if (params.employeeId) queryParams.append('employeeId', params.employeeId);
    if (params.jobId) queryParams.append('jobId', params.jobId);
    if (params.startDate) queryParams.append('startDate', params.startDate.toISOString());
    if (params.endDate) queryParams.append('endDate', params.endDate.toISOString());
    
    const url = `${API_URL}${queryParams.toString() ? '?' + queryParams.toString() : ''}`;
    
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching time entries:', error);
    throw error;
  }
}

async function getTimeEntry(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching time entry with ID ${id}:`, error);
    throw error;
  }
}

async function clockIn(data) {
  try {
    const response = await axios.post(`${API_URL}/clock-in`, data);
    return response.data;
  } catch (error) {
    console.error('Error clocking in:', error);
    throw error;
  }
}

async function clockOut(data) {
  try {
    const response = await axios.post(`${API_URL}/clock-out`, data);
    return response.data;
  } catch (error) {
    console.error('Error clocking out:', error);
    throw error;
  }
}

async function createManualTimeEntry(timeEntry) {
  try {
    const response = await axios.post(API_URL, timeEntry);
    return response.data;
  } catch (error) {
    console.error('Error creating manual time entry:', error);
    throw error;
  }
}

async function updateTimeEntry(id, timeEntry) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, timeEntry);
    return response.data;
  } catch (error) {
    console.error(`Error updating time entry with ID ${id}:`, error);
    throw error;
  }
}

async function deleteTimeEntry(id) {
  try {
    await axios.delete(`${API_URL}/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting time entry with ID ${id}:`, error);
    throw error;
  }
}

async function getCurrentTimeEntry(employeeId) {
  try {
    const response = await axios.get(`${API_URL}/employee/${employeeId}/current`);
    return response.data;
  } catch (error) {
    if (error.response && error.response.status === 404) {
      // Not found is expected if not clocked in
      return null;
    }
    console.error(`Error checking current time entry for employee ${employeeId}:`, error);
    throw error;
  }
}

async function getTimeSummary(params = {}) {
  try {
    const queryParams = new URLSearchParams();
    
    if (params.employeeId) queryParams.append('employeeId', params.employeeId);
    if (params.jobId) queryParams.append('jobId', params.jobId);
    if (params.startDate) queryParams.append('startDate', params.startDate.toISOString());
    if (params.endDate) queryParams.append('endDate', params.endDate.toISOString());
    
    const url = `${API_URL}/summary${queryParams.toString() ? '?' + queryParams.toString() : ''}`;
    
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching time summary:', error);
    throw error;
  }
}
