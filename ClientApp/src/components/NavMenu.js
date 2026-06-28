import React, { useState } from 'react';
import { 
  Collapse, 
  Navbar, 
  NavbarToggler, 
  NavbarBrand, 
  Nav, 
  NavItem, 
  NavLink, 
  UncontrolledDropdown,
  DropdownToggle,
  DropdownMenu,
  DropdownItem 
} from 'reactstrap';
import { Link, useNavigate } from 'react-router-dom';
import { useSelector, useDispatch } from 'react-redux';
import { logout } from '../store/userSlice';
import { isAuthenticated } from '../services/auth';
import './NavMenu.css';

const NavMenu = () => {
  const [isOpen, setIsOpen] = useState(false);
  const { currentUser } = useSelector(state => state.users);
  const dispatch = useDispatch();
  const navigate = useNavigate();
  
  const toggle = () => setIsOpen(!isOpen);
  
  const handleLogout = () => {
    dispatch(logout());
    navigate('/');
  };
  
  const isAdmin = currentUser && currentUser.role === 'Administrator';
  const isManagerOrAdmin = currentUser && ['Administrator', 'ProjectManager'].includes(currentUser.role);
  
  return (
    <Navbar color="dark" dark expand="md" fixed="top" className="navbar-expand-lg">
      <NavbarBrand tag={Link} to="/">Job Tracker</NavbarBrand>
      <NavbarToggler onClick={toggle} />
      <Collapse isOpen={isOpen} navbar>
        <Nav className="mr-auto" navbar>
          {isAuthenticated() ? (
            <>
              <NavItem>
                <NavLink tag={Link} to="/dashboard">Dashboard</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} to="/jobs">Jobs</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} to="/time">Time Tracking</NavLink>
              </NavItem>
              {isManagerOrAdmin && (
                <NavItem>
                  <NavLink tag={Link} to="/subcontractors">Subcontractors</NavLink>
                </NavItem>
              )}
              {isManagerOrAdmin && (
                <NavItem>
                  <NavLink tag={Link} to="/users">Users</NavLink>
                </NavItem>
              )}
              <NavItem>
                <NavLink tag={Link} to="/code-helper">Code Helper</NavLink>
              </NavItem>
            </>
          ) : (
            <NavItem>
              <NavLink tag={Link} to="/">Home</NavLink>
            </NavItem>
          )}
        </Nav>
        
        {isAuthenticated() && currentUser && (
          <Nav className="ml-auto" navbar>
            <UncontrolledDropdown nav inNavbar>
              <DropdownToggle nav caret>
                {currentUser.firstName || currentUser.userName}
              </DropdownToggle>
              <DropdownMenu right>
                <DropdownItem tag={Link} to="/timesheet">
                  My Timesheet
                </DropdownItem>
                <DropdownItem divider />
                <DropdownItem onClick={handleLogout}>
                  Logout
                </DropdownItem>
              </DropdownMenu>
            </UncontrolledDropdown>
          </Nav>
        )}
      </Collapse>
    </Navbar>
  );
};

export default NavMenu;
