import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./api/AuthContext";
import { Layout } from "./components/Layout";
import { ToastProvider } from "./components/Toast";
import { AlbumPage } from "./pages/AlbumPage";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage";
import { SongPage } from "./pages/SongPage";

export function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<HomePage />} />
              <Route path="albums/:albumId" element={<AlbumPage />} />
              <Route path="albums/:albumId/songs/:songId" element={<SongPage />} />
              <Route path="login" element={<LoginPage />} />
              <Route path="register" element={<RegisterPage />} />
              <Route path="me" element={<ProfilePage />} />
              <Route path="users/:userId" element={<ProfilePage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </ToastProvider>
    </AuthProvider>
  );
}
