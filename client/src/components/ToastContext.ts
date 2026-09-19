import { createContext, useContext } from "react";
export const ToastContext = createContext<((message: string, error?: boolean) => void) | null>(null);
export function useToast() {
  const notify = useContext(ToastContext);
  if (!notify) throw new Error("ToastProvider is required");
  return notify;
}
