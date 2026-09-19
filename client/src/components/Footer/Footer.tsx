import { Link } from 'react-router-dom'
import './Footer.css'

export default function Footer() {
  return <footer className="site-footer"><div className="container footer-inner"><Link className="footer-brand" to="/">PortfolioHub</Link><p>讓作品說明你的能力，也讓好合作被看見。</p><span>© {new Date().getFullYear()} PortfolioHub</span></div></footer>
}
