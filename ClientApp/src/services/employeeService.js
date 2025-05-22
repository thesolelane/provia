import axios from 'axios';

const API_URL = '/api/Employees';

export const employeeService = {
  getEmployees,
  getEmployeeById,
  createEmployee,
  updateEmployee,
  deleteEmployee,
  getEmployeeAssignments
};

async function getEmployees(active = null, search = null) {
  try {
    let url = API_URL;
    const params = [];
    
    if (active !== null) params.push(`active=${active}`);
    if (search !== null) params.push(`search=${encodeURIComponent(search)}`);
    
    if (params.length > 0) {
      url += `?${params.join('&')}`;
    }
    
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching employees:', error);
    throw error;
  }
}

async function getEmployeeById(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching employee with ID ${id}:`, error);
    throw error;
  }
}

async function createEmployee(employee) {
  try {
    const response = await axios.post(API_URL, employee);
    return response.data;
  } catch (error) {
    console.error('Error creating employee:', error);
    throw error;
  }
}

async function updateEmployee(id, employee) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, employee);
    return response.data;
  } catch (error) {
    console.error(`Error updating employee with ID ${id}:`, error);
    throw error;
  }
}

async function deleteEmployee(id) {
  try {
    await axios.delete(`${API_URL}/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting employee with ID ${id}:`, error);
    throw error;
  }
}

async function getEmployeeAssignments(id, date = null) {
  try {
    let url = `${API_URL}/${id}/job-assignments`;
    
    if (date) {
      url += `?date=${date.toISOString().split('T')[0]}`;
    }
    
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error(`Error fetching assignments for employee ID ${id}:`, error);
    throw error;
  }
}
