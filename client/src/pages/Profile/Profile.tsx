import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import axios from "axios";
import { useAuth } from "../../auth/AuthContext";
import { authService } from "../../services/authService";
import type { Account, ProfileInput } from "../../services/authService";
import { errorMessage } from "../../api/axios";
function ProfileForm({ account }: { account: Account }) {
  const { updateUser } = useAuth();
  const [form, setForm] = useState<ProfileInput>({
    email: account.email || "",
    displayName: account.displayName || "",
    contactPhone: account.contactPhone || "",
    avatarUrl: account.avatarUrl || "",
    bio: account.bio || "",
    workStatus: account.workStatus ?? 0,
  });
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState({ error: false, message: "" });
  const field = (key: keyof ProfileInput, value: string | number) =>
    setForm((prev) => ({ ...prev, [key]: value }));
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    setBusy(true);
    setFeedback({ error: false, message: "" });
    try {
      const result = await authService.updateProfile(form);
      updateUser(result);
      setFeedback({ error: false, message: result.message });
    } catch (failure) {
      setFeedback({ error: true, message: errorMessage(failure) });
    } finally {
      setBusy(false);
    }
  }
  if (account.role === "Admin")
    return (
      <section className="form-card">
        <h1>基本資料</h1>
        <p className="form-intro">管理員帳號</p>
        <label className="field">
          <span>使用者名稱</span>
          <input value={account.displayName} readOnly />
        </label>
        <label className="field">
          <span>電子信箱</span>
          <input value={account.email} readOnly />
        </label>
      </section>
    );
  return (
    <section className="form-card" style={{ maxWidth: 620 }}>
      <h1>基本資料</h1>
      <p className="form-intro">更新你的名稱、聯絡方式與創作者介紹。</p>
      {feedback.message && (
        <p
          className={`feedback ${feedback.error ? "feedback-error" : "feedback-success"}`}
          role={feedback.error ? "alert" : "status"}
        >
          {feedback.message}
        </p>
      )}
      <form onSubmit={(event) => void submit(event)}>
        <label className="field">
          <span>使用者名稱</span>
          <input
            required
            maxLength={100}
            autoComplete="nickname"
            value={form.displayName}
            onChange={(e) => field("displayName", e.target.value)}
          />
        </label>
        <label className="field">
          <span>電子信箱</span>
          <input
            type="email"
            required
            maxLength={256}
            autoComplete="email"
            value={form.email}
            aria-label="電子信箱"
            aria-describedby="profile-email-hint"
            onChange={(e) => field("email", e.target.value)}
          />
          <small className="form-hint" id="profile-email-hint">同時作為登入信箱與公開聯絡信箱。</small>
        </label>
        <label className="field">
          <span>聯絡電話</span>
          <input
            type="tel"
            maxLength={30}
            autoComplete="tel"
            value={form.contactPhone}
            onChange={(e) => field("contactPhone", e.target.value)}
          />
        </label>
        <label className="field">
          <span>頭像圖片網址</span>
          <input
            type="url"
            maxLength={2048}
            value={form.avatarUrl}
            onChange={(e) => field("avatarUrl", e.target.value)}
            placeholder="https://…"
          />
        </label>
        <label className="field">
          <span>接案狀態</span>
          <select
            value={form.workStatus}
            onChange={(e) => field("workStatus", Number(e.target.value))}
          >
            <option value={0}>不接案</option>
            <option value={1}>接案中</option>
            <option value={2}>可接案</option>
          </select>
        </label>
        <label className="field">
          <span>個人簡介</span>
          <textarea
            maxLength={2000}
            value={form.bio}
            onChange={(e) => field("bio", e.target.value)}
          />
        </label>
        <button className="button button-primary form-submit" disabled={busy}>
          {busy ? "儲存中…" : "儲存變更"}
        </button>
      </form>
    </section>
  );
}
export default function Profile() {
  const [account, setAccount] = useState<Account | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    authService
      .me(controller.signal)
      .then(setAccount)
      .catch((failure) => {
        if (!axios.isCancel(failure)) setError(errorMessage(failure));
      });
    return () => controller.abort();
  }, [attempt]);
  if (error)
    return (
      <div className="empty-page">
        <p role="alert">{error}</p>
        <button
          className="button button-quiet"
          onClick={() => {
            setError("");
            setAttempt((a) => a + 1);
          }}
        >
          重新載入
        </button>
      </div>
    );
  return account ? (
    <ProfileForm account={account} />
  ) : (
    <p role="status">正在載入基本資料…</p>
  );
}
