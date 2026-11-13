import type {
    ActivityWriteModel, AuthResponse, Conversion, Dashboard, Meta, PagedResult,
    ProblemDetails, TripDetails, TripListItem, TripQueryParams, TripWriteModel, User
} from './types';

const ACCESS_KEY = 'tp.accessToken';
const REFRESH_KEY = 'tp.refreshToken';

/** Хранилище токенов. localStorage удобен для SPA; при повышенных требованиях — httpOnly-cookie + BFF. */
export const tokens = {
    get access() { return localStorage.getItem(ACCESS_KEY); },
    get refresh() { return localStorage.getItem(REFRESH_KEY); },
    save(response: AuthResponse) {
        localStorage.setItem(ACCESS_KEY, response.accessToken);
        localStorage.setItem(REFRESH_KEY, response.refreshToken);
    },
    clear() {
        localStorage.removeItem(ACCESS_KEY);
        localStorage.removeItem(REFRESH_KEY);
    }
};

export class ApiError extends Error {
    constructor(public status: number, message: string, public errors?: Record<string, string[]>) {
        super(message);
        this.name = 'ApiError';
    }
}

/** Single-flight: параллельные 401 приводят только к одному запросу обновления токена. */
let refreshPromise: Promise<boolean> | null = null;

async function refreshTokens(): Promise<boolean> {
    const refreshToken = tokens.refresh;
    if (!refreshToken) return false;
    try {
        const response = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken })
        });
        if (!response.ok) { tokens.clear(); return false; }
        tokens.save((await response.json()) as AuthResponse);
        return true;
    } catch {
        tokens.clear();
        return false;
    }
}

async function request<T>(path: string, init: RequestInit = {}, allowRetry = true): Promise<T> {
    const headers = new Headers(init.headers);
    if (init.body) headers.set('Content-Type', 'application/json');
    if (tokens.access) headers.set('Authorization', `Bearer ${tokens.access}`);

    const response = await fetch(path, { ...init, headers });

    if (response.status === 401 && allowRetry) {
        if (tokens.refresh) {
            refreshPromise ??= refreshTokens();
            const renewed = await refreshPromise;
            refreshPromise = null;
            if (renewed) return request<T>(path, init, false);
        }
        tokens.clear();
        window.dispatchEvent(new Event('tp:unauthorized'));
    }

    if (response.status === 204) return undefined as T;

    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (!response.ok) {
        const problem = payload as ProblemDetails | null;
        const message = problem?.detail ?? problem?.title ?? `Ошибка запроса (${response.status})`;
        throw new ApiError(response.status, message, problem?.errors);
    }

    return payload as T;
}

function query(params: Record<string, unknown>): string {
    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
        if (value === undefined || value === null || value === '') return;
        search.append(key, String(value));
    });
    return search.toString();
}

export const api = {
    register: (body: { email: string; password: string; displayName: string }) =>
        request<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify(body) }),
    login: (body: { email: string; password: string }) =>
        request<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
    logout: (refreshToken: string) =>
        request<void>('/api/auth/logout', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
    me: () => request<User>('/api/auth/me'),

    meta: () => request<Meta>('/api/meta'),
    convert: (amount: number, from: string, to: string) =>
        request<Conversion>(`/api/meta/convert?${query({ amount, from, to })}`),
    dashboard: () => request<Dashboard>('/api/dashboard'),

    listTrips: (params: TripQueryParams) =>
        request<PagedResult<TripListItem>>(`/api/trips?${query(params as Record<string, unknown>)}`),
    getTrip: (id: string) => request<TripDetails>(`/api/trips/${id}`),
    createTrip: (model: TripWriteModel) =>
        request<TripDetails>('/api/trips', { method: 'POST', body: JSON.stringify(model) }),
    updateTrip: (id: string, model: TripWriteModel) =>
        request<TripDetails>(`/api/trips/${id}`, { method: 'PUT', body: JSON.stringify(model) }),
    deleteTrip: (id: string) => request<void>(`/api/trips/${id}`, { method: 'DELETE' }),

    createActivity: (tripId: string, model: ActivityWriteModel) =>
        request<TripDetails>(`/api/trips/${tripId}/activities`, { method: 'POST', body: JSON.stringify(model) }),
    updateActivity: (tripId: string, activityId: string, model: ActivityWriteModel) =>
        request<TripDetails>(`/api/trips/${tripId}/activities/${activityId}`, { method: 'PUT', body: JSON.stringify(model) }),
    deleteActivity: (tripId: string, activityId: string) =>
        request<TripDetails>(`/api/trips/${tripId}/activities/${activityId}`, { method: 'DELETE' }),

    users: (page = 1, pageSize = 20) =>
        request<PagedResult<User>>(`/api/admin/users?${query({ page, pageSize })}`)
};