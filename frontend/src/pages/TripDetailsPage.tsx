import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { ActivityItem, ActivityWriteModel, Meta, TripDetails, TripStatus, TripWriteModel } from '../api/types';
import ActivityForm from '../components/ActivityForm';
import BudgetDonut from '../components/BudgetDonut';
import Loader from '../components/Loader';
import Modal from '../components/Modal';
import { ActivityStatusBadge, TripStatusBadge } from '../components/StatusBadge';
import TripForm from '../components/TripForm';
import { categoryLabels, errorMessage, formatDate, money, tripStatusLabels } from '../labels';

const toWriteModel = (trip: TripDetails, status: TripStatus = trip.status): TripWriteModel => ({
    title: trip.title, city: trip.city, country: trip.country, description: trip.description,
    startDate: trip.startDate, endDate: trip.endDate, budget: trip.budget, currency: trip.currency,
    status, travelers: trip.travelers, version: trip.version
});


/** undefined — форма закрыта, null — создание, объект — редактирование. */
type ActivityDraft = ActivityItem | null | undefined;

export default function TripDetailsPage() {
    const { id = '' } = useParams();
    const navigate = useNavigate();
    const [trip, setTrip] = useState<TripDetails | null>(null);
    const [meta, setMeta] = useState<Meta | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [editing, setEditing] = useState(false);
    const [draft, setDraft] = useState<ActivityDraft>(undefined);
    const [submitting, setSubmitting] = useState(false);
    const [conversion, setConversion] = useState<string | null>(null);

    const load = useCallback(async () => {
        try { setTrip(await api.getTrip(id)); }
        catch (e) { setError(errorMessage(e)); }
    }, [id]);

    useEffect(() => { void load(); }, [load]);
    useEffect(() => { api.meta().then(setMeta).catch(() => undefined); }, []);

    const days = useMemo(() => {
        if (!trip) return [];
        const map = new Map<string, ActivityItem[]>();
        trip.activities.forEach(activity => {
            map.set(activity.date, [...(map.get(activity.date) ?? []), activity]);
        });
        return [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
    }, [trip]);

    if (error) return <p className="error">{error}</p>;
    if (!trip) return <Loader />;

    const readOnly = trip.status === 'Completed' || trip.status === 'Cancelled';

    const changeStatus = async (status: TripStatus) => {
        setError(null);
        try { setTrip(await api.updateTrip(trip.id, toWriteModel(trip, status))); }
        catch (e) { setError(errorMessage(e)); }
    };

    const saveActivity = async (model: ActivityWriteModel) => {
        setSubmitting(true); setError(null);
        try {
            const updated = draft
                ? await api.updateActivity(trip.id, draft.id, model)
                : await api.createActivity(trip.id, model);
            setTrip(updated);
            setDraft(undefined);
        } catch (e) { setError(errorMessage(e)); throw e; }
        finally { setSubmitting(false); }
    };

    const removeActivity = async (activity: ActivityItem) => {
        if (!window.confirm(`Удалить «${activity.title}» из программы?`)) return;
        try { setTrip(await api.deleteActivity(trip.id, activity.id)); }
        catch (e) { setError(errorMessage(e)); }
    };

    const convertBudget = async () => {
        const target = trip.currency === 'RUB' ? 'CNY' : 'RUB';
        try {
            const result = await api.convert(trip.budget, trip.currency, target);
            setConversion(`${money(result.amount, result.from)} ≈ ${money(result.converted, result.to)} (курс ${result.rate})`);
        } catch (e) { setError(errorMessage(e)); }
    };

    const removeTrip = async () => {
        if (!window.confirm('Удалить поездку безвозвратно?')) return;
        await api.deleteTrip(trip.id);
        navigate('/trips', { replace: true });
    };

    return (
        <section>
            <p className="muted"><Link to="/trips">← Все поездки</Link></p>

            <div className="page-head">
                <div>
                    <h1>{trip.title} <TripStatusBadge status={trip.status} /></h1>
                    <p className="muted">
                        {trip.city}, {trip.country} · {formatDate(trip.startDate)} — {formatDate(trip.endDate)} · {trip.durationDays} дн.
                    </p>
                </div>
                <div className="actions">
                    <button className="button secondary small" onClick={() => setEditing(true)}>Редактировать</button>
                    <button className="button danger small" onClick={removeTrip}>Удалить</button>
                </div>
            </div>

            {trip.description && <div className="card"><p>{trip.description}</p></div>}

            <div className="card">
                <h2>Статус поездки</h2>
                <div className="actions">
                    {(meta?.tripStatuses ?? []).map(item => (
                        <button key={item.name}
                            className={`button small ${trip.status === item.name ? '' : 'secondary'}`}
                            disabled={trip.status === item.name}
                            onClick={() => changeStatus(item.name as TripStatus)}>
                            {tripStatusLabels[item.name as TripStatus]}
                        </button>
                    ))}
                </div>
                <p className="hint">Недопустимые переходы отклоняются сервером (HTTP 409) — правила заданы в домене.</p>
            </div>

            <div className="stats">
                <div className="card stat"><span>Бюджет</span><b>{money(trip.budget, trip.currency)}</b></div>
                <div className="card stat"><span>Запланировано</span><b>{money(trip.plannedCost, trip.currency)}</b></div>
                <div className="card stat"><span>Забронировано</span><b>{money(trip.bookedCost, trip.currency)}</b></div>
                <div className="card stat">
                    <span>Остаток</span>
                    <b className={trip.remaining < 0 ? 'negative' : ''}>{money(trip.remaining, trip.currency)}</b>
                </div>
            </div>

            <div className="grid-2-1">
                <div className="card">
                    <h2>Расходы по категориям</h2>
                    <BudgetDonut data={trip.costByCategory} currency={trip.currency} />
                </div>
                <div className="card">
                    <h2>Попутчики</h2>
                    <div className="chips">
                        {trip.travelers.length === 0 && <span className="muted">Не указаны</span>}
                        {trip.travelers.map(person => <span className="chip" key={person}>{person}</span>)}
                    </div>
                    <h2>Конвертер</h2>
                    <button className="button secondary small" onClick={convertBudget}>Пересчитать бюджет</button>
                    {conversion && <p className="hint">{conversion}</p>}
                    <p className="hint">Версия записи: <code>{trip.version.slice(0, 8)}</code></p>
                </div>
            </div>

            <div className="card">
                <div className="page-head">
                    <h2>Программа по дням</h2>
                    <button className="button" disabled={readOnly} onClick={() => setDraft(null)}>+ Добавить пункт</button>
                </div>
                {readOnly && <p className="hint">Поездка завершена или отменена — программа доступна только для чтения.</p>}

                {days.length === 0 && <p className="muted">Программа пуста.</p>}
                {days.map(([date, activities]) => (
                    <div className="day" key={date}>
                        <h3>{formatDate(date)}</h3>
                        <table className="table">
                            <thead>
                                <tr><th>Время</th><th>Пункт</th><th>Категория</th><th>Статус</th><th>Стоимость</th><th /></tr>
                            </thead>
                            <tbody>
                                {activities.map(activity => (
                                    <tr key={activity.id}>
                                        <td>{activity.startTime ?? '—'}<br /><span className="muted">{activity.durationMinutes} мин</span></td>
                                        <td>
                                            <b>{activity.title}</b>
                                            {activity.location && <div className="muted">{activity.location}</div>}
                                            {activity.notes && <div className="muted">{activity.notes}</div>}
                                        </td>
                                        <td>{categoryLabels[activity.category]}</td>
                                        <td><ActivityStatusBadge status={activity.status} /></td>
                                        <td>{money(activity.cost, trip.currency)}</td>
                                        <td className="row-actions">
                                            <button className="button secondary small" disabled={readOnly} onClick={() => setDraft(activity)}>✎</button>
                                            <button className="button danger small" disabled={readOnly} onClick={() => removeActivity(activity)}>🗑</button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                ))}
            </div>

            {editing && (
                <Modal title="Параметры поездки" onClose={() => setEditing(false)}>
                    <TripForm initial={trip} meta={meta} submitting={submitting} onCancel={() => setEditing(false)}
                        onSubmit={async model => {
                            setSubmitting(true);
                            try { setTrip(await api.updateTrip(trip.id, model)); setEditing(false); }
                            finally { setSubmitting(false); }
                        }} />
                </Modal>
            )}

            {draft !== undefined && (
                <Modal title={draft ? 'Изменить пункт программы' : 'Новый пункт программы'} onClose={() => setDraft(undefined)}>
                    <ActivityForm trip={trip} initial={draft} meta={meta} submitting={submitting}
                        onCancel={() => setDraft(undefined)} onSubmit={saveActivity} />
                </Modal>
            )}
        </section>
    );
}