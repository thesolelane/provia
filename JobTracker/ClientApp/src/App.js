import React from 'react';
import { Routes, Route } from 'react-router-dom';
import './App.css';

import Layout from './components/Layout';
import Dashboard from './components/Dashboard';
import JobList from './components/jobs/JobList';
import JobDetail from './components/jobs/JobDetail';
import JobForm from './components/jobs/JobForm';
import JobSectionForm from './components/jobs/JobSectionForm';
import JobSectionList from './components/jobs/JobSectionList';
import JobSectionDetail from './components/jobs/JobSectionDetail';
import ContactList from './components/contacts/ContactList';
import ContactDetail from './components/contacts/ContactDetail';
import LeadPipeline from './components/leads/LeadPipeline';
import InvoiceList from './components/invoices/InvoiceList';
import InvoiceDetail from './components/invoices/InvoiceDetail';
import TaskList from './components/tasks/TaskList';
import VendorList from './components/vendors/VendorList';
import VendorDetail from './components/vendors/VendorDetail';

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
        <Route path="/contacts" element={<Layout><ContactList /></Layout>} />
        <Route path="/contacts/:id" element={<Layout><ContactDetail /></Layout>} />
        <Route path="/leads" element={<Layout><LeadPipeline /></Layout>} />
        <Route path="/invoices" element={<Layout><InvoiceList /></Layout>} />
        <Route path="/invoices/:id" element={<Layout><InvoiceDetail /></Layout>} />
        <Route path="/tasks" element={<Layout><TaskList /></Layout>} />
        <Route path="/vendors" element={<Layout><VendorList /></Layout>} />
        <Route path="/vendors/:id" element={<Layout><VendorDetail /></Layout>} />
      </Routes>
    </div>
  );
}

export default App;
