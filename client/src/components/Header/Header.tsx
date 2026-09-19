import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { errorMessage } from "../../api/axios";
import Icon from "../Icon";
import "./Header.css";

function Avatar({ url, name }: { url: string | null; name: string }) {
  const [failed, setFailed] = useState(false);
  return (
    <span className="avatar" aria-label={name + "的頭像"}>
      {url && /^https?:\/\//i.test(url) && !failed ? (
        <img
          src={url}
          alt=""
          onError={() => setFailed(true)}
          referrerPolicy="no-referrer"
        />
      ) : (
        Array.from(name || "使用者")[0]
      )}
    </span>
  );
}
export default function Header() {
  const { user, status, logout, refresh } = useAuth();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const ref = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const navigate = useNavigate();
  useEffect(() => {
    if (!open) return;
    const outside = (event: PointerEvent) => {
      if (!ref.current?.contains(event.target as Node)) setOpen(false);
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
        trigger.current?.focus();
      }
    };
    document.addEventListener("pointerdown", outside);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("pointerdown", outside);
      document.removeEventListener("keydown", escape);
    };
  }, [open]);
  const handleLogout = async () => {
    setBusy(true);
    setError("");
    try {
      await logout();
      setOpen(false);
      navigate("/");
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  };
  return (
    <header className="site-header">
      <Link to="/" className="brand" aria-label="PortfolioHub 首頁">
        <span className="brand-mark">
          P<span />
        </span>
        <span className="brand-name">
          Portfolio<span>Hub</span>
        </span>
      </Link>
      {status === "loading" ? (
        <span className="header-status" role="status">
          正在確認登入…
        </span>
      ) : status === "unavailable" ? (
        <button className="button button-quiet" onClick={() => void refresh()}>
          重試登入連線
        </button>
      ) : user ? (
        <div
          className="account-control"
          ref={ref}
          onBlur={(event) => {
            // Disabling the logout button can blur it without moving focus outside.
            if (event.relatedTarget && !event.currentTarget.contains(event.relatedTarget))
              setOpen(false);
          }}
        >
          <button
            ref={trigger}
            className="account-trigger"
            aria-expanded={open}
            aria-controls="account-dropdown"
            onClick={() => setOpen(!open)}
          >
            <span className="account-name">
              {user.displayName || user.email}
            </span>
            <Icon name="chevron" />
          </button>
          <Avatar
            key={user.avatarUrl}
            url={user.avatarUrl}
            name={user.displayName || user.email}
          />
          {open && (
            <nav
              id="account-dropdown"
              className="account-dropdown"
              aria-label="帳號選單"
            >
              <Link
                to="/change-password"
                onClick={() => {
                  setOpen(false);
                  setError("");
                }}
              >
                <Icon name="lock" />
                修改密碼
              </Link>
              <Link
                to="/profile"
                onClick={() => {
                  setOpen(false);
                  setError("");
                }}
              >
                <Icon name="user" />
                基本資料
              </Link>
              <div className="menu-divider" />
              <button disabled={busy} onClick={() => void handleLogout()}>
                <Icon name="logout" />
                {busy ? "登出中…" : "登出"}
              </button>
              {error && (
                <p className="menu-error" role="alert">
                  {error}
                </p>
              )}
            </nav>
          )}
        </div>
      ) : (
        <div className="auth-actions">
          <Link className="button button-quiet" to="/login">
            登入
          </Link>
          <Link className="button button-primary" to="/register">
            註冊
          </Link>
        </div>
      )}
    </header>
  );
}
