import { useEffect, useState } from 'react';
import { getTenants, getRules, createRule, deleteRule } from '../api';
import type { Tenant, RoutingRule } from '../api';

const MATCH_MODES = ['Exact', 'Prefix', 'Contains'];

export default function RulesPage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [selectedTenant, setSelectedTenant] = useState('');
  const [rules, setRules] = useState<RoutingRule[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [form, setForm] = useState({
    name: '', eventTypePattern: '', matchMode: 0, priority: 0,
    channelType: 'log', webhookUrl: '',
  });

  useEffect(() => { getTenants().then(setTenants); }, []);

  useEffect(() => {
    if (!selectedTenant) { setRules([]); return; }
    getRules(selectedTenant).then(setRules).catch(e => setError(e.message));
  }, [selectedTenant]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    const settings: Record<string, string> = {};
    if (form.channelType === 'webhook') settings['url'] = form.webhookUrl;

    try {
      await createRule(selectedTenant, {
        name: form.name,
        eventTypePattern: form.eventTypePattern,
        matchMode: form.matchMode,
        priority: form.priority,
        channels: [{ type: form.channelType, settings }],
      });
      setSuccess('Rule created.');
      setForm({ name: '', eventTypePattern: '', matchMode: 0, priority: 0, channelType: 'log', webhookUrl: '' });
      getRules(selectedTenant).then(setRules);
    } catch (e: any) { setError(e.message); }
  };

  const handleDelete = async (ruleId: string) => {
    if (!confirm('Delete this rule?')) return;
    try { await deleteRule(selectedTenant, ruleId); getRules(selectedTenant).then(setRules); }
    catch (e: any) { setError(e.message); }
  };

  return (
    <div>
      <h2 className="page-title">Routing Rules</h2>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

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
          <h3>Create Rule</h3>
          <form onSubmit={handleCreate}>
            <div className="form-row">
              <div className="form-group">
                <label>Name</label>
                <input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} required style={{ width: 180 }} placeholder="Alert on signup" />
              </div>
              <div className="form-group">
                <label>Event Type Pattern</label>
                <input value={form.eventTypePattern} onChange={e => setForm(f => ({ ...f, eventTypePattern: e.target.value }))} required style={{ width: 200 }} placeholder="user.signup" />
              </div>
              <div className="form-group">
                <label>Match Mode</label>
                <select value={form.matchMode} onChange={e => setForm(f => ({ ...f, matchMode: +e.target.value }))}>
                  {MATCH_MODES.map((m, i) => <option key={i} value={i}>{m}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label>Priority</label>
                <input type="number" value={form.priority} onChange={e => setForm(f => ({ ...f, priority: +e.target.value }))} style={{ width: 80 }} min={0} max={1000} />
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label>Channel Type</label>
                <select value={form.channelType} onChange={e => setForm(f => ({ ...f, channelType: e.target.value }))}>
                  <option value="log">Log (structured log)</option>
                  <option value="webhook">Webhook (HTTP POST)</option>
                </select>
              </div>
              {form.channelType === 'webhook' && (
                <div className="form-group">
                  <label>Webhook URL</label>
                  <input value={form.webhookUrl} onChange={e => setForm(f => ({ ...f, webhookUrl: e.target.value }))} required style={{ width: 300 }} placeholder="https://example.com/hook" />
                </div>
              )}
              <button type="submit" className="btn btn-primary">+ Create Rule</button>
            </div>
          </form>
        </div>
      )}

      {selectedTenant && (
        <div className="card">
          <table>
            <thead>
              <tr><th>Name</th><th>Pattern</th><th>Mode</th><th>Channels</th><th>Priority</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {rules.map(r => (
                <tr key={r.id}>
                  <td><strong>{r.name}</strong></td>
                  <td className="mono">{r.eventTypePattern}</td>
                  <td>{MATCH_MODES[r.matchMode]}</td>
                  <td>{r.channels.map(c => <span key={c.type} className="badge badge-blue" style={{ marginRight: 4 }}>{c.type}</span>)}</td>
                  <td>{r.priority}</td>
                  <td><span className={`badge ${r.isActive ? 'badge-green' : 'badge-red'}`}>{r.isActive ? 'Active' : 'Off'}</span></td>
                  <td><button className="btn btn-danger btn-sm" onClick={() => handleDelete(r.id)}>Delete</button></td>
                </tr>
              ))}
              {rules.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', color: '#94a3b8', padding: '24px' }}>No rules for this tenant.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
