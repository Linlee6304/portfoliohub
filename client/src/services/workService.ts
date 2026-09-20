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
export interface WorkInput {
  title: string;
  description: string;
  startDate: string | null;
  endDate: string | null;
  workType: number;
  status: number;
  media: WorkMediaInput[];
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
