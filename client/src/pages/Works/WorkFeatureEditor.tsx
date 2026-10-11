import { featureStatusLabels, type FeatureDraft } from "../../services/workService";

// 後台草稿編輯器：不自行寫入資料庫，與作品／媒體一起由父表單提交。
export function WorkFeatureEditor({ items, onChange }: {
  items: FeatureDraft[]; onChange: (items: FeatureDraft[]) => void;
}) {
  const change = (key: string, patch: Partial<FeatureDraft>) =>
    onChange(items.map((item) => item.key === key ? { ...item, ...patch, error: undefined } : item));

  return <section className="work-features" aria-labelledby="features-heading">
    <div className="work-media-heading">
      <h2 id="features-heading">功能區域 <small>{items.length} / 50</small></h2>
      <button type="button" className="button button-quiet" disabled={items.length >= 50}
        onClick={() => onChange([...items, { key: crypto.randomUUID(), open: true, featureId: 0,
          featureName: "", startDate: null, endDate: null, status: 0, progress: 0, introduction: "" }])}>＋ 新增功能</button>
    </div>
    <p className="media-help">記錄每個功能的製作日期、進度與介紹；日期可留白。</p>
    {items.length === 0 && <p className="media-empty">尚未新增功能</p>}
    {items.map((item, index) => <article className="media-item feature-item" key={item.key}>
      <div className="media-row">
        <button type="button" className="media-toggle feature-toggle" aria-expanded={item.open}
          aria-controls={`feature-${item.key}`} onClick={() => change(item.key, { open: !item.open })}>
          <span aria-hidden="true">{item.open ? "▾" : "▸"}</span>
          <strong className="feature-name">{item.featureName || "新的功能"}</strong>
          <span className="feature-summary">{featureStatusLabels[item.status] ?? "請選擇狀態"} · {item.progress === "" ? "—" : item.progress}%</span>
        </button>
        <button type="button" className="media-delete" aria-label={`刪除第 ${index + 1} 筆功能`}
          onClick={() => onChange(items.filter((entry) => entry.key !== item.key))}>刪除</button>
      </div>
      <div id={`feature-${item.key}`} hidden={!item.open}>
        {item.open && <div className="feature-fields">
          <label className="field"><span>功能名稱 *</span><input maxLength={200} value={item.featureName}
            aria-invalid={!!item.error} onChange={(e) => change(item.key, { featureName: e.target.value })}
            placeholder="例如：會員登入與帳號管理" /></label>
          <div className="feature-columns">
            <label className="field"><span>開始日期（選填）</span><input type="date" value={item.startDate || ""}
              onChange={(e) => change(item.key, { startDate: e.target.value || null })} /></label>
            <label className="field"><span>結束日期（選填）</span><input type="date" value={item.endDate || ""}
              onChange={(e) => change(item.key, { endDate: e.target.value || null })} /></label>
          </div>
          <div className="feature-columns">
            <label className="field"><span>功能狀態</span><select value={item.status}
              onChange={(e) => change(item.key, { status: Number(e.target.value) })}>
              {!featureStatusLabels[item.status] && <option value={item.status}>請選擇狀態</option>}
              {Object.entries(featureStatusLabels).map(([value, text]) => <option key={value} value={value}>{text}</option>)}
            </select></label>
            <div className="field"><label htmlFor={`progress-${item.key}`}>目前進度</label>
              <div className="feature-progress">
                <input type="range" min={0} max={100} step={1} aria-label={`第 ${index + 1} 筆功能進度滑桿`}
                  value={item.progress === "" ? 0 : item.progress} onChange={(e) => change(item.key, { progress: Number(e.target.value) })} />
                <input id={`progress-${item.key}`} type="number" min={0} max={100} step={1} value={item.progress}
                  onChange={(e) => change(item.key, { progress: e.target.value === "" ? "" : Number(e.target.value) })} />
                <span>%</span>
              </div>
            </div>
          </div>
          <label className="field"><span>功能介紹</span><textarea rows={3} maxLength={5000} value={item.introduction}
            onChange={(e) => change(item.key, { introduction: e.target.value })} placeholder="介紹這個功能如何製作、使用的技術與目前進展" /></label>
          {item.error && <p className="feature-error" role="alert">{item.error}</p>}
        </div>}
      </div>
    </article>)}
  </section>;
}
