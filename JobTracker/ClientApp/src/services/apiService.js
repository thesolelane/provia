/**
 * API Service for handling all API requests
 * This centralizes our API calls and error handling
 */

// Base API URL - empty since we're using the proxy setting in package.json
const API_BASE_URL = '';

// General fetch function with error handling
async function fetchWithErrorHandling(url, options = {}) {
  try {
    const response = await fetch(`${API_BASE_URL}${url}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    // Check if the request was successful
    if (!response.ok) {
      // Try to get error details from response
      let errorMessage;
      try {
        const errorData = await response.json();
        errorMessage = errorData.message || `Error: ${response.status} ${response.statusText}`;
      } catch (e) {
        errorMessage = `Error: ${response.status} ${response.statusText}`;
      }
      
      throw new Error(errorMessage);
    }

    // For DELETE requests or other requests that might not return JSON
    if (options.method === 'DELETE') {
      return { success: true };
    }

    // Parse and return JSON response
    return await response.json();
  } catch (error) {
    console.error('API request failed:', error);
    throw error;
  }
}

// Jobs API
const jobsApi = {
  getAll: async () => fetchWithErrorHandling('/api/jobs'),
  
  getById: async (id) => fetchWithErrorHandling(`/api/jobs/${id}`),
  
  create: async (job) => fetchWithErrorHandling('/api/jobs', {
    method: 'POST',
    body: JSON.stringify(job),
  }),
  
  update: async (id, job) => fetchWithErrorHandling(`/api/jobs/${id}`, {
    method: 'PUT',
    body: JSON.stringify(job),
  }),
  
  delete: async (id) => fetchWithErrorHandling(`/api/jobs/${id}`, {
    method: 'DELETE',
  }),
};

// Job Sections API
const jobSectionsApi = {
  getAll: async () => fetchWithErrorHandling('/api/jobsections'),
  
  getById: async (id) => fetchWithErrorHandling(`/api/jobsections/${id}`),
  
  getByJobId: async (jobId) => fetchWithErrorHandling(`/api/jobsections/job/${jobId}`),
  
  create: async (section) => fetchWithErrorHandling('/api/jobsections', {
    method: 'POST',
    body: JSON.stringify(section),
  }),
  
  update: async (id, section) => fetchWithErrorHandling(`/api/jobsections/${id}`, {
    method: 'PUT',
    body: JSON.stringify(section),
  }),
  
  delete: async (id) => fetchWithErrorHandling(`/api/jobsections/${id}`, {
    method: 'DELETE',
  }),
};

// Contacts (CRM) API
const contactsApi = {
  getAll: async (q = '', includeInactive = false) =>
    fetchWithErrorHandling(`/api/contacts?q=${encodeURIComponent(q)}&includeInactive=${includeInactive}`),

  getById: async (id) => fetchWithErrorHandling(`/api/contacts/${id}`),

  create: async (contact) => fetchWithErrorHandling('/api/contacts', {
    method: 'POST',
    body: JSON.stringify(contact),
  }),

  update: async (id, contact) => fetchWithErrorHandling(`/api/contacts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(contact),
  }),

  deactivate: async (id) => fetchWithErrorHandling(`/api/contacts/${id}`, {
    method: 'DELETE',
  }),

  addNote: async (id, note) => fetchWithErrorHandling(`/api/contacts/${id}/activity`, {
    method: 'POST',
    body: JSON.stringify({ note }),
  }),

  linkJob: async (contactId, jobId) => fetchWithErrorHandling(`/api/contacts/${contactId}/link-job/${jobId}`, {
    method: 'POST',
  }),
};

// Leads Pipeline API
const leadsApi = {
  getAll: async (includeArchived = false) =>
    fetchWithErrorHandling(`/api/leads?includeArchived=${includeArchived}`),

  getById: async (id) => fetchWithErrorHandling(`/api/leads/${id}`),

  create: async (lead) => fetchWithErrorHandling('/api/leads', {
    method: 'POST',
    body: JSON.stringify(lead),
  }),

  update: async (id, lead) => fetchWithErrorHandling(`/api/leads/${id}`, {
    method: 'PUT',
    body: JSON.stringify(lead),
  }),

  changeStage: async (id, stage, appointmentAt = null) =>
    fetchWithErrorHandling(`/api/leads/${id}/stage`, {
      method: 'POST',
      body: JSON.stringify({ stage, appointmentAt }),
    }),

  addNote: async (id, body) => fetchWithErrorHandling(`/api/leads/${id}/notes`, {
    method: 'POST',
    body: JSON.stringify({ body }),
  }),

  archive: async (id, reason) => fetchWithErrorHandling(`/api/leads/${id}/archive`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  }),

  graduate: async (id) => fetchWithErrorHandling(`/api/leads/${id}/graduate`, {
    method: 'POST',
  }),
};

// Invoicing API
const invoicesApi = {
  getAll: async (status = 'all', search = '') =>
    fetchWithErrorHandling(`/api/invoices?status=${status}&search=${encodeURIComponent(search)}`),

  getSummary: async () => fetchWithErrorHandling('/api/invoices/summary'),

  getById: async (id) => fetchWithErrorHandling(`/api/invoices/${id}`),

  create: async (invoice) => fetchWithErrorHandling('/api/invoices', {
    method: 'POST',
    body: JSON.stringify(invoice),
  }),

  update: async (id, invoice) => fetchWithErrorHandling(`/api/invoices/${id}`, {
    method: 'PUT',
    body: JSON.stringify(invoice),
  }),

  send: async (id) => fetchWithErrorHandling(`/api/invoices/${id}/send`, { method: 'POST' }),

  recordPayment: async (id, payment) => fetchWithErrorHandling(`/api/invoices/${id}/payments`, {
    method: 'POST',
    body: JSON.stringify(payment),
  }),

  void: async (id) => fetchWithErrorHandling(`/api/invoices/${id}/void`, { method: 'POST' }),
};

// Tasks API
const tasksApi = {
  getAll: async ({ view = 'all', jobId, contactId, leadId } = {}) => {
    const params = new URLSearchParams();
    if (view !== 'all') params.set('view', view);
    if (jobId) params.set('jobId', jobId);
    if (contactId) params.set('contactId', contactId);
    if (leadId) params.set('leadId', leadId);
    return fetchWithErrorHandling(`/api/tasks?${params}`);
  },

  getCounts: async () => fetchWithErrorHandling('/api/tasks/counts'),

  getById: async (id) => fetchWithErrorHandling(`/api/tasks/${id}`),

  create: async (task) => fetchWithErrorHandling('/api/tasks', {
    method: 'POST',
    body: JSON.stringify(task),
  }),

  update: async (id, task) => fetchWithErrorHandling(`/api/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(task),
  }),

  complete: async (id, note = '') => fetchWithErrorHandling(`/api/tasks/${id}/complete`, {
    method: 'POST',
    body: JSON.stringify({ note }),
  }),

  updateStatus: async (id, status) => fetchWithErrorHandling(`/api/tasks/${id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  }),

  delete: async (id) => fetchWithErrorHandling(`/api/tasks/${id}`, { method: 'DELETE' }),
};

// Export the APIs
export const apiService = {
  jobs: jobsApi,
  jobSections: jobSectionsApi,
  contacts: contactsApi,
  leads: leadsApi,
  invoices: invoicesApi,
  tasks: tasksApi,
};