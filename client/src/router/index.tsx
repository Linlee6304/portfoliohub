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
export default function AppRouter() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<App />}>
            <Route index element={<Home />} />
            <Route path="works" element={<WorkList />} />
            <Route path="login" element={<Login />} />
            <Route path="register" element={<Register />} />
            <Route element={<ProtectedRoute />}>
              <Route path="profile" element={<Profile />} />
              <Route path="change-password" element={<ChangePassword />} />
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
