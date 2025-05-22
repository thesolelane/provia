import React from 'react';
import { Routes, Route } from 'react-router-dom';
import './App.css';

// Import our components
import Layout from './components/Layout';
import Dashboard from './components/Dashboard';
import JobList from './components/jobs/JobList';
import JobDetail from './components/jobs/JobDetail';
import JobForm from './components/jobs/JobForm';
import JobSectionForm from './components/jobs/JobSectionForm';
import JobSectionList from './components/jobs/JobSectionList';
import JobSectionDetail from './components/jobs/JobSectionDetail';

function App() {
  return (
    <div className="app-container">
      <Routes>
        <Route path="/" element={<Layout><Dashboard /></Layout>} />
        <Route path="/jobs" element={<Layout><JobList /></Layout>} />
        <Route path="/jobs/create" element={<Layout><JobForm /></Layout>} />
        <Route path="/jobs/:id" element={<Layout><JobDetail /></Layout>} />
        <Route path="/jobs/:id/edit" element={<Layout><JobForm /></Layout>} />
        <Route path="/jobs/:id/sections/create" element={<Layout><JobSectionForm /></Layout>} />
        <Route path="/sections" element={<Layout><JobSectionList /></Layout>} />
        <Route path="/sections/:id" element={<Layout><JobSectionDetail /></Layout>} />
        <Route path="/sections/:sectionId/edit" element={<Layout><JobSectionForm /></Layout>} />
      </Routes>
    </div>
  );
}

export default App;