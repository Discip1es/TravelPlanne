import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useNotifications } from '../hooks/useNotifications';

export default function Layout() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();
    const { items, connected, dismiss } = useNotifications(Boolean(user));

    const onLogout = async () => {
        await logout();
        navigate('/login', { replace: true });
    };

    return (
        <div className="app">
            <header className="topbar">
                <div className="brand">🧭 TravelPlanner</div>
                <nav className="nav">
                    <NavLink to="/" end>Дашборд</NavLink>
                    <NavLink to="/trips">Поездки</NavLink>
                    {user?.role === 'Admin' && <NavLink to="/admin">Администрирование</NavLink>}
                </nav>
                <div className="user">
                    <span className={connected ? 'online' : 'offline'} title={connected ? 'Real-time подключён' : 'Нет соединения'}>●</span>
                    <span>{user?.displayName}</span>
                    <button className="button secondary small" onClick={onLogout}>Выйти</button>
                </div>
            </header>

            <main className="content"><Outlet /></main>

            <div className="toasts">
                {items.map((notification, index) => (
                    <div className="toast" key={`${notification.createdAtUtc}-${index}`}>
                        <div>
                            <b>{notification.title}</b>
                            <p>{notification.message}</p>
                        </div>
                        <button className="icon-button" onClick={() => dismiss(index)}>✕</button>
                    </div>
                ))}
            </div>
        </div>
    );
}