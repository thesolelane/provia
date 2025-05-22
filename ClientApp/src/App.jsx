import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import Layout from './components/Layout';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import JobList from './components/Jobs/JobList';
import JobDetail from './components/Jobs/JobDetail';
import JobForm from './components/Jobs/JobForm';
import SectionList from './components/Sections/SectionList';
import SectionDetail from './components/Sections/SectionDetail';
import SectionForm from './components/Sections/SectionForm';
import ClockInOut from './components/TimeTracking/ClockInOut';
import TimeEntryList from './components/TimeTracking/TimeEntryList';
import SubcontractorList from './components/Subcontractors/SubcontractorList';
import SubcontractorForm from './components/Subcontractors/SubcontractorForm';
import BuildingCodeHelper from './components/BuildingCode/BuildingCodeHelper';
import AIAssistant from './components/AI/AIAssistant';
import { authService } from './services/authService';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f5f5f5',
    },
  },
  typography: {
    fontFamily: [
      'Roboto',
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Arial',
      'sans-serif',
    ].join(','),
  },
  components: {
    MuiTableCell: {
      styleOverrides: {
        head: {
          backgroundColor: '#f5f5f5',
          fontWeight: 'bold',
        },
      },
    },
  },
});

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in
    const checkAuthStatus = async () => {
      try {
        const currentUser = await authService.getCurrentUser();
        if (currentUser) {
          setIsAuthenticated(true);
          setUser(currentUser);
        }
      } catch (error) {
        console.error('Authentication check failed:', error);
        // Clear any invalid tokens
        authService.logout();
      } finally {
        setLoading(false);
      }
    };

    checkAuthStatus();
  }, []);

  const handleLogin = (userData) => {
    setIsAuthenticated(true);
    setUser(userData);
  };

  const handleLogout = () => {
    authService.logout();
    setIsAuthenticated(false);
    setUser(null);
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        {isAuthenticated ? (
          <Layout user={user} onLogout={handleLogout}>
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/jobs" element={<JobList />} />
              <Route path="/jobs/new" element={<JobForm />} />
              <Route path="/jobs/:id" element={<JobDetail />} />
              <Route path="/jobs/:id/edit" element={<JobForm />} />
              <Route path="/sections" element={<SectionList />} />
              <Route path="/sections/:id" element={<SectionDetail />} />
              <Route path="/sections/:id/edit" element={<SectionForm />} />
              <Route path="/timetracking" element={<TimeEntryList />} />
              <Route path="/clockinout" element={<ClockInOut />} />
              <Route path="/subcontractors" element={<SubcontractorList />} />
              <Route path="/subcontractors/new" element={<SubcontractorForm />} />
              <Route path="/subcontractors/:id" element={<SubcontractorForm />} />
              <Route path="/buildingcode" element={<BuildingCodeHelper />} />
              <Route path="/aiassistant" element={<AIAssistant />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Layout>
        ) : (
          <Routes>
            <Route path="/login" element={<Login onLogin={handleLogin} />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        )}
      </Router>
    </ThemeProvider>
  );
}

export default App;
