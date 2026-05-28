const BASE = '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(body.error ?? res.statusText);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  rateLimitPerMinute: number;
  isActive: boolean;
  createdAt: string;
}

export interface ChannelConfig {
  type: string;
  settings: Record<string, string>;
}

export interface RoutingRule {
  id: string;
  tenantId: string;
  name: string;
  eventTypePattern: string;
  matchMode: number;
  channels: ChannelConfig[];
  priority: number;
  isActive: boolean;
  createdAt: string;
}

export interface NotificationLog {
  id: string;
  tenantId: string;
  ruleId: string | null;
  eventType: string;
  channelType: string;
  status: string;
  errorMessage: string | null;
  createdAt: string;
}

export interface IngestEventResponse {
  dispatchedCount: number;
  wasRateLimited: boolean;
  matchedChannels: string[];
}

// Tenants
export const getTenants = () => request<Tenant[]>('/tenants');
export const createTenant = (body: { name: string; slug: string; rateLimitPerMinute: number }) =>
  request<Tenant>('/tenants', { method: 'POST', body: JSON.stringify(body) });
export const updateTenant = (id: string, body: { name: string; rateLimitPerMinute: number }) =>
  request<Tenant>(`/tenants/${id}`, { method: 'PUT', body: JSON.stringify(body) });
export const deleteTenant = (id: string) =>
  request<void>(`/tenants/${id}`, { method: 'DELETE' });

// Routing Rules
export const getRules = (tenantId: string) =>
  request<RoutingRule[]>(`/tenants/${tenantId}/rules`);
export const createRule = (tenantId: string, body: Omit<RoutingRule, 'id' | 'tenantId' | 'isActive' | 'createdAt'>) =>
  request<RoutingRule>(`/tenants/${tenantId}/rules`, { method: 'POST', body: JSON.stringify(body) });
export const deleteRule = (tenantId: string, ruleId: string) =>
  request<void>(`/tenants/${tenantId}/rules/${ruleId}`, { method: 'DELETE' });

// Logs
export const getLogs = (tenantId: string, page = 1) =>
  request<NotificationLog[]>(`/tenants/${tenantId}/logs?page=${page}&pageSize=50`);

// Events
export const ingestEvent = (body: { tenantId: string; eventType: string; payload?: Record<string, unknown> }) =>
  request<IngestEventResponse>('/events', { method: 'POST', body: JSON.stringify(body) });
