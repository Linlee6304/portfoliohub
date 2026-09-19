import axios from "axios";

const TOKEN_KEY = "portfoliohub.token";
export const readToken = () => sessionStorage.getItem(TOKEN_KEY);
export const writeToken = (token: string | null) => {
  if (token) sessionStorage.setItem(TOKEN_KEY, token);
  else sessionStorage.removeItem(TOKEN_KEY);
};
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
  timeout: 15000,
});
api.interceptors.request.use((config) => {
  const token = readToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (
      axios.isAxiosError(error) &&
      error.response?.status === 401 &&
      error.config?.headers.Authorization &&
      !["/Auth/login", "/Auth/register"].includes(error.config.url || "")
    ) {
      // Do not let a response from an older session clear a newer login.
      if (error.config.headers.Authorization === `Bearer ${readToken()}`) {
        writeToken(null);
        window.dispatchEvent(new Event("portfoliohub:unauthorized"));
      }
    }
    return Promise.reject(error);
  },
);
export function errorMessage(error: unknown): string {
  if (!axios.isAxiosError(error))
    return error instanceof Error ? error.message : "操作未完成，請稍後再試。";
  const data = error.response?.data;
  if (typeof data?.message === "string") return data.message;
  if (data?.errors) return "請檢查電子信箱與表單內容是否正確。";
  if (error.response?.status === 401) return "登入已過期，請重新登入。";
  if ([502, 503, 504].includes(error.response?.status ?? 0))
    return "服務目前無法連線，請稍後再試。";
  if ((error.response?.status ?? 0) >= 500)
    return "服務處理發生錯誤，請稍後再試。";
  if (!error.response) return "目前無法連線，請確認網路或稍後再試。";
  return "操作未完成，請稍後再試。";
}
