import React from 'react';
import { Link, useLocation } from 'react-router-dom';

function Layout({ children }) {
  const location = useLocation();
  
  const isActive = (path) => {
    return location.pathname === path ? 'active' : '';
  };
  
  return (
    <div className="layout">
      <header className="app-header">
        <div className="header-container">
          <h1>
            <Link to="/" className="logo">PROVIA</Link>
          </h1>
          <nav className="main-nav">
            <ul>
              <li className={isActive('/')}>
                <Link to="/">Dashboard</Link>
              </li>
              <li className={isActive('/jobs')}>
                <Link to="/jobs">Jobs</Link>
              </li>
              <li className={isActive('/sections')}>
                <Link to="/sections">Sections</Link>
              </li>
            </ul>
          </nav>
        </div>
      </header>
      
      <main className="app-content">
        {children}
      </main>
      
      <footer className="app-footer">
        <div className="footer-container">
          <p>&copy; {new Date().getFullYear()} PROVIA Application</p>
        </div>
      </footer>
    </div>
  );
}

export default Layout;