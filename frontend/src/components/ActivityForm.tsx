import { useState } from 'react';
import type { ActivityCategory, ActivityItem, ActivityStatus, ActivityWriteModel, Meta, TripDetails } from '../api/types';
import { activityStatusLabels, categoryLabels } from '../labels';

interface Props {
    trip: TripDetails;
    initial?: ActivityItem | null;
    meta: Meta | null;
    submitting: boolean;
    onSubmit: (model: ActivityWriteModel) => Promise<void>;
    onCancel: () => void;
}

export default function ActivityForm({ trip, initial, meta, submitting, onSubmit, onCancel }: Props) {
    const [title, setTitle] = useState(initial?.title ?? '');
    const [category, setCategory] = useState<ActivityCategory>(initial?.category ?? 'Museum');
    const [status, setStatus] = useState<ActivityStatus>(initial?.status ?? 'Planned');
    const [date, setDate] = useState(initial?.date ?? trip.startDate);
    const [startTime, setStartTime] = useState(initial?.startTime ?? '10:00');
    const [durationMinutes, setDurationMinutes] = useState(String(initial?.durationMinutes ?? 90));
    const [cost, setCost] = useState(String(initial?.cost ?? 0));
    const [location, setLocation] = useState(initial?.location ?? '');
    const [notes, setNotes] = useState(initial?.notes ?? '');
    const [error, setError] = useState<string | null>(null);

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        setError(null);
        if (date < trip.startDate || date > trip.endDate) {
            setError(`Дата должна быть в диапазоне ${trip.startDate} — ${trip.endDate}.`);
            return;
        }
        try {
            await onSubmit({
                title: title.trim(), category, status, date,
                startTime: startTime || null,
                durationMinutes: Number(durationMinutes),
                cost: Number(cost),
                location: location.trim() || null,
                notes: notes.trim() || null,
                order: initial?.order ?? 0
            });
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Ошибка сохранения');
        }
    };

    return (
        <form className="form" onSubmit={submit}>
            <label>Название<input value={title} onChange={e => setTitle(e.target.value)} required minLength={2} maxLength={160} placeholder="Прогулка на катере по Неве" /></label>
            <div className="grid-2">
                <label>Категория
                    <select value={category} onChange={e => setCategory(e.target.value as ActivityCategory)}>
                        {(meta?.activityCategories ?? []).map(item => (
                            <option key={item.name} value={item.name}>{categoryLabels[item.name as ActivityCategory]}</option>
                        ))}
                    </select>
                </label>
                <label>Статус
                    <select value={status} onChange={e => setStatus(e.target.value as ActivityStatus)}>
                        {(meta?.activityStatuses ?? []).map(item => (
                            <option key={item.name} value={item.name}>{activityStatusLabels[item.name as ActivityStatus]}</option>
                        ))}
                    </select>
                </label>
            </div>
            <div className="grid-3">
                <label>Дата<input type="date" value={date} min={trip.startDate} max={trip.endDate} onChange={e => setDate(e.target.value)} required /></label>
                <label>Время<input type="time" value={startTime} onChange={e => setStartTime(e.target.value)} /></label>
                <label>Длительность, мин<input type="number" min={5} max={1440} step={5} value={durationMinutes} onChange={e => setDurationMinutes(e.target.value)} /></label>
            </div>
            <div className="grid-2">
                <label>Стоимость ({trip.currency})<input type="number" min={0} step={50} value={cost} onChange={e => setCost(e.target.value)} /></label>
                <label>Место<input value={location} onChange={e => setLocation(e.target.value)} maxLength={200} /></label>
            </div>
            <label>Заметки<textarea rows={2} value={notes} onChange={e => setNotes(e.target.value)} maxLength={1000} /></label>

            {error && <p className="error">{error}</p>}
            <div className="form-actions">
                <button type="button" className="button secondary" onClick={onCancel}>Отмена</button>
                <button type="submit" className="button" disabled={submitting}>{submitting ? 'Сохранение…' : 'Сохранить'}</button>
            </div>
        </form>
    );
}