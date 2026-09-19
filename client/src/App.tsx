import { useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import Header from "./components/Header/Header";
import Icon from "./components/Icon";
import { useAuth } from "./auth/AuthContext";
import "./App.css";

const navigation = [
  { to: "/", label: "首頁", icon: "home" as const },
  { to: "/works", label: "作品列表", icon: "grid" as const },
  { to: "/profile", label: "基本資料", icon: "user" as const },
];
const pageNames: Record<string, string> = {
  "/": "首頁",
  "/works": "作品列表",
  "/profile": "基本資料",
  "/login": "登入",
  "/register": "註冊",
  "/change-password": "修改密碼",
};
export default function App() {
  const [collapsed, setCollapsed] = useState(
    () => window.matchMedia("(max-width: 700px)").matches,
  );
  const { pathname } = useLocation();
  const { notice } = useAuth();
  const page = pageNames[pathname] || "找不到頁面";
  const closeMobile = () => {
    if (window.matchMedia("(max-width: 700px)").matches) setCollapsed(true);
  };
  return (
    <div className={`app-shell ${collapsed ? "sidebar-collapsed" : ""}`}>
      <a className="skip-link" href="#main-content">
        跳至主要內容
      </a>
      {!collapsed && (
        <button
          className="sidebar-scrim"
          aria-label="收合導覽"
          onClick={() => setCollapsed(true)}
        />
      )}
      <aside className="sidebar" aria-label="主要導覽">
        <div className="sidebar-heading">
          <div className="sidebar-title">
            <span>目前頁面</span>
            <strong>{page}</strong>
          </div>
          <button
            className="sidebar-toggle"
            onClick={() => setCollapsed(!collapsed)}
            aria-label={collapsed ? "展開左側列表" : "收合左側列表"}
            aria-expanded={!collapsed}
            aria-controls="sidebar-navigation"
            title={collapsed ? "展開左側列表" : "收合左側列表"}
          >
            <Icon name="menu" />
          </button>
        </div>
        <nav id="sidebar-navigation" className="sidebar-navigation">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              onClick={closeMobile}
              title={collapsed ? item.label : undefined}
              aria-label={item.label}
              className={({ isActive }) =>
                `sidebar-link ${isActive ? "active" : ""}`
              }
            >
              <Icon name={item.icon} />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <span className="sidebar-dot" />
          <span>PortfolioHub</span>
        </div>
      </aside>
      <div className="workspace">
        <Header />
        <main id="main-content" className="main-content" tabIndex={-1}>
          {notice && (
            <p className="session-notice" role="status">
              {notice}
            </p>
          )}
          <Outlet />
        </main>
      </div>
    </div>
  );
}
