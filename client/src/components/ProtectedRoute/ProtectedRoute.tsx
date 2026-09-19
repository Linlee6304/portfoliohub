import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
export default function ProtectedRoute() {
  const { user, status, refresh } = useAuth();
  const location = useLocation();
  if (status === "loading") return <p role="status">正在確認登入…</p>;
  if (status === "unavailable")
    return (
      <div className="empty-page">
        <p>暫時無法確認登入狀態。</p>
        <button className="button button-quiet" onClick={() => void refresh()}>
          重試連線
        </button>
      </div>
    );
  return user ? (
    <Outlet />
  ) : (
    <Navigate to="/login" state={{ from: location.pathname }} replace />
  );
}
