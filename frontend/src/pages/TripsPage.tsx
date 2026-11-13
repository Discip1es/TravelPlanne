import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import type { Meta, PagedResult, TripListItem, TripStatus } from '../api/types';
import Loader from '../components/Loader';
import Modal from '../components/Modal';
import { TripStatusBadge } from '../components/StatusBadge';
import TripForm from '../components/TripForm';
import { errorMessage, formatDate, money, tripStatusLabels } from '../labels';

export default function TripsPage() {
    const navigate = useNavigate();
    const [params, setParams] = useState({ search: '', status: '', sortBy: 'startDate', desc: false, page: 1, pageSize: 6 });
    const [data, setData] = useState<PagedResult<TripListItem> | null>(null);
    const [meta, setMeta] = useState<Meta | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [creating, setCreating] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    const load = useCallback(async () => {
        setLoading(true); setError(null);
        try { setData(await api.listTrips(params)); }
        catch (e) { setError(errorMessage(e)); }
        finally { setLoading(false); }
    }, [params]);

    // Небольшая задержка — чтобы поиск не отправлял запрос на каждое нажатие клавиши.
    useEffect(() => { const timer = setTimeout(load, 250); return () => clearTimeout(timer); }, [load]);
    useEffect(() => { api.meta().then(setMeta).catch(() => undefined); }, []);

    const remove = async (trip: TripListItem) => {
        if (!window.confirm(`Удалить поездку «${trip.title}» вместе с программой?`)) return;
        try { await api.deleteTrip(trip.id); await load(); }
        catch (e) { setError(errorMessage(e)); }
    };

    return (
        <section>
            <div className="page-head">
                <h1>Поездки</h1>
                <button className="button" onClick={() => setCreating(true)}>+ Новая поездка</button>
            </div>

            <div className="card filters">
                <input placeholder="Поиск: название, город, страна" value={params.search}
                    onChange={e => setParams({ ...params, search: e.target.value, page: 1 })} />
                <select value={params.status} onChange={e => setParams({ ...params, status: e.target.value, page: 1 })}>
                    <option value="">Все статусы</option>
                    {(meta?.tripStatuses ?? []).map(item => (
                        <option key={item.name} value={item.name}>{tripStatusLabels[item.name as TripStatus]}</option>
                    ))}
                </select>
                <select value={params.sortBy} onChange={e => setParams({ ...params, sortBy: e.target.value, page: 1 })}>
                    <option value="startDate">По дате начала</option>
                    <option value="endDate">По дате окончания</option>
                    <option value="title">По названию</option>
                    <option value="status">По статусу</option>
                    <option value="createdAt">По дате создания</option>
                </select>
                <label className="checkbox">
                    <input type="checkbox" checked={params.desc} onChange={e => setParams({ ...params, desc: e.target.checked, page: 1 })} />
                    по убыванию
                </label>
            </div>

            {error && <p className="error">{error}</p>}
            {loading && <Loader />}

            {!loading && data && (
                <>
                    <div className="cards">
                        {data.items.map(trip => (
                            <article className="card trip-card" key={trip.id}>
                                <header>
                                    <Link to={`/trips/${trip.id}`}><h3>{trip.title}</h3></Link>
                                    <TripStatusBadge status={trip.status} />
                                </header>
                                <p className="muted">{trip.city}, {trip.country}</p>
                                <p>{formatDate(trip.startDate)} — {formatDate(trip.endDate)} · {trip.durationDays} дн.</p>
                                <p className="muted">Пунктов программы: {trip.activitiesCount} · попутчиков: {trip.travelersCount}</p>
                                <div className="progress" title={`Использовано бюджета: ${trip.budgetUsagePercent}%`}>
                                    <i style={{ width: `${Math.min(trip.budgetUsagePercent, 100)}%` }}
                                        className={trip.budgetUsagePercent > 100 ? 'over' : ''} />
                                </div>
                                <p className="money">
                                    {money(trip.plannedCost, trip.currency)} <span className="muted">из {money(trip.budget, trip.currency)}</span>
                                </p>
                                <footer>
                                    <Link className="button secondary small" to={`/trips/${trip.id}`}>Открыть</Link>
                                    <button className="button danger small" onClick={() => remove(trip)}>Удалить</button>
                                </footer>
                            </article>
                        ))}
                    </div>

                    {data.items.length === 0 && <p className="muted">Ничего не найдено. Измените фильтры или создайте поездку.</p>}

                    <div className="pager">
                        <button className="button secondary small" disabled={data.page <= 1}
                            onClick={() => setParams({ ...params, page: params.page - 1 })}>← Назад</button>
                        <span>Страница {data.page} из {Math.max(data.totalPages, 1)} · всего {data.totalCount}</span>
                        <button className="button secondary small" disabled={data.page >= data.totalPages}
                            onClick={() => setParams({ ...params, page: params.page + 1 })}>Вперёд →</button>
                    </div>
                </>
            )}

            {creating && (
                <Modal title="Новая поездка" onClose={() => setCreating(false)}>
                    <TripForm meta={meta} submitting={submitting} onCancel={() => setCreating(false)}
                        onSubmit={async model => {
                            setSubmitting(true);
                            try { const created = await api.createTrip(model); setCreating(false); navigate(`/trips/${created.id}`); }
                            finally { setSubmitting(false); }
                        }} />
                </Modal>
            )}
        </section>
    );
}