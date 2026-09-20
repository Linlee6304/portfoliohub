import { useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { errorMessage } from "../../api/axios";
import { useToast } from "../../components/ToastContext";
import { workService } from "../../services/workService";
import "./WorkEdit.css";

interface FormValues { title: string; description: string; startDate: string; endDate: string }
const empty: FormValues = { title: "", description: "", startDate: "", endDate: "" };

export default function WorkEdit() {
  const { workId } = useParams();
  const navigate = useNavigate();
  const notify = useToast();
  // 每個 route 使用獨立編輯器，避免切換作品後沿用上一筆內容。
  return <WorkEditor key={workId || "new"} workId={workId}
    onCancel={() => navigate("/works")}
    onSaved={() => { notify("儲存成功"); navigate("/works", { replace: true }); }} />;
}

function WorkEditor({ workId, onCancel, onSaved }: {
  workId?: string; onCancel: () => void; onSaved: () => void;
}) {
  const [form, setForm] = useState<FormValues>(empty);
  const [loaded, setLoaded] = useState(!workId);
  const [loadError, setLoadError] = useState("");
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);

  useEffect(() => {
    if (!workId) return;
    const controller = new AbortController();
    workService.get(Number(workId), controller.signal).then((work) => {
      if (controller.signal.aborted) return;
      setForm({ title: work.title, description: work.description,
        startDate: work.startDate?.slice(0, 10) || "", endDate: work.endDate?.slice(0, 10) || "" });
      setLoaded(true);
    }).catch((failure: unknown) => {
      if (!controller.signal.aborted) setLoadError(errorMessage(failure));
    });
    return () => controller.abort();
  }, [workId, attempt]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current) return;
    if (!form.title.trim()) { setError("請填入作品名稱"); return; }
    if (form.startDate && form.endDate && form.endDate < form.startDate) {
      setError("結束日期不可早於開始日期"); return;
    }
    pending.current = true;
    setSaving(true);
    setError("");
    try {
      const input = { title: form.title.trim(), description: form.description.trim(),
        startDate: form.startDate || null, endDate: form.endDate || null };
      if (workId) await workService.update(Number(workId), input);
      else await workService.create(input);
      onSaved();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }

  const field = (name: keyof FormValues, value: string) => setForm((previous) => ({ ...previous, [name]: value }));
  return <section className="work-editor">
    <Link to="/works" className="work-back">← 作品列表</Link>
    <h1>{workId ? "編輯作品" : "新增作品"}</h1>
    <p className="page-subtitle">填寫作品的基本資料</p>
    {loadError ? <div className="feedback feedback-error"><p role="alert">{loadError}</p>
      <button className="button button-quiet" onClick={() => { setLoadError(""); setAttempt((n) => n + 1); }}>重新載入</button></div>
      : !loaded ? <p role="status">正在載入作品…</p>
      : <form className="work-form" onSubmit={(event) => void save(event)}>
        <fieldset disabled={saving}>
          <label className="field"><span>作品名稱 *</span><input required maxLength={200} value={form.title}
            onChange={(e) => field("title", e.target.value)} placeholder="請輸入作品名稱" /></label>
          <label className="field"><span>作品描述</span><textarea maxLength={5000} value={form.description}
            onChange={(e) => field("description", e.target.value)} placeholder="簡單介紹作品內容與你的貢獻" /></label>
          <div className="work-dates"><label className="field"><span>開始日期</span><input type="date" value={form.startDate}
            onChange={(e) => field("startDate", e.target.value)} /></label>
            <label className="field"><span>結束日期</span><input type="date" value={form.endDate} min={form.startDate || undefined}
              onChange={(e) => field("endDate", e.target.value)} /></label></div>
        </fieldset>
        {error && <p className="feedback feedback-error" role="alert">{error}</p>}
        <div className="work-form-actions"><button type="button" className="button button-quiet" disabled={saving} onClick={onCancel}>取消</button>
          <button type="submit" className="button button-primary" disabled={saving}>{saving ? "儲存中…" : "儲存"}</button></div>
      </form>}
  </section>;
}
