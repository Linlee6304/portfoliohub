import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { errorMessage } from "../../api/axios";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../components/ToastContext";
import { workService, type WorkListResult } from "../../services/workService";
import "./WorkList.css";

export default function WorkList() {
  const { user } = useAuth();
  const notify = useToast();
  const [data, setData] = useState<WorkListResult | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [selected, setSelected] = useState<number[]>([]);
  const [busy, setBusy] = useState(false);
  const deleting = useRef(false);
  const dialog = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const controller = new AbortController();
    workService.list(controller.signal).then((result) => {
      if (!controller.signal.aborted) setData(result);
    }).catch((failure: unknown) => {
      if (!controller.signal.aborted) setError(errorMessage(failure));
    });
    return () => controller.abort();
  }, [attempt, user?.identityUserId]);

  async function remove() {
    if (deleting.current || !selected.length) return;
    deleting.current = true;
    setBusy(true);
    try {
      await workService.remove(selected);
      setData((previous) => previous && ({ ...previous, items: previous.items.filter((w) => !selected.includes(w.workId)) }));
      setSelected([]);
      notify("作品已刪除");
    } catch (failure) {
      notify(errorMessage(failure), true);
    } finally {
      deleting.current = false;
      setBusy(false);
      dialog.current?.close();
    }
  }

  return <section className="works-page">
    <div className="works-heading"><div><h1>個人作品</h1>
      {data?.hasCreatorProfile && <p>管理你建立的作品</p>}</div>
      {data?.hasCreatorProfile && <div className="works-actions">
        <button className="button works-delete" disabled={busy || selected.length === 0}
          onClick={() => dialog.current?.showModal()}>刪除{selected.length > 0 ? `（${selected.length}）` : ""}</button>
        <Link className="button button-primary" to="/works/new">＋ 開始登入作品</Link>
      </div>}
    </div>
    {error ? <div className="feedback feedback-error"><p role="alert">{error}</p>
      <button className="button button-quiet" onClick={() => { setError(""); setAttempt((n) => n + 1); }}>重新載入</button>
    </div> : data === null ? <p role="status">正在載入作品…</p>
      : !data.hasCreatorProfile ? <div className="works-start"><Link className="button button-primary" to="/works/new">＋ 開始登入作品</Link></div>
      : data.items.length === 0 ? <div className="works-empty">尚未建立作品</div>
      : <ul className="works-list">{data.items.map((work) => <li key={work.workId}>
        <input className="works-checkbox" type="checkbox" checked={selected.includes(work.workId)} disabled={busy}
          aria-label={`選取${work.title}`} onChange={(event) => setSelected((previous) => event.target.checked
            ? [...previous, work.workId] : previous.filter((id) => id !== work.workId))} />
        <div className="works-info"><Link className="works-title" to={`/works/${work.workId}/edit`}>{work.title}</Link>
          <p className="works-description">{work.description || "尚未填寫作品描述"}</p>
          {(work.startDate || work.endDate) && <p className="works-date">{work.startDate?.slice(0, 10) || "未填開始日期"} ～ {work.endDate?.slice(0, 10) || "未填結束日期"}</p>}
        </div>
        <Link className="button button-quiet works-edit-link" to={`/works/${work.workId}/edit`}>編輯 →</Link>
      </li>)}</ul>}
    <dialog className="works-confirm" ref={dialog} aria-labelledby="works-delete-title"
      onCancel={(event) => { if (busy) event.preventDefault(); }}>
      <h2 id="works-delete-title">刪除所選作品？</h2>
      <p>將刪除 {selected.length} 筆作品及其附屬資料，刪除後無法復原。</p>
      <div className="works-actions"><button className="button button-quiet" disabled={busy} onClick={() => dialog.current?.close()}>取消</button>
        <button className="button works-delete" disabled={busy} onClick={() => void remove()}>{busy ? "刪除中…" : "確認刪除"}</button></div>
    </dialog>
  </section>;
}
