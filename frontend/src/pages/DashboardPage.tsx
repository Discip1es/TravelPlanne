import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Dashboard, TripStatus } from '../api/types';
import BudgetDonut from '../components/BudgetDonut';
import Loader from '../components/Loader';
import { TripStatusBadge } from '../components/StatusBadge';
import { errorMessage, formatDate, money, tripStatusLabels } from '../labels';

export default function DashboardPage() {
    const [data, setData] = useState<Dashboard | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => { api.dashboard().then(setData).catch(e => setError(errorMessage(e))); }, []);

    if (error) return <p className="error">{error}</p>;
    if (!data) return <Loader />;

    const maxStatus = Math.max(1, ...data.tripsByStatus.map(item => item.value));

    return (
        <section>
            <h1>Сводка</h1>
            <div className="stats">
                <div className="card stat"><span>Всего поездок</span><b>{data.totalTrips}</b></div>
                <div className="card stat"><span>Предстоящие</span><b>{data.upcomingTrips}</b></div>
                <div className="card stat"><span>В поездке</span><b>{data.inProgressTrips}</b></div>
                <div className="card stat"><span>Завершено</span><b>{data.completedTrips}</b></div>
            </div>

            <div className="stats">
                <div className="card stat"><span>Бюджет ({data.baseCurrency})</span><b>{money(data.totalBudget, data.baseCurrency)}</b></div>
                <div className="card stat"><span>Запланировано</span><b>{money(data.totalPlannedCost, data.baseCurrency)}</b></div>
                <div className="card stat"><span>Забронировано</span><b>{money(data.totalBookedCost, data.baseCurrency)}</b></div>
                <div className="card stat"><span>Свободный остаток</span><b>{money(data.totalBudget - data.totalPlannedCost, data.baseCurrency)}</b></div>
            </div>

            <div className="grid-2-1">
                <div className="card">
                    <h2>Структура расходов</h2>
                    <BudgetDonut data={data.costByCategory} currency={data.baseCurrency} />
                </div>
                <div className="card">
                    <h2>Поездки по статусам</h2>
                    <ul className="bars">
                        {data.tripsByStatus.map(item => (
                            <li key={item.name}>
                                <span>{tripStatusLabels[item.name as TripStatus]}</span>
                                <div className="bar"><i style={{ width: `${(item.value / maxStatus) * 100}%` }} /></div>
                                <b>{item.value}</b>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>

            <div className="card">
                <h2>Ближайшие поездки</h2>
                {data.nextTrips.length === 0 && <p className="muted">Пока ничего не запланировано. <Link to="/trips">Создать поездку</Link></p>}
                <ul className="list">
                    {data.nextTrips.map(trip => (
                        <li key={trip.id}>
                            <Link to={`/trips/${trip.id}`}><b>{trip.title}</b></Link>
                            <span className="muted">{trip.city}, {trip.country} · {formatDate(trip.startDate)} — {formatDate(trip.endDate)}</span>
                            <TripStatusBadge status={trip.status} />
                        </li>
                    ))}
                </ul>
            </div>
        </section>
    );
}