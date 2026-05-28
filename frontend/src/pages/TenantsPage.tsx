import { useEffect, useState } from 'react';
import { getTenants, createTenant, deleteTenant } from '../api';
import type { Tenant } from '../api';

export default function TenantsPage() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [form, setForm] = useState({ name: '', slug: '', rateLimitPerMinute: 100 });

  const load = () => getTenants().then(setTenants).catch(e => setError(e.message));

  useEffect(() => { load(); }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    try {
      await createTenant(form);
      setSuccess('Tenant created.');
      setForm({ name: '', slug: '', rateLimitPerMinute: 100 });
      load();
    } catch (e: any) { setError(e.message); }
  };

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(`Delete tenant "${name}"? This will remove all their rules and logs.`)) return;
    setError(''); setSuccess('');
    try { await deleteTenant(id); setSuccess('Tenant deleted.'); load(); }
    catch (e: any) { setError(e.message); }
  };

  return (
    <div>
      <h2 className="page-title">Tenants</h2>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="card">
        <h3>Create Tenant</h3>
        <form onSubmit={handleCreate}>
          <div className="form-row">
            <div className="form-group">
              <label>Name</label>
              <input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} required placeholder="Acme Corp" style={{ width: 200 }} />
            </div>
            <div className="form-group">
              <label>Slug</label>
              <input value={form.slug} onChange={e => setForm(f => ({ ...f, slug: e.target.value }))} required placeholder="acme-corp" pattern="[a-z0-9\-]+" style={{ width: 160 }} />
            </div>
            <div className="form-group">
              <label>Rate Limit / min</label>
              <input type="number" value={form.rateLimitPerMinute} onChange={e => setForm(f => ({ ...f, rateLimitPerMinute: +e.target.value }))} min={1} max={10000} style={{ width: 120 }} />
            </div>
            <button type="submit" className="btn btn-primary">+ Create</button>
          </div>
        </form>
      </div>

      <div className="card">
        <table>
          <thead>
            <tr>
              <th>Name</th><th>Slug</th><th>Rate Limit / min</th><th>Status</th><th>Created</th><th></th>
            </tr>
          </thead>
          <tbody>
            {tenants.map(t => (
              <tr key={t.id}>
                <td><strong>{t.name}</strong><br /><span className="mono" style={{ color: '#64748b' }}>{t.id}</span></td>
                <td className="mono">{t.slug}</td>
                <td>{t.rateLimitPerMinute.toLocaleString()}</td>
                <td><span className={`badge ${t.isActive ? 'badge-green' : 'badge-red'}`}>{t.isActive ? 'Active' : 'Inactive'}</span></td>
                <td>{new Date(t.createdAt).toLocaleDateString()}</td>
                <td><button className="btn btn-danger btn-sm" onClick={() => handleDelete(t.id, t.name)}>Delete</button></td>
              </tr>
            ))}
            {tenants.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', color: '#94a3b8', padding: '24px' }}>No tenants yet.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}
