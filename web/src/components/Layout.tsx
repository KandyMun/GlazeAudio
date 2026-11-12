import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { Disc3, LogIn, LogOut, Menu, UserRound, UserPlus, X, Code2 } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import { Logo } from "./Logo";

export function Layout() {
  const { user, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

  // Close the mobile menu after navigating.
  useEffect(() => setMenuOpen(false), [location.pathname]);

  async function handleLogout() {
    await logout();
    navigate("/");
  }

  return (
    <div className="app">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="site-header">
        <div className="container site-header__inner">
          <Link to="/" className="brand" aria-label="GlazeAudio home">
            <Logo />
            <span className="brand__name">GlazeAudio</span>
          </Link>

          <button
            className="menu-toggle"
            aria-expanded={menuOpen}
            aria-controls="main-nav"
            onClick={() => setMenuOpen((o) => !o)}
          >
            {menuOpen ? <X size={24} aria-hidden="true" /> : <Menu size={24} aria-hidden="true" />}
            <span className="visually-hidden">{menuOpen ? "Close menu" : "Open menu"}</span>
          </button>

          <nav id="main-nav" className={`site-nav${menuOpen ? " is-open" : ""}`} aria-label="Main">
            <NavLink to="/" end className="site-nav__link">
              <Disc3 size={18} aria-hidden="true" /> Albums
            </NavLink>
            {user ? (
              <>
                <NavLink to="/me" className="site-nav__link">
                  <UserRound size={18} aria-hidden="true" /> {user.username}
                  {user.role === "Admin" && <span className="tag tag--admin">Admin</span>}
                </NavLink>
                <button className="site-nav__link site-nav__button" onClick={handleLogout}>
                  <LogOut size={18} aria-hidden="true" /> Log out
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login" className="site-nav__link">
                  <LogIn size={18} aria-hidden="true" /> Log in
                </NavLink>
                <NavLink to="/register" className="site-nav__link site-nav__link--cta">
                  <UserPlus size={18} aria-hidden="true" /> Sign up
                </NavLink>
              </>
            )}
          </nav>
        </div>
      </header>

      <main id="main" className="site-main">
        <Outlet />
      </main>

      <footer className="site-footer">
        <div className="container site-footer__inner">
          <div className="site-footer__brand">
            <Logo size={28} />
            <p>
              <strong>GlazeAudio</strong>
              <br />
              Songs rated on lyrics, melody, mood and expressiveness.
            </p>
          </div>
          <ul className="site-footer__links">
            <li>
              <a href="https://github.com/KandyMun/GlazeAudio" target="_blank" rel="noreferrer">
                <Code2 size={16} aria-hidden="true" /> Source code
              </a>
            </li>
          </ul>
          <p className="site-footer__note">University course project. Demo data only.</p>
        </div>
      </footer>
    </div>
  );
}
