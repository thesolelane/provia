import React, { useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, Button } from 'reactstrap';
import { useNavigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { isAuthenticated } from '../services/auth';
import './Home.css';

const Home = () => {
  const navigate = useNavigate();
  const { currentUser } = useSelector(state => state.users);
  
  useEffect(() => {
    // If user is already authenticated, redirect to dashboard
    if (isAuthenticated()) {
      navigate('/dashboard');
    }
  }, [navigate]);
  
  return (
    <div className="home-page">
      <div className="hero-section">
        <Container>
          <Row>
            <Col md={6}>
              <h1>Job Tracker Application</h1>
              <p className="lead">
                Comprehensive tracking system for construction and renovation projects
              </p>
              <p>
                Track job progress across 11 sections, manage subcontractors, and access 
                Massachusetts building code information all in one place.
              </p>
              <div>
                <Button color="primary" size="lg" onClick={() => window.location.href = "/signin-oidc"}>
                  Login with Active Directory
                </Button>
              </div>
            </Col>
            <Col md={6} className="d-none d-md-block">
              <div className="hero-image">
                <svg viewBox="0 0 500 400" xmlns="http://www.w3.org/2000/svg">
                  <rect x="50" y="150" width="400" height="200" rx="5" fill="#f8f9fa" stroke="#6c757d" />
                  <rect x="75" y="175" width="350" height="150" rx="3" fill="#e9ecef" stroke="#6c757d" />
                  <rect x="100" y="200" width="300" height="100" rx="3" fill="#dee2e6" stroke="#6c757d" />
                  <rect x="150" y="100" width="50" height="50" fill="#adb5bd" />
                  <rect x="300" y="100" width="50" height="50" fill="#adb5bd" />
                  <polyline points="150,100 100,150 400,150 350,100" fill="#ced4da" stroke="#6c757d" />
                  <line x1="175" y1="150" x2="175" y2="350" stroke="#6c757d" stroke-width="2" />
                  <line x1="325" y1="150" x2="325" y2="350" stroke="#6c757d" stroke-width="2" />
                </svg>
              </div>
            </Col>
          </Row>
        </Container>
      </div>
      
      <div className="features-section">
        <Container>
          <h2 className="text-center mb-5">Key Features</h2>
          <Row>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-tasks"></i>
                  </div>
                  <h3>Job Section Tracking</h3>
                  <p>
                    Track progress across 11 different job sections from permits to finish work
                  </p>
                </CardBody>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-clock"></i>
                  </div>
                  <h3>Time Tracking</h3>
                  <p>
                    Clock in/out functionality for employees with job and section assignment
                  </p>
                </CardBody>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-user-hard-hat"></i>
                  </div>
                  <h3>Subcontractor Management</h3>
                  <p>
                    Track and manage subcontractors with simplified or detailed section views
                  </p>
                </CardBody>
              </Card>
            </Col>
          </Row>
          <Row>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-book"></i>
                  </div>
                  <h3>Building Code Integration</h3>
                  <p>
                    Access Massachusetts building code information relevant to each job section
                  </p>
                </CardBody>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-robot"></i>
                  </div>
                  <h3>AI Assistant</h3>
                  <p>
                    Get answers about building codes and best practices from our AI assistant
                  </p>
                </CardBody>
              </Card>
            </Col>
            <Col md={4}>
              <Card className="mb-4">
                <CardBody>
                  <div className="feature-icon">
                    <i className="fas fa-file-export"></i>
                  </div>
                  <h3>Office Integration</h3>
                  <p>
                    Seamless integration with Microsoft Office and Google services
                  </p>
                </CardBody>
              </Card>
            </Col>
          </Row>
        </Container>
      </div>
    </div>
  );
};

export default Home;
