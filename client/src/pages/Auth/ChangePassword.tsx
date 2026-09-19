import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { authService } from "../../services/authService";
import { errorMessage } from "../../api/axios";
export default function ChangePassword() {
  const { clearSession } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const field = (key: keyof typeof form, value: string) =>
    setForm((prev) => ({ ...prev, [key]: value }));
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    if (form.newPassword !== form.confirmNewPassword) {
      setError("新密碼與確認密碼不一致。");
      return;
    }
    setBusy(true);
    setError("");
    try {
      const result = await authService.changePassword(form);
      clearSession(result.message);
      navigate("/login", { replace: true });
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="form-card">
      <h1>修改密碼</h1>
      <p className="form-intro">更新後，請在各裝置使用新密碼重新登入。</p>
      {error && (
        <p className="feedback feedback-error" role="alert">
          {error}
        </p>
      )}
      <form onSubmit={(event) => void submit(event)}>
        <label className="field">
          <span>目前密碼</span>
          <input
            type="password"
            autoComplete="current-password"
            required
            maxLength={128}
            value={form.currentPassword}
            onChange={(e) => field("currentPassword", e.target.value)}
          />
        </label>
        <label className="field">
          <span id="new-password-label">新密碼</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={6}
            maxLength={128}
            value={form.newPassword}
            onChange={(e) => field("newPassword", e.target.value)}
            aria-describedby="new-password-policy"
            aria-labelledby="new-password-label"
          />
          <small className="form-hint" id="new-password-policy">
            至少 6 個字元，包含大寫、小寫英文字母、數字和符號。
          </small>
        </label>
        <label className="field">
          <span>確認新密碼</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            maxLength={128}
            value={form.confirmNewPassword}
            onChange={(e) => field("confirmNewPassword", e.target.value)}
          />
        </label>
        <button className="button button-primary form-submit" disabled={busy}>
          {busy ? "更新中…" : "更新密碼"}
        </button>
      </form>
    </section>
  );
}
