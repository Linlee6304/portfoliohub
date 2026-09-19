import { useState } from "react";
import type { FormEvent } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { authService } from "../../services/authService";
import { errorMessage } from "../../api/axios";
export default function Register() {
  const { user } = useAuth();
  const [form, setForm] = useState({
    email: "",
    displayName: "",
    password: "",
    confirmPassword: "",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();
  if (user) return <Navigate to="/" replace />;
  const field = (key: keyof typeof form, value: string) =>
    setForm((prev) => ({ ...prev, [key]: value }));
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    if (form.password !== form.confirmPassword) {
      setError("兩次輸入的密碼不一致。");
      return;
    }
    setBusy(true);
    setError("");
    try {
      const result = await authService.register({
        ...form,
        email: form.email.trim(),
        displayName: form.displayName.trim(),
      });
      navigate("/login", { replace: true, state: { message: result.message } });
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="form-card">
      <h1>註冊</h1>
      <p className="form-intro">建立你的創作者帳號。</p>
      {error && (
        <p className="feedback feedback-error" role="alert">
          {error}
        </p>
      )}
      <form onSubmit={(event) => void submit(event)}>
        <label className="field">
          <span>使用者名稱</span>
          <input
            autoComplete="nickname"
            required
            maxLength={100}
            value={form.displayName}
            onChange={(e) => field("displayName", e.target.value)}
            placeholder="你希望顯示的名字"
          />
        </label>
        <label className="field">
          <span>電子信箱</span>
          <input
            type="email"
            autoComplete="username"
            required
            maxLength={256}
            value={form.email}
            onChange={(e) => field("email", e.target.value)}
            placeholder="you@example.com"
          />
        </label>
        <label className="field">
          <span id="register-password-label">密碼</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={6}
            maxLength={128}
            value={form.password}
            onChange={(e) => field("password", e.target.value)}
            aria-describedby="password-policy"
            aria-labelledby="register-password-label"
          />
          <small className="form-hint" id="password-policy">
            至少 6 個字元，包含大寫、小寫英文字母、數字和符號。
          </small>
        </label>
        <label className="field">
          <span>確認密碼</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            maxLength={128}
            value={form.confirmPassword}
            onChange={(e) => field("confirmPassword", e.target.value)}
          />
        </label>
        <button className="button button-primary form-submit" disabled={busy}>
          {busy ? "建立帳號中…" : "註冊"}
        </button>
      </form>
      <p className="form-footer">
        已經有帳號？<Link to="/login">登入</Link>
      </p>
    </section>
  );
}
