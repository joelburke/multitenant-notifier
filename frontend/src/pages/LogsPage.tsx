import { useEffect, useState } from 'react';
import { getTenants, getLogs } from '../api';
import type { Tenant, NotificationLog } from '../api';

const statusBadge = (s: string) => {
  if (s === 'Sent') return 'badge-green';
  if (s === 'RateLimited') return 'badge-yellow';
  return 'badge-red';
};

export default function LogsPage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [selectedTenant, setSelectedTenant] = useState('');
  const [logs, setLogs] = useState<NotificationLog[]>([]);
  const [error, setError] = useState('');

  useEffect(() => { getTenants().then(setTenants); }, []);

  useEffect(() => {
    if (!selectedTenant) { setLogs([]); return; }
    getLogs(selectedTenant).then(setLogs).catch(e => setError(e.message));
  }, [selectedTenant]);

  return (
    <div>
      <h2 className="page-title">Notification Logs</h2>
      {error && <div className="alert alert-error">{error}</div>}

      <div className="card">
        <div className="form-group" style={{ marginBottom: 0 }}>
          <label>Select Tenant</label>
          <select value={selectedTenant} onChange={e => setSelectedTenant(e.target.value)} style={{ width: 280 }}>
            <option value="">— choose a tenant —</option>
            {tenants.map(t => <option key={t.id} value={t.id}>{t.name} ({t.slug})</option>)}
          </select>
        </div>
      </div>

      {selectedTenant && (
        <div className="card">
          <table>
            <thead>
              <tr><th>Time</th><th>Event Type</th><th>Channel</th><th>Status</th><th>Error</th></tr>
            </thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id}>
                  <td className="mono" style={{ whiteSpace: 'nowrap' }}>{new Date(l.createdAt).toLocaleString()}</td>
                  <td className="mono">{l.eventType}</td>
                  <td><span className="badge badge-blue">{l.channelType}</span></td>
                  <td><span className={`badge ${statusBadge(l.status)}`}>{l.status}</span></td>
                  <td style={{ color: '#dc2626', fontSize: 12 }}>{l.errorMessage ?? ''}</td>
                </tr>
              ))}
              {logs.length === 0 && <tr><td colSpan={5} style={{ textAlign: 'center', color: '#94a3b8', padding: '24px' }}>No logs yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
