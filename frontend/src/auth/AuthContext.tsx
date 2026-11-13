import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, tokens } from '../api/client';
import type { User } from '../api/types';

interface AuthContextValue {
    user: User | null;
    loading: boolean;
    login: (email: string, password: string) => Promise<void>;
    register: (email: string, password: string, displayName: string) => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);

    // Восстановление сессии при перезагрузке страницы (в том числе через refresh-токен).
    useEffect(() => {
        let alive = true;
        (async () => {
            if (!tokens.access && !tokens.refresh) { setLoading(false); return; }
            try {
                const profile = await api.me();
                if (alive) setUser(profile);
            } catch {
                tokens.clear();
            } finally {
                if (alive) setLoading(false);
            }
        })();

        const onUnauthorized = () => setUser(null);
        window.addEventListener('tp:unauthorized', onUnauthorized);
        return () => { alive = false; window.removeEventListener('tp:unauthorized', onUnauthorized); };
    }, []);

    const value = useMemo<AuthContextValue>(() => ({
        user,
        loading,
        login: async (email, password) => {
            const response = await api.login({ email, password });
            tokens.save(response);
            setUser(response.user);
        },
        register: async (email, password, displayName) => {
            const response = await api.register({ email, password, displayName });
            tokens.save(response);
            setUser(response.user);
        },
        logout: async () => {
            const refreshToken = tokens.refresh;
            if (refreshToken) { try { await api.logout(refreshToken); } catch { /* выходим локально в любом случае */ } }
            tokens.clear();
            setUser(null);
        }
    }), [user, loading]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const context = useContext(AuthContext);
    if (!context) throw new Error('useAuth должен использоваться внутри AuthProvider');
    return context;
}