import { useEffect } from "react";
import "./Toast.css";
export interface ToastNotice { id: number; message: string; error?: boolean }
export default function Toast({ notice, onDismiss }: { notice: ToastNotice | null; onDismiss: () => void }) {
  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(onDismiss, notice.error ? 6000 : 3500);
    return () => window.clearTimeout(timer);
  }, [notice, onDismiss]);
  return notice ? <div className={`toast-card ${notice.error ? "toast-error" : ""}`}
    role={notice.error ? "alert" : "status"}>
    <span className="toast-mark" aria-hidden="true">{notice.error ? "!" : "✓"}</span>
    <span>{notice.message}</span>
  </div> : null;
}
