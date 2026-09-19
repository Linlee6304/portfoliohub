import { api } from "../api/axios";
export interface Account {
  identityUserId: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  role: "Admin" | "Creator";
  contactPhone?: string | null;
  bio?: string | null;
  workStatus?: number | null;
}
export interface LoginInput {
  email: string;
  password: string;
}
export interface RegisterInput extends LoginInput {
  confirmPassword: string;
  displayName: string;
  contactPhone?: string;
}
export interface ProfileInput {
  email: string;
  displayName: string;
  contactPhone: string;
  avatarUrl: string;
  bio: string;
  workStatus: number;
}
export interface PasswordInput {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}
type Message = { success: boolean; message: string };
export const authService = {
  async login(input: LoginInput) {
    return (
      await api.post<
        Account & Message & { token: string; tokenExpiresAt: string }
      >("/Auth/login", input)
    ).data;
  },
  async register(input: RegisterInput) {
    return (await api.post<Message>("/Auth/register", input)).data;
  },
  async me(signal?: AbortSignal) {
    return (await api.get<Account & Message>("/Auth/me", { signal })).data;
  },
  async logout() {
    await api.post("/Auth/logout");
  },
  async updateProfile(input: ProfileInput) {
    return (await api.put<Account & Message>("/Auth/profile", input)).data;
  },
  async changePassword(input: PasswordInput) {
    return (await api.post<Message>("/Auth/change-password", input)).data;
  },
};
