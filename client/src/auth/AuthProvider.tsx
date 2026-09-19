import { useCallback, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import axios from "axios";
import { readToken, writeToken } from "../api/axios";
import { authService } from "../services/authService";
import type { Account, LoginInput } from "../services/authService";
import { AuthContext } from "./AuthContext";
import type { AuthStatus } from "./AuthContext";

export default function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Account | null>(null);
  const [status, setStatus] = useState<AuthStatus>(() =>
    readToken() ? "loading" : "ready",
  );
  const [notice, setNotice] = useState("");
  const expiryTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const clearSession = useCallback((message = "") => {
    writeToken(null);
    if (expiryTimer.current) clearTimeout(expiryTimer.current);
    setUser(null);
    setStatus("ready");
    setNotice(message);
  }, []);
  const armExpiry = useCallback(
    (token: string) => {
      if (expiryTimer.current) clearTimeout(expiryTimer.current);
      try {
        // JWT timestamps are only a client-side convenience; the server validates the token.
        const part = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
        const expires = JSON.parse(atob(part)).exp * 1000;
        if (!Number.isFinite(expires)) throw new Error("Invalid expiration");
        expiryTimer.current = setTimeout(
          () => clearSession("登入已過期，請重新登入。"),
          Math.max(0, Math.min(expires - Date.now(), 2147483647)),
        );
      } catch {
        clearSession("登入已失效，請重新登入。");
      }
    },
    [clearSession],
  );
  const restore = useCallback(
    async (signal?: AbortSignal) => {
      const token = readToken();
      if (!token) return;
      try {
        const account = await authService.me(signal);
        if (readToken() !== token || signal?.aborted) return;
        setUser(account);
        setStatus("ready");
        setNotice("");
        armExpiry(token);
      } catch (error) {
        if (axios.isCancel(error) || signal?.aborted || readToken() !== token)
          return;
        if (axios.isAxiosError(error) && error.response?.status === 401)
          clearSession("登入已失效，請重新登入。");
        else setStatus("unavailable");
      }
    },
    [armExpiry, clearSession],
  );
  useEffect(() => {
    const controller = new AbortController();
    // Restore only updates React state after the asynchronous /me request resolves.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void restore(controller.signal);
    const onUnauthorized = () => clearSession("登入已失效，請重新登入。");
    window.addEventListener("portfoliohub:unauthorized", onUnauthorized);
    return () => {
      controller.abort();
      window.removeEventListener("portfoliohub:unauthorized", onUnauthorized);
      if (expiryTimer.current) clearTimeout(expiryTimer.current);
    };
  }, [restore, clearSession]);
  const login = async (input: LoginInput) => {
    const result = await authService.login(input);
    if (!result.token) throw new Error("登入資料不完整，請稍後再試。");
    writeToken(result.token);
    setUser(result);
    setStatus("ready");
    setNotice("");
    armExpiry(result.token);
  };
  const logout = async () => {
    try {
      await authService.logout();
    } catch (error) {
      if (!(axios.isAxiosError(error) && error.response?.status === 401))
        throw error;
    }
    clearSession("已登出。");
  };
  return (
    <AuthContext.Provider
      value={{
        user,
        status,
        notice,
        login,
        logout,
        clearSession,
        refresh: async () => {
          setStatus("loading");
          await restore();
        },
        updateUser: (changes) => setUser((previous) =>
          previous?.identityUserId === changes.identityUserId ? { ...previous, ...changes } : previous),
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
