import type { WorkMediaInput } from "../../services/workService";

export interface MediaDraft extends WorkMediaInput { key: string; open: boolean }
const labels: Record<number, string> = { 1: "YOUTUBE", 2: "GIT", 3: "文件" };

// 僅維護表單草稿，不呼叫 API；由父表單的儲存一次提交作品與媒體。
export function WorkMediaEditor({ items, onChange }: { items: MediaDraft[]; onChange: (items: MediaDraft[]) => void }) {
  const change = (key: string, patch: Partial<MediaDraft>) => onChange(items.map((item) => item.key === key ? { ...item, ...patch } : item));
  return <section className="work-media" aria-labelledby="media-heading">
    <div className="work-media-heading"><h2 id="media-heading">媒體區域 <small>{items.length} / 50</small></h2>
      <button type="button" className="button button-quiet" disabled={items.length >= 50}
        onClick={() => onChange([...items, { key: crypto.randomUUID(), mediaId: 0, mediaType: 1, mediaUrl: "", open: true }])}>＋ 新增媒體</button></div>
    <p className="media-help">貼上影片、程式碼或文件連結，按下儲存後套用。</p>
    {items.length === 0 && <p className="media-empty">尚未新增媒體連結</p>}
    {items.map((item, index) => <article className="media-item" key={item.key}>
      <div className="media-row">
        <button type="button" className="media-toggle" aria-expanded={item.open} aria-controls={`media-${item.key}`}
          onClick={() => change(item.key, { open: !item.open })}>
          <span aria-hidden="true">{item.open ? "▾" : "▸"}</span><strong>{labels[item.mediaType] ?? "請選擇類型"}</strong>
          <span className="media-preview">{item.mediaUrl || "尚未填入連結"}</span>
        </button>
        <button type="button" className="media-delete" aria-label={`刪除第 ${index + 1} 筆媒體`}
          onClick={() => onChange(items.filter((entry) => entry.key !== item.key))}>刪除</button>
      </div>
      {item.open && <div className="media-fields" id={`media-${item.key}`}>
        <label className="field"><span>媒體類型</span><select value={item.mediaType}
          onChange={(e) => change(item.key, { mediaType: Number(e.target.value) })}>
          {!labels[item.mediaType] && <option value={item.mediaType}>請選擇類型</option>}
          <option value={1}>YOUTUBE</option><option value={2}>GIT</option><option value={3}>文件</option>
        </select></label>
        <label className="field"><span>連結網址 *</span><input type="text" inputMode="url" maxLength={2048}
          value={item.mediaUrl} onChange={(e) => change(item.key, { mediaUrl: e.target.value })} placeholder="https://…" />
          <small>請貼上完整的 http:// 或 https:// 網址</small></label>
      </div>}
    </article>)}
  </section>;
}
