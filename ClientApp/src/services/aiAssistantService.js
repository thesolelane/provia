import axios from 'axios';

const API_URL = '/api/AIAssistant';

export const aiAssistantService = {
  getBuildingCodeAssistance,
  getSectionGuidance,
  getGeneralAssistance
};

async function getBuildingCodeAssistance(query, sectionType = null) {
  try {
    const response = await axios.post(`${API_URL}/building-code`, {
      query,
      sectionType
    });
    return response.data;
  } catch (error) {
    console.error('Error getting building code assistance from AI:', error);
    throw error;
  }
}

async function getSectionGuidance(sectionId) {
  try {
    const response = await axios.post(`${API_URL}/section-guidance/${sectionId}`);
    return response.data;
  } catch (error) {
    console.error(`Error getting AI guidance for section ID ${sectionId}:`, error);
    throw error;
  }
}

async function getGeneralAssistance(query, jobType = 'renovation') {
  try {
    const response = await axios.post(`${API_URL}/general-question`, {
      query,
      jobType
    });
    return response.data;
  } catch (error) {
    console.error('Error getting general assistance from AI:', error);
    throw error;
  }
}
