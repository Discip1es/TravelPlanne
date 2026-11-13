import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../labels';

export default function LoginPage() {
    const { login } = useAuth();
    const navigate = useNavigate();
    const [email, setEmail] = useState('demo@travel.local');
    const [password, setPassword] = useState('Demo#12345');
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        setBusy(true); setError(null);
        try {
            await login(email, password);
            navigate('/', { replace: true });
        } catch (e) {
            setError(errorMessage(e));
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="auth-page">
            <form className="card auth-card form" onSubmit={submit}>
                <h1>Вход</h1>
                <p className="muted">Планировщик путешествий: программы, бюджеты, бронирования.</p>
                <label>E-mail<input type="email" value={email} onChange={e => setEmail(e.target.value)} required /></label>
                <label>Пароль<input type="password" value={password} onChange={e => setPassword(e.target.value)} required /></label>
                {error && <p className="error">{error}</p>}
                <button className="button" type="submit" disabled={busy}>{busy ? 'Вход…' : 'Войти'}</button>
                <p className="muted">Нет аккаунта? <Link to="/register">Зарегистрироваться</Link></p>
                <p className="hint">Демо: demo@travel.local / Demo#12345 · админ: admin@travel.local / Admin#12345</p>
            </form>
        </div>
    );
}