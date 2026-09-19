import { api } from "../api/axios";
export interface AdminUser {
  identityUserId: string;
  displayName: string;
  email: string;
  contactPhone: string | null;
  isActive: boolean;
}
export const adminUserService = {
  async list(signal?: AbortSignal) {
    return (await api.get<AdminUser[]>("/admin/users", { signal })).data;
  },
  async setStatus(userId: string, isActive: boolean) {
    return (await api.patch<{ identityUserId: string; isActive: boolean }>(
      `/admin/users/${encodeURIComponent(userId)}/status`, { isActive })).data;
  },
};
