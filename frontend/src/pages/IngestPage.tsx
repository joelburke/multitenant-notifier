import { useEffect, useState } from 'react';
import { getTenants, ingestEvent } from '../api';
import type { Tenant, IngestEventResponse } from '../api';

export default function IngestPage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [form, setForm] = useState({ tenantId: '', eventType: '', payload: '{}' });
  const [result, setResult] = useState<IngestEventResponse | null>(null);
  const [error, setError] = useState('');

  useEffect(() => { getTenants().then(setTenants); }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setResult(null);
    let payload: Record<string, unknown> | undefined;
    try { payload = JSON.parse(form.payload); } catch { setError('Payload must be valid JSON.'); return; }
    try {
      const r = await ingestEvent({ tenantId: form.tenantId, eventType: form.eventType, payload });
      setResult(r);
    } catch (e: any) { setError(e.message); }
  };

  return (
    <div>
      <h2 className="page-title">Ingest Event</h2>
      <p style={{ fontSize: 13, color: '#64748b', marginBottom: 20 }}>
        Send a test event to exercise routing rules and see what gets dispatched.
      </p>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="card" style={{ maxWidth: 560 }}>
        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: 12 }}>
            <label>Tenant</label>
            <select value={form.tenantId} onChange={e => setForm(f => ({ ...f, tenantId: e.target.value }))} required>
              <option value="">— select tenant —</option>
              {tenants.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </div>
          <div className="form-group" style={{ marginBottom: 12 }}>
            <label>Event Type</label>
            <input value={form.eventType} onChange={e => setForm(f => ({ ...f, eventType: e.target.value }))} required placeholder="user.signup" />
          </div>
          <div className="form-group" style={{ marginBottom: 16 }}>
            <label>Payload (JSON)</label>
            <textarea value={form.payload} onChange={e => setForm(f => ({ ...f, payload: e.target.value }))} rows={4} style={{ fontFamily: 'monospace', fontSize: 12 }} />
          </div>
          <button type="submit" className="btn btn-primary">Send Event →</button>
        </form>
      </div>

      {result && (
        <div className="card" style={{ maxWidth: 560 }}>
          <h3>Result</h3>
          <table>
            <tbody>
              <tr><td style={{ width: 180, color: '#64748b' }}>Dispatched</td><td><strong>{result.dispatchedCount}</strong></td></tr>
              <tr><td style={{ color: '#64748b' }}>Rate Limited</td><td><span className={`badge ${result.wasRateLimited ? 'badge-red' : 'badge-green'}`}>{result.wasRateLimited ? 'Yes' : 'No'}</span></td></tr>
              <tr><td style={{ color: '#64748b' }}>Channels</td><td>{result.matchedChannels.length ? result.matchedChannels.map(c => <span key={c} className="badge badge-blue" style={{ marginRight: 4 }}>{c}</span>) : <em style={{ color: '#94a3b8' }}>none</em>}</td></tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
