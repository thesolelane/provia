import React, { useEffect } from 'react';
import { Route, Routes, Navigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { fetchCurrentUser } from './store/userSlice';
import Layout from './components/Layout';
import Home from './components/Home';
import Dashboard from './components/Dashboard';
import JobList from './components/jobs/JobList';
import JobDetail from './components/jobs/JobDetail';
import JobForm from './components/jobs/JobForm';
import JobSectionDetail from './components/jobs/JobSectionDetail';
import TimeTracker from './components/time/TimeTracker';
import TimeSheet from './components/time/TimeSheet';
import SubcontractorList from './components/subcontractors/SubcontractorList';
import SubcontractorForm from './components/subcontractors/SubcontractorForm';
import UserList from './components/users/UserList';
import UserForm from './components/users/UserForm';
import BuildingCodeHelper from './components/codeHelper/BuildingCodeHelper';
import AIAssistant from './components/codeHelper/AIAssistant';
import { isAuthenticated } from './services/auth';
import './App.css';

// Protected route component to handle authentication
const ProtectedRoute = ({ children, requiredRoles = [] }) => {
  const { currentUser, isLoading } = useSelector(state => state.users);
  
  if (isLoading) {
    return <div className="loading-spinner">Loading...</div>;
  }
  
  if (!isAuthenticated()) {
    return <Navigate to="/" replace />;
  }
  
  if (requiredRoles.length > 0 && currentUser && !requiredRoles.includes(currentUser.role)) {
    return <Navigate to="/dashboard" replace />;
  }
  
  return children;
};

function App() {
  const dispatch = useDispatch();
  const { isLoading } = useSelector(state => state.users);
  
  useEffect(() => {
    if (isAuthenticated()) {
      dispatch(fetchCurrentUser());
    }
  }, [dispatch]);
  
  if (isLoading) {
    return <div className="app-loading">Loading application...</div>;
  }
  
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Home />} />
        
        <Route path="dashboard" element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        } />
        
        <Route path="jobs" element={
          <ProtectedRoute>
            <JobList />
          </ProtectedRoute>
        } />
        
        <Route path="jobs/new" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager']}>
            <JobForm />
          </ProtectedRoute>
        } />
        
        <Route path="jobs/:id" element={
          <ProtectedRoute>
            <JobDetail />
          </ProtectedRoute>
        } />
        
        <Route path="jobs/:id/edit" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager']}>
            <JobForm />
          </ProtectedRoute>
        } />
        
        <Route path="sections/:id" element={
          <ProtectedRoute>
            <JobSectionDetail />
          </ProtectedRoute>
        } />
        
        <Route path="time" element={
          <ProtectedRoute>
            <TimeTracker />
          </ProtectedRoute>
        } />
        
        <Route path="timesheet" element={
          <ProtectedRoute>
            <TimeSheet />
          </ProtectedRoute>
        } />
        
        <Route path="subcontractors" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager', 'Supervisor']}>
            <SubcontractorList />
          </ProtectedRoute>
        } />
        
        <Route path="subcontractors/new" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager']}>
            <SubcontractorForm />
          </ProtectedRoute>
        } />
        
        <Route path="subcontractors/:id/edit" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager']}>
            <SubcontractorForm />
          </ProtectedRoute>
        } />
        
        <Route path="users" element={
          <ProtectedRoute requiredRoles={['Administrator', 'ProjectManager']}>
            <UserList />
          </ProtectedRoute>
        } />
        
        <Route path="users/new" element={
          <ProtectedRoute requiredRoles={['Administrator']}>
            <UserForm />
          </ProtectedRoute>
        } />
        
        <Route path="users/:id/edit" element={
          <ProtectedRoute requiredRoles={['Administrator']}>
            <UserForm />
          </ProtectedRoute>
        } />
        
        <Route path="code-helper" element={
          <ProtectedRoute>
            <BuildingCodeHelper />
          </ProtectedRoute>
        } />
        
        <Route path="ai-assistant" element={
          <ProtectedRoute>
            <AIAssistant />
          </ProtectedRoute>
        } />
        
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
