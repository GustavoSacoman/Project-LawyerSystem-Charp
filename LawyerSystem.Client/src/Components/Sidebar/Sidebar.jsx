import React,{useState } from 'react'
import './Sidebar.css';
import { useAuth } from '../../Context/AuthContext'; 
import { Link, useNavigate } from 'react-router-dom';
import PeopleIcon from '@mui/icons-material/People';
import GavelIcon from '@mui/icons-material/Gavel';
import PersonIcon from '@mui/icons-material/Person';
import DescriptionIcon from '@mui/icons-material/Description';
import HomeIcon from '@mui/icons-material/Home';
import logoImage from '../../assets/logo.png';

const Sidebar = () => { 

    const { user, isAuthenticated, logout } = useAuth();
    const navigate = useNavigate();
    const role = user?.role;
    
    const handleLogoutClick = () => {
        logout();
        navigate('/login'); 
    };

    return (
        <>
            <div className="sidebar">
                <div className="menu-icon">
                    <div className="bar top-bar"></div>
                    <div className="bar middle-bar"></div>
                    <div className="bar bottom-bar"></div>
                </div>
                <div className="logo-section">
                    <img src={logoImage} alt="Logo" className="logo" />
                </div>
                
                <div id="nav-links">
                    <Link to="/initialpage" className="nav-link">
                        <HomeIcon style={{ color: 'white' }} />
                        <span>Início</span>
                    </Link>
                    {role === 1 && (
                        <>
                            <Link to="/managerCases" className="nav-link">
                                <DescriptionIcon style={{ color: 'white' }} />
                                <span>Processos</span>
                            </Link>
                            <Link to="/managerClient" className="nav-link">
                                <PeopleIcon style={{ color: 'white' }} />
                                <span>Clientes</span>
                            </Link>
                            <Link to="/managerLawyer" className="nav-link">
                                <GavelIcon style={{ color: 'white' }} />
                                <span>Advogados</span>
                            </Link>
                        </>
                    )}
                    <Link to="/profile" className="nav-link">
                        <PersonIcon style={{ color: 'white' }} />
                        <span>Perfil</span>
                    </Link>
                </div>

                <div id="button-section">
                    <button className={isAuthenticated ? 'logout-button' : 'login-button'} onClick={handleLogoutClick}>
                        <span>{isAuthenticated ? 'Logout' : 'Sign in'}</span>
                    </button>
                </div>
            </div>
        </>
    );
}

export default Sidebar;