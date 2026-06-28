import axios from 'axios';

const API_URL = '/api/Subcontractors';

export const subcontractorService = {
  getSubcontractors,
  getSubcontractorById,
  createSubcontractor,
  updateSubcontractor,
  deleteSubcontractor,
  getSubcontractorSections,
  createSectionAssignment,
  updateSectionAssignment,
  deleteSectionAssignment
};

async function getSubcontractors(active = null, search = null) {
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
    console.error('Error fetching subcontractors:', error);
    throw error;
  }
}

async function getSubcontractorById(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching subcontractor with ID ${id}:`, error);
    throw error;
  }
}

async function createSubcontractor(subcontractor) {
  try {
    const response = await axios.post(API_URL, subcontractor);
    return response.data;
  } catch (error) {
    console.error('Error creating subcontractor:', error);
    throw error;
  }
}

async function updateSubcontractor(id, subcontractor) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, subcontractor);
    return response.data;
  } catch (error) {
    console.error(`Error updating subcontractor with ID ${id}:`, error);
    throw error;
  }
}

async function deleteSubcontractor(id) {
  try {
    await axios.delete(`${API_URL}/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting subcontractor with ID ${id}:`, error);
    throw error;
  }
}

async function getSubcontractorSections(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}/sections`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching sections for subcontractor ID ${id}:`, error);
    throw error;
  }
}

async function createSectionAssignment(assignment) {
  try {
    const response = await axios.post(`${API_URL}/section-assignment`, assignment);
    return response.data;
  } catch (error) {
    console.error('Error creating section-subcontractor assignment:', error);
    throw error;
  }
}

async function updateSectionAssignment(id, assignment) {
  try {
    const response = await axios.put(`${API_URL}/section-assignment/${id}`, assignment);
    return response.data;
  } catch (error) {
    console.error(`Error updating section-subcontractor assignment ID ${id}:`, error);
    throw error;
  }
}

async function deleteSectionAssignment(id) {
  try {
    await axios.delete(`${API_URL}/section-assignment/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting section-subcontractor assignment ID ${id}:`, error);
    throw error;
  }
}
