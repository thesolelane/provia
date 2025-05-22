import React from 'react';
import { Routes, Route, Link } from 'react-router-dom';
import './App.css';

// Placeholder components - we'll implement these next
const Dashboard = () => <div>Dashboard</div>;
const JobList = () => <div>Job List</div>;
const JobDetail = () => <div>Job Detail</div>;
const JobForm = () => <div>Job Form</div>;
const SectionList = () => <div>Section List</div>;
const SectionDetail = () => <div>Section Detail</div>;

function App() {
  return (
    <div className="app-container">
      <header className="app-header">
        <h1>Job Tracker</h1>
        <nav>
          <ul>
            <li><Link to="/">Dashboard</Link></li>
            <li><Link to="/jobs">Jobs</Link></li>
            <li><Link to="/sections">Sections</Link></li>
          </ul>
        </nav>
      </header>
      
      <main className="app-content">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/jobs" element={<JobList />} />
          <Route path="/jobs/:id" element={<JobDetail />} />
          <Route path="/jobs/create" element={<JobForm />} />
          <Route path="/jobs/:id/edit" element={<JobForm />} />
          <Route path="/sections" element={<SectionList />} />
          <Route path="/sections/:id" element={<SectionDetail />} />
        </Routes>
      </main>
      
      <footer className="app-footer">
        <p>&copy; {new Date().getFullYear()} Job Tracker Application</p>
      </footer>
    </div>
  );
}

export default App;