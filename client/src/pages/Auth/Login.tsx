import { useState } from "react";
import type { FormEvent } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { errorMessage } from "../../api/axios";
export default function Login() {
  const { user, login, status } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const location = useLocation();
  const navigate = useNavigate();
  const requestedPath: unknown = location.state?.from;
  const from = typeof requestedPath === "string" &&
    (["/profile", "/change-password"].includes(requestedPath) || /^\/works(?:\/new|\/\d+\/edit)?$/.test(requestedPath))
    ? requestedPath
    : "/";
  if (user) return <Navigate to={from} replace />;
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await login({ email: email.trim(), password });
      navigate(from, { replace: true });
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="form-card">
      <h1>登入</h1>
      <p className="form-intro">登入你的 PortfolioHub 帳號。</p>
      {location.state?.message && (
        <p className="feedback feedback-success" role="status">
          {location.state.message}
        </p>
      )}
      {error && (
        <p className="feedback feedback-error" role="alert">
          {error}
        </p>
      )}
      <form onSubmit={(event) => void submit(event)}>
        <label className="field">
          <span>電子信箱</span>
          <input
            type="email"
            autoComplete="username"
            required
            maxLength={256}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
          />
        </label>
        <label className="field">
          <span>密碼</span>
          <input
            type="password"
            autoComplete="current-password"
            required
            maxLength={128}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="輸入密碼"
          />
        </label>
        <button
          className="button button-primary form-submit"
          disabled={busy || status === "loading"}
        >
          {busy ? "登入中…" : "登入"}
        </button>
      </form>
      <p className="form-footer">
        還沒有帳號？<Link to="/register">註冊</Link>
      </p>
    </section>
  );
}
