import axios from 'axios';

const API_URL = '/api/BuildingCode';

export const buildingCodeService = {
  getAllCodes,
  getCodeById,
  searchCodes,
  getCodesBySection,
  getCodesByCategory,
  createCode,
  updateCode,
  deleteCode
};

async function getAllCodes() {
  try {
    const response = await axios.get(API_URL);
    return response.data;
  } catch (error) {
    console.error('Error fetching all building codes:', error);
    throw error;
  }
}

async function getCodeById(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching building code with ID ${id}:`, error);
    throw error;
  }
}

async function searchCodes(searchTerm) {
  try {
    const response = await axios.get(`${API_URL}/search?searchTerm=${encodeURIComponent(searchTerm)}`);
    return response.data;
  } catch (error) {
    console.error(`Error searching building codes with term "${searchTerm}":`, error);
    throw error;
  }
}

async function getCodesBySection(sectionType) {
  try {
    const response = await axios.get(`${API_URL}/section/${sectionType}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching building codes for section type ${sectionType}:`, error);
    throw error;
  }
}

async function getCodesByCategory(category) {
  try {
    const response = await axios.get(`${API_URL}/category/${encodeURIComponent(category)}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching building codes for category "${category}":`, error);
    throw error;
  }
}

async function createCode(code) {
  try {
    const response = await axios.post(API_URL, code);
    return response.data;
  } catch (error) {
    console.error('Error creating building code:', error);
    throw error;
  }
}

async function updateCode(id, code) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, code);
    return response.data;
  } catch (error) {
    console.error(`Error updating building code with ID ${id}:`, error);
    throw error;
  }
}

async function deleteCode(id) {
  try {
    await axios.delete(`${API_URL}/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting building code with ID ${id}:`, error);
    throw error;
  }
}
