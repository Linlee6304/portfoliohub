import { createContext, useContext } from "react";
import type { Account, LoginInput } from "../services/authService";
export type AuthStatus = "loading" | "ready" | "unavailable";
export interface AuthState {
  user: Account | null;
  status: AuthStatus;
  notice: string;
  login: (input: LoginInput) => Promise<void>;
  logout: () => Promise<void>;
  clearSession: (message?: string) => void;
  refresh: () => Promise<void>;
  updateUser: (changes: Partial<Account> & Pick<Account, "identityUserId">) => void;
}
export const AuthContext = createContext<AuthState | null>(null);
export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("AuthProvider is required");
  return value;
}
