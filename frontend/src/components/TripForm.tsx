import { useState } from 'react';
import type { Meta, TripDetails, TripStatus, TripWriteModel } from '../api/types';
import { tripStatusLabels } from '../labels';

interface Props {
    initial?: TripDetails | null;
    meta: Meta | null;
    submitting: boolean;
    onSubmit: (model: TripWriteModel) => Promise<void>;
    onCancel: () => void;
}

const today = () => new Date().toISOString().slice(0, 10);
const plusDays = (days: number) => new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);

export default function TripForm({ initial, meta, submitting, onSubmit, onCancel }: Props) {
    const [title, setTitle] = useState(initial?.title ?? '');
    const [city, setCity] = useState(initial?.city ?? '');
    const [country, setCountry] = useState(initial?.country ?? 'Россия');
    const [description, setDescription] = useState(initial?.description ?? '');
    const [startDate, setStartDate] = useState(initial?.startDate ?? plusDays(7));
    const [endDate, setEndDate] = useState(initial?.endDate ?? plusDays(12));
    const [budget, setBudget] = useState(String(initial?.budget ?? 50000));
    const [currency, setCurrency] = useState(initial?.currency ?? 'RUB');
    const [status, setStatus] = useState<TripStatus>(initial?.status ?? 'Draft');
    const [travelers, setTravelers] = useState((initial?.travelers ?? []).join(', '));
    const [error, setError] = useState<string | null>(null);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        setError(null);
        if (endDate < startDate) { setError('Дата окончания раньше даты начала.'); return; }
        try {
            await onSubmit({
                title: title.trim(), city: city.trim(), country: country.trim(),
                description: description.trim() || null,
                startDate, endDate, budget: Number(budget), currency, status,
                travelers: travelers.split(',').map(value => value.trim()).filter(Boolean),
                version: initial?.version ?? null
            });
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Ошибка сохранения');
        }
    };

    return (
        <form className="form" onSubmit={submit}>
            <label>Название поездки<input value={title} onChange={e => setTitle(e.target.value)} required minLength={3} maxLength={120} placeholder="Санкт-Петербург: флот и крепости" /></label>
            <div className="grid-2">
                <label>Город<input value={city} onChange={e => setCity(e.target.value)} required maxLength={80} /></label>
                <label>Страна<input value={country} onChange={e => setCountry(e.target.value)} required maxLength={80} /></label>
            </div>
            <div className="grid-2">
                <label>Начало<input type="date" value={startDate} min={initial ? undefined : today()} onChange={e => setStartDate(e.target.value)} required /></label>
                <label>Окончание<input type="date" value={endDate} min={startDate} onChange={e => setEndDate(e.target.value)} required /></label>
            </div>
            <div className="grid-2">
                <label>Бюджет<input type="number" min={0} step={100} value={budget} onChange={e => setBudget(e.target.value)} required /></label>
                <label>Валюта
                    <select value={currency} onChange={e => setCurrency(e.target.value)}>
                        {(meta?.currencies ?? ['RUB', 'USD', 'EUR', 'CNY']).map(code => <option key={code} value={code}>{code}</option>)}
                    </select>
                </label>
            </div>
            {initial && (
                <label>Статус
                    <select value={status} onChange={e => setStatus(e.target.value as TripStatus)}>
                        {(meta?.tripStatuses ?? []).map(item => (
                            <option key={item.name} value={item.name}>{tripStatusLabels[item.name as TripStatus]}</option>
                        ))}
                    </select>
                </label>
            )}
            <label>Попутчики (через запятую)<input value={travelers} onChange={e => setTravelers(e.target.value)} placeholder="Александр, Анна" /></label>
            <label>Описание<textarea rows={3} value={description} onChange={e => setDescription(e.target.value)} maxLength={2000} /></label>

            {error && <p className="error">{error}</p>}
            <div className="form-actions">
                <button type="button" className="button secondary" onClick={onCancel}>Отмена</button>
                <button type="submit" className="button" disabled={submitting}>{submitting ? 'Сохранение…' : 'Сохранить'}</button>
            </div>
        </form>
    );
}