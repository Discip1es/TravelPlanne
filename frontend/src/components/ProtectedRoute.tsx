import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import Loader from './Loader';

export default function ProtectedRoute({ children }: { children: ReactNode }) {
    const { user, loading } = useAuth();
    const location = useLocation();

    if (loading) return <Loader text="Проверка сессии…" />;
    if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
    return <>{children}</>;
}