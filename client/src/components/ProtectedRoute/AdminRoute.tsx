import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
export default function AdminRoute() {
  const { user } = useAuth();
  return user?.role === "Admin" ? <Outlet /> : <section className="empty-page">
    <h1>無權限瀏覽</h1><p>此頁面僅供管理者使用。</p><Link to="/">返回首頁</Link>
  </section>;
}
