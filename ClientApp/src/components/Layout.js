import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Container } from 'reactstrap';
import NavMenu from './NavMenu';
import { useSelector } from 'react-redux';
import AIAssistant from './codeHelper/AIAssistant';
import './Layout.css';

const Layout = () => {
  const [showAssistant, setShowAssistant] = useState(false);
  const { currentUser } = useSelector(state => state.users);
  const isLoggedIn = !!currentUser;

  const toggleAssistant = () => {
    setShowAssistant(!showAssistant);
  };

  return (
    <div className="app-container">
      <NavMenu />
      <Container className="main-content">
        <Outlet />
      </Container>
      
      {isLoggedIn && (
        <>
          <button 
            className="assistant-toggle-btn" 
            onClick={toggleAssistant}
            title="Building Code AI Assistant"
          >
            <i className="fas fa-robot"></i>
          </button>
          
          {showAssistant && (
            <div className="assistant-panel">
              <div className="assistant-header">
                <h4>Building Code Assistant</h4>
                <button className="close-btn" onClick={toggleAssistant}>×</button>
              </div>
              <div className="assistant-content">
                <AIAssistant inPanel={true} />
              </div>
            </div>
          )}
        </>
      )}
      
      <footer className="app-footer">
        <Container>
          <p>© {new Date().getFullYear()} Job Tracker App</p>
        </Container>
      </footer>
    </div>
  );
};

export default Layout;
