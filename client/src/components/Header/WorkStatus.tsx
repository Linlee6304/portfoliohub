import { useRef, useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import { authService } from "../../services/authService";
import { errorMessage } from "../../api/axios";
const labels = ["不接案", "接案中", "可接案"];
export default function WorkStatus({ userId, initial, notify }: {
  userId: string; initial: number; notify: (message: string, error?: boolean) => void;
}) {
  const { updateUser } = useAuth();
  const [value, setValue] = useState(initial);
  const [saving, setSaving] = useState(false);
  const desired = useRef(initial);
  const confirmed = useRef(initial);
  const running = useRef(false);
  async function change(next: number) {
    setValue(next); desired.current = next;
    if (running.current || next === confirmed.current) return;
    running.current = true; setSaving(true);
    try {
      // 快速連續滑動時依序送出，保留最後選擇，避免較慢的舊請求蓋過新狀態。
      while (desired.current !== confirmed.current) {
        const result = await authService.updateWorkStatus(desired.current);
        confirmed.current = result.workStatus;
        updateUser({ identityUserId: userId, workStatus: result.workStatus });
      }
      notify("接案狀態已儲存");
    } catch (failure) {
      desired.current = confirmed.current; setValue(confirmed.current);
      notify(errorMessage(failure), true);
    } finally { running.current = false; setSaving(false); }
  }
  return <div className="work-status-control">
    <div className="work-status-heading"><label htmlFor="work-status-slider">接案狀態</label>
      <span>{saving ? "儲存中…" : labels[value]}</span></div>
    <input id="work-status-slider" type="range" min={0} max={2} step={1} value={value}
      aria-valuetext={labels[value]} aria-describedby="work-status-help"
      onChange={(event) => void change(Number(event.target.value))} />
    <div className="work-status-labels" aria-hidden="true">{labels.map((label, index) =>
      <span key={label} className={value === index ? "selected" : ""}>{label}</span>)}</div>
    <small id="work-status-help">滑動後自動儲存</small>
  </div>;
}
