import { useEffect, useRef, useState } from "react";
import axios from "axios";
import { adminUserService } from "../../services/adminUserService";
import type { AdminUser } from "../../services/adminUserService";
import { errorMessage } from "../../api/axios";
import { useToast } from "../../components/ToastContext";
import "./AdminUsers.css";

export default function AdminUsers() {
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [error, setError] = useState("");
  const [forbidden, setForbidden] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [busy, setBusy] = useState<Set<string>>(new Set());
  const pending = useRef(new Set<string>());
  const notify = useToast();
  useEffect(() => {
    const controller = new AbortController();
    adminUserService.list(controller.signal).then(setUsers).catch((failure) => {
      if (axios.isCancel(failure)) return;
      if (axios.isAxiosError(failure) && failure.response?.status === 403) setForbidden(true);
      else setError(errorMessage(failure));
    });
    return () => controller.abort();
  }, [attempt]);

  async function toggle(user: AdminUser, isActive: boolean) {
    if (pending.current.has(user.identityUserId)) return;
    pending.current.add(user.identityUserId);
    setBusy(new Set(pending.current));
    setUsers((previous) => previous?.map((item) => item.identityUserId === user.identityUserId
      ? { ...item, isActive } : item) ?? null);
    try {
      const result = await adminUserService.setStatus(user.identityUserId, isActive);
      setUsers((previous) => previous?.map((item) => item.identityUserId === user.identityUserId
        ? { ...item, isActive: result.isActive } : item) ?? null);
      notify("帳戶狀態已儲存");
    } catch (failure) {
      setUsers((previous) => previous?.map((item) => item.identityUserId === user.identityUserId
        ? { ...item, isActive: user.isActive } : item) ?? null);
      if (axios.isAxiosError(failure) && failure.response?.status === 403) setForbidden(true);
      notify(axios.isAxiosError(failure) && failure.response?.status === 403
        ? "管理者權限已變更，無法繼續操作。" : errorMessage(failure), true);
    } finally {
      pending.current.delete(user.identityUserId);
      setBusy(new Set(pending.current));
    }
  }

  if (forbidden) return <section className="empty-page"><h1>無權限瀏覽</h1><p>目前帳戶沒有管理者權限。</p></section>;
  return <section className="admin-users">
    <h1>一般使用者</h1>
    <p className="admin-intro">查看一般使用者帳戶，並控制帳戶是否可登入使用。</p>
    {error ? <div className="feedback feedback-error"><p role="alert">{error}</p>
      <button className="button button-quiet" onClick={() => { setError(""); setAttempt((n) => n + 1); }}>重新載入</button>
    </div> : users === null ? <p role="status">正在載入使用者…</p> :
      <div className="admin-users-panel">
        <div className="admin-panel-heading"><strong>使用者列表</strong><span>共 {users.length} 位</span></div>
        {users.length === 0 ? <p className="admin-empty">目前沒有一般使用者。</p> :
          <table className="admin-users-table">
            <caption className="admin-sr-only">一般使用者帳戶列表</caption>
            <thead><tr><th scope="col">使用者名稱</th><th scope="col">信箱</th><th scope="col">電話</th><th scope="col">帳戶狀態</th></tr></thead>
            <tbody>{users.map((user) => <tr key={user.identityUserId}>
              <td data-label="使用者名稱"><span className="admin-person"><span className="admin-user-avatar" aria-hidden="true">{Array.from(user.displayName || "使")[0]}</span><span>{user.displayName}</span></span></td>
              <td data-label="信箱">{user.email}</td>
              <td data-label="電話">{user.contactPhone || "未提供"}</td>
              <td data-label="帳戶狀態"><label className="admin-toggle">
                <span aria-live="polite">{busy.has(user.identityUserId) ? "儲存中…" : user.isActive ? "啟用" : "已關閉"}</span>
                <span className="admin-switch"><input type="checkbox" role="switch" checked={user.isActive}
                  disabled={busy.has(user.identityUserId)} aria-label={`${user.displayName}帳戶啟用`}
                  onChange={(event) => void toggle(user, event.target.checked)} /><span className="admin-switch-track" /></span>
              </label></td>
            </tr>)}</tbody>
          </table>}
      </div>}
    <p className="admin-note">切換後立即儲存。關閉帳戶會使既有登入失效；名稱、信箱及電話僅供查看。</p>
  </section>;
}
