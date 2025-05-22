import axios from 'axios';

const API_URL = '/api/Sections';

export const sectionService = {
  getAllSections,
  getSectionsByJobId,
  getSectionById,
  updateSection,
  toggleSubcontractor,
  scheduleInspection,
  recordInspection,
  getAIGuidance
};

async function getAllSections() {
  try {
    const response = await axios.get(API_URL);
    return response.data;
  } catch (error) {
    console.error('Error fetching all sections:', error);
    throw error;
  }
}

async function getSectionsByJobId(jobId) {
  try {
    const response = await axios.get(`${API_URL}?jobId=${jobId}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching sections for job ID ${jobId}:`, error);
    throw error;
  }
}

async function getSectionById(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching section with ID ${id}:`, error);
    throw error;
  }
}

async function updateSection(id, section) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, section);
    return response.data;
  } catch (error) {
    console.error(`Error updating section with ID ${id}:`, error);
    throw error;
  }
}

async function toggleSubcontractor(id, data) {
  try {
    const response = await axios.post(`${API_URL}/${id}/toggle-subcontractor`, data);
    return response.data;
  } catch (error) {
    console.error(`Error toggling subcontractor for section ID ${id}:`, error);
    throw error;
  }
}

async function scheduleInspection(id, inspectionDate) {
  try {
    const response = await axios.post(`${API_URL}/${id}/schedule-inspection`, { inspectionDate });
    return response.data;
  } catch (error) {
    console.error(`Error scheduling inspection for section ID ${id}:`, error);
    throw error;
  }
}

async function recordInspection(id, data) {
  try {
    const response = await axios.post(`${API_URL}/${id}/record-inspection`, data);
    return response.data;
  } catch (error) {
    console.error(`Error recording inspection result for section ID ${id}:`, error);
    throw error;
  }
}

async function getAIGuidance(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}/ai-guidance`);
    return response.data;
  } catch (error) {
    console.error(`Error getting AI guidance for section ID ${id}:`, error);
    throw error;
  }
}
