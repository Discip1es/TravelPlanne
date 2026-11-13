import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../labels';

export default function RegisterPage() {
    const { register } = useAuth();
    const navigate = useNavigate();
    const [displayName, setDisplayName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        setBusy(true); setError(null);
        try {
            await register(email, password, displayName);
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
                <h1>Регистрация</h1>
                <label>Имя<input value={displayName} onChange={e => setDisplayName(e.target.value)} required minLength={2} maxLength={64} /></label>
                <label>E-mail<input type="email" value={email} onChange={e => setEmail(e.target.value)} required /></label>
                <label>Пароль (минимум 8 символов)<input type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={8} /></label>
                {error && <p className="error">{error}</p>}
                <button className="button" type="submit" disabled={busy}>{busy ? 'Создание…' : 'Создать аккаунт'}</button>
                <p className="muted">Уже зарегистрированы? <Link to="/login">Войти</Link></p>
            </form>
        </div>
    );
}