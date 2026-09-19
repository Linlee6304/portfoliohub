import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import axios from "axios";
import { useAuth } from "../../auth/AuthContext";
import { authService } from "../../services/authService";
import type { Account, ProfileInput } from "../../services/authService";
import { errorMessage } from "../../api/axios";
import { useToast } from "../../components/ToastContext";
import "./Profile.css";

function ProfileForm({ account }: { account: Account }) {
  const { updateUser } = useAuth();
  const [form, setForm] = useState<ProfileInput>({
    displayName: account.displayName || "", contactPhone: account.contactPhone || "",
    avatarUrl: account.avatarUrl || "", bio: account.bio || "",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const notify = useToast();
  const field = (key: keyof ProfileInput, value: string) => setForm((prev) => ({ ...prev, [key]: value }));
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    setBusy(true); setError("");
    try {
      const result = await authService.updateProfile(form);
      // 只合併此表單負責的四個欄位，避免覆蓋同時更新的接案狀態。
      const saved = { displayName: result.displayName, contactPhone: result.contactPhone || "",
        avatarUrl: result.avatarUrl || "", bio: result.bio || "" };
      updateUser({ identityUserId: account.identityUserId, ...saved });
      setForm(saved);
      notify("儲存成功");
    } catch (failure) { setError(errorMessage(failure)); }
    finally { setBusy(false); }
  }
  return <section className="form-card profile-card">
    <h1>基本資料</h1>
    <p className="form-intro">讓大家更認識你，更新你的暱稱與個人介紹。</p>
    <label className="field"><span>登入信箱</span>
      <input type="email" value={account.email} readOnly aria-label="登入信箱" aria-describedby="profile-email-hint" />
      <small className="form-hint" id="profile-email-hint">登入信箱固定，無法修改。</small>
    </label>
    {account.workStatus == null ? <p className="form-intro">此帳戶尚無可更新的創作者基本資料。</p> :
      <form onSubmit={(event) => void submit(event)}>
        {error && <p className="feedback feedback-error" role="alert">{error}</p>}
        <fieldset disabled={busy} className="profile-fields">
          <label className="field"><span>暱稱</span>
            <input required maxLength={100} autoComplete="nickname" value={form.displayName}
              onChange={(e) => field("displayName", e.target.value)} />
          </label>
          <label className="field"><span>電話</span>
            <input type="tel" maxLength={30} autoComplete="tel" value={form.contactPhone}
              onChange={(e) => field("contactPhone", e.target.value)} />
          </label>
          <label className="field"><span>頭像圖片網址</span>
            <input type="url" maxLength={2048} value={form.avatarUrl} placeholder="https://…"
              onChange={(e) => field("avatarUrl", e.target.value)} />
          </label>
          <label className="field"><span>簡介</span>
            <textarea aria-label="簡介" maxLength={2000} value={form.bio} onChange={(e) => field("bio", e.target.value)} />
          </label>
          <div className="profile-actions"><button className="button button-primary" type="submit">
            {busy ? "儲存中…" : "儲存"}
          </button></div>
        </fieldset>
      </form>}
  </section>;
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
