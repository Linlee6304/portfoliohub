import { BrowserRouter, Link, Route, Routes } from "react-router-dom";
import App from "../App";
import AuthProvider from "../auth/AuthProvider";
import ProtectedRoute from "../components/ProtectedRoute/ProtectedRoute";
import Home from "../pages/Home/Home";
import Login from "../pages/Auth/Login";
import Register from "../pages/Auth/Register";
import ChangePassword from "../pages/Auth/ChangePassword";
import Profile from "../pages/Profile/Profile";
import WorkList from "../pages/Works/WorkList";
import WorkEdit from "../pages/Works/WorkEdit";
import AdminRoute from "../components/ProtectedRoute/AdminRoute";
import AdminUsers from "../pages/Admin/AdminUsers";
export default function AppRouter() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<App />}>
            <Route index element={<Home />} />
            <Route path="login" element={<Login />} />
            <Route path="register" element={<Register />} />
            <Route element={<ProtectedRoute />}>
              <Route path="works" element={<WorkList />} />
              <Route path="works/new" element={<WorkEdit />} />
              <Route path="works/:workId/edit" element={<WorkEdit />} />
              <Route path="profile" element={<Profile />} />
              <Route path="change-password" element={<ChangePassword />} />
              <Route element={<AdminRoute />}>
                <Route path="admin/users" element={<AdminUsers />} />
              </Route>
            </Route>
            <Route
              path="*"
              element={
                <>
                  <h1>找不到頁面</h1>
                  <Link to="/">返回首頁</Link>
                </>
              }
            />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
