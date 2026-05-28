import { useState } from 'react';
import './App.css';
import TenantsPage from './pages/TenantsPage';
import RulesPage from './pages/RulesPage';
import IngestPage from './pages/IngestPage';
import LogsPage from './pages/LogsPage';

type Page = 'tenants' | 'rules' | 'ingest' | 'logs';

const PAGES: { id: Page; label: string }[] = [
  { id: 'tenants', label: 'Tenants' },
  { id: 'rules', label: 'Routing Rules' },
  { id: 'ingest', label: 'Ingest Event' },
  { id: 'logs', label: 'Notification Logs' },
];

export default function App() {
  const [page, setPage] = useState<Page>('tenants');

  return (
    <div className="layout">
      <aside className="sidebar">
        <h1>Notifier Admin</h1>
        <nav>
          {PAGES.map(p => (
            <a
              key={p.id}
              href="#"
              className={page === p.id ? 'active' : ''}
              onClick={e => { e.preventDefault(); setPage(p.id); }}
            >
              {p.label}
            </a>
          ))}
        </nav>
      </aside>
      <main className="main">
        {page === 'tenants' && <TenantsPage />}
        {page === 'rules' && <RulesPage />}
        {page === 'ingest' && <IngestPage />}
        {page === 'logs' && <LogsPage />}
      </main>
    </div>
  );
}
