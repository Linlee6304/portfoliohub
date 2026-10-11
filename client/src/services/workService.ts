import { api } from "../api/axios";

export function validMediaUrl(value: string) {
  if (!value.trim() || value.length > 2048) return false;
  try {
    const url = new URL(value.trim());
    return /^https?:\/\//i.test(value.trim()) && ["http:", "https:"].includes(url.protocol) && !!url.hostname;
  } catch { return false; }
}

export interface WorkMediaInput {
  mediaId: number;
  mediaType: number;
  mediaUrl: string;
}
export interface WorkFeatureInput {
  featureId: number;
  featureName: string;
  startDate: string | null;
  endDate: string | null;
  status: number;
  progress: number;
  introduction: string;
}

export const featureStatusLabels: Record<number, string> = {
  0: "未開始", 1: "完成", 2: "製作中", 3: "暫時停止製作", 4: "放棄製作",
};

// 草稿允許暫時清空數字欄位；儲存前驗證，避免空白被轉成 0。
export interface FeatureDraft extends Omit<WorkFeatureInput, "progress"> {
  key: string;
  open: boolean;
  progress: number | "";
  error?: string;
}
export function featureError(item: FeatureDraft): string {
  if (!item.featureName.trim() || item.featureName.length > 200) return "功能名稱必填，最多 200 字";
  if (item.introduction.length > 5000) return "功能介紹最多 5000 字";
  if (!Number.isInteger(item.status) || item.status < 0 || item.status > 4) return "請選擇功能狀態";
  if (item.progress === "" || !Number.isInteger(item.progress) || item.progress < 0 || item.progress > 100)
    return "進度請填 0 至 100 的整數";
  if (item.startDate && item.endDate && item.endDate < item.startDate) return "功能結束日期不可早於開始日期";
  return "";
}
export interface WorkInput {
  title: string;
  description: string;
  startDate: string | null;
  endDate: string | null;
  workType: number;
  status: number;
  media: WorkMediaInput[];
  features: WorkFeatureInput[];
}
export interface Work extends WorkInput {
  workId: number;
  createdAt: string;
  updatedAt: string;
}
export interface WorkListResult {
  hasCreatorProfile: boolean;
  items: Work[];
}
export const workService = {
  list: async (signal?: AbortSignal) =>
    (await api.get<WorkListResult>("/works", { signal })).data,
  get: async (id: number, signal?: AbortSignal) =>
    (await api.get<Work>(`/works/${id}`, { signal })).data,
  create: async (input: WorkInput) => (await api.post<Work>("/works", input)).data,
  update: async (id: number, input: WorkInput) =>
    (await api.put<Work>(`/works/${id}`, input)).data,
  remove: async (workIds: number[]) => { await api.post("/works/delete-batch", { workIds }); },
};
