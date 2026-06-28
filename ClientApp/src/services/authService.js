import axios from 'axios';

const API_URL = '/api/QuickAuth';

export const authService = {
  login,
  logout,
  getCurrentUser,
  getToken,
  isAdmin,
  isProjectManager,
  hasRole
};

async function login(username, password) {
  try {
    const response = await axios.post(`${API_URL}/login`, { identifier: username, password });
    
    if (response.data.token) {
      // Store user data and token in local storage
      localStorage.setItem('user', JSON.stringify(response.data.user));
      localStorage.setItem('token', response.data.token);
      
      // Set the JWT token in axios default headers for all future requests
      setAuthHeader(response.data.token);
    }
    
    return response.data;
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
}

function logout() {
  // Remove user from local storage and remove auth header
  localStorage.removeItem('user');
  localStorage.removeItem('token');
  axios.defaults.headers.common['Authorization'] = '';
}

async function getCurrentUser() {
  try {
    const token = getToken();
    
    if (!token) {
      return null;
    }
    
    // Set auth header for the request
    setAuthHeader(token);
    
    const response = await axios.get(`${API_URL}/me`);
    return response.data;
  } catch (error) {
    console.error('Get current user error:', error);
    
    // If the token is invalid or expired, clear the stored data
    if (error.response && (error.response.status === 401 || error.response.status === 403)) {
      logout();
    }
    
    return null;
  }
}

function getToken() {
  return localStorage.getItem('token');
}

function setAuthHeader(token) {
  axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
}

function isAdmin() {
  const user = JSON.parse(localStorage.getItem('user'));
  return user && user.role === 'Admin';
}

function isProjectManager() {
  const user = JSON.parse(localStorage.getItem('user'));
  return user && (user.role === 'ProjectManager' || user.role === 'Admin');
}

function hasRole(requiredRoles) {
  const user = JSON.parse(localStorage.getItem('user'));
  if (!user) return false;
  
  if (Array.isArray(requiredRoles)) {
    return requiredRoles.includes(user.role);
  } else {
    return user.role === requiredRoles;
  }
}

// Set auth header on service initialization if token exists
const token = getToken();
if (token) {
  setAuthHeader(token);
}
