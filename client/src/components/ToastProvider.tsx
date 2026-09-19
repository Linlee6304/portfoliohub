import { useCallback, useState } from "react";
import type { ReactNode } from "react";
import Toast from "./Toast";
import type { ToastNotice } from "./Toast";
import { ToastContext } from "./ToastContext";
export default function ToastProvider({ children }: { children: ReactNode }) {
  const [notice, setNotice] = useState<ToastNotice | null>(null);
  const dismiss = useCallback(() => setNotice(null), []);
  const notify = useCallback((message: string, error = false) =>
    setNotice({ id: Date.now(), message, error }), []);
  return <ToastContext.Provider value={notify}>
    {children}
    <Toast notice={notice} onDismiss={dismiss} />
  </ToastContext.Provider>;
}
