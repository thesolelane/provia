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

// Placeholder for sections (we'll implement these next)
const SectionList = () => <div>Section List</div>;
const SectionDetail = () => <div>Section Detail</div>;

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
        <Route path="/sections" element={<Layout><SectionList /></Layout>} />
        <Route path="/sections/:id" element={<Layout><SectionDetail /></Layout>} />
        <Route path="/sections/:sectionId/edit" element={<Layout><JobSectionForm /></Layout>} />
      </Routes>
    </div>
  );
}

export default App;