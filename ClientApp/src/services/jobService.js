import axios from 'axios';

const API_URL = '/api/Jobs';

export const jobService = {
  getJobs,
  getJobById,
  createJob,
  updateJob,
  deleteJob,
  exportJobToExcel,
  exportJobToWord,
  createGoogleDoc
};

async function getJobs(search = '', status = '') {
  try {
    let url = API_URL;
    const params = [];
    
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    
    if (params.length > 0) {
      url += `?${params.join('&')}`;
    }
    
    const response = await axios.get(url);
    return response.data;
  } catch (error) {
    console.error('Error fetching jobs:', error);
    throw error;
  }
}

async function getJobById(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching job with ID ${id}:`, error);
    throw error;
  }
}

async function createJob(job) {
  try {
    const response = await axios.post(API_URL, job);
    return response.data;
  } catch (error) {
    console.error('Error creating job:', error);
    throw error;
  }
}

async function updateJob(id, job) {
  try {
    const response = await axios.put(`${API_URL}/${id}`, job);
    return response.data;
  } catch (error) {
    console.error(`Error updating job with ID ${id}:`, error);
    throw error;
  }
}

async function deleteJob(id) {
  try {
    await axios.delete(`${API_URL}/${id}`);
    return true;
  } catch (error) {
    console.error(`Error deleting job with ID ${id}:`, error);
    throw error;
  }
}

async function exportJobToExcel(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}/export-excel`, {
      responseType: 'blob'
    });
    
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `Job_${id}_Export.xlsx`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    
    return true;
  } catch (error) {
    console.error(`Error exporting job to Excel with ID ${id}:`, error);
    throw error;
  }
}

async function exportJobToWord(id) {
  try {
    const response = await axios.get(`${API_URL}/${id}/export-word`, {
      responseType: 'blob'
    });
    
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `Job_${id}_Export.docx`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    
    return true;
  } catch (error) {
    console.error(`Error exporting job to Word with ID ${id}:`, error);
    throw error;
  }
}

async function createGoogleDoc(id) {
  try {
    const response = await axios.post(`${API_URL}/${id}/create-google-doc`);
    return response.data;
  } catch (error) {
    console.error(`Error creating Google Doc for job with ID ${id}:`, error);
    throw error;
  }
}
