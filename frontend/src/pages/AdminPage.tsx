import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { PagedResult, User } from '../api/types';
import Loader from '../components/Loader';
import { errorMessage } from '../labels';

export default function AdminPage() {
    const [data, setData] = useState<PagedResult<User> | null>(null);
    const [page, setPage] = useState(1);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        api.users(page, 20).then(setData).catch(e => setError(errorMessage(e)));
    }, [page]);

    if (error) return <p className="error">{error}</p>;
    if (!data) return <Loader />;

    return (
        <section>
            <h1>Пользователи</h1>
            <div className="card">
                <table className="table">
                    <thead><tr><th>Имя</th><th>E-mail</th><th>Роль</th><th>Идентификатор</th></tr></thead>
                    <tbody>
                        {data.items.map(user => (
                            <tr key={user.id}>
                                <td>{user.displayName}</td><td>{user.email}</td><td>{user.role}</td>
                                <td><code>{user.id}</code></td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                <div className="pager">
                    <button className="button secondary small" disabled={page <= 1} onClick={() => setPage(page - 1)}>← Назад</button>
                    <span>Страница {data.page} из {Math.max(data.totalPages, 1)}</span>
                    <button className="button secondary small" disabled={page >= data.totalPages} onClick={() => setPage(page + 1)}>Вперёд →</button>
                </div>
            </div>
        </section>
    );
}