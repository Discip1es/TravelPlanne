import { ApiError } from './api/client';
import type { ActivityCategory, ActivityStatus, TripStatus } from './api/types';

export const tripStatusLabels: Record<TripStatus, string> = {
    Draft: 'Черновик', Planned: 'Запланирована', Booked: 'Забронирована',
    InProgress: 'В поездке', Completed: 'Завершена', Cancelled: 'Отменена'
};

export const activityStatusLabels: Record<ActivityStatus, string> = {
    Planned: 'Запланировано', Booked: 'Забронировано', Done: 'Выполнено', Cancelled: 'Отменено'
};

export const categoryLabels: Record<ActivityCategory, string> = {
    Transport: 'Транспорт', Accommodation: 'Проживание', Food: 'Еда', Museum: 'Музеи',
    SeaTrip: 'Морские прогулки', Extreme: 'Экстрим', Walk: 'Прогулки', Shopping: 'Покупки', Other: 'Прочее'
};

export const categoryColors: Record<ActivityCategory, string> = {
    Transport: '#60a5fa', Accommodation: '#a78bfa', Food: '#fbbf24', Museum: '#34d399',
    SeaTrip: '#22d3ee', Extreme: '#f87171', Walk: '#c084fc', Shopping: '#fb923c', Other: '#94a3b8'
};

export const money = (value: number, currency: string) =>
    new Intl.NumberFormat('ru-RU', { style: 'currency', currency, maximumFractionDigits: 0 }).format(value);

/** Дата приходит как 'yyyy-MM-dd'; добавляем время, чтобы избежать сдвига часового пояса. */
export const formatDate = (iso: string) =>
    new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: 'short', year: 'numeric' })
        .format(new Date(`${iso}T00:00:00`));

export const errorMessage = (error: unknown) =>
    error instanceof ApiError ? error.message : 'Непредвиденная ошибка. Повторите попытку.';