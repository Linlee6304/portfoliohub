import "./Home.css";
export default function Home() {
  return (
    <section aria-labelledby="home-title">
      <h1 id="home-title">首頁</h1>
      <div className="home-canvas" aria-label="首頁內容區域" />
    </section>
  );
}
