// Типы полностью соответствуют DTO бэкенда (camelCase — политика System.Text.Json по умолчанию).
export type TripStatus = 'Draft' | 'Planned' | 'Booked' | 'InProgress' | 'Completed' | 'Cancelled';
export type ActivityCategory =
    | 'Transport' | 'Accommodation' | 'Food' | 'Museum' | 'SeaTrip' | 'Extreme' | 'Walk' | 'Shopping' | 'Other';
export type ActivityStatus = 'Planned' | 'Booked' | 'Done' | 'Cancelled';

export interface User { id: string; email: string; displayName: string; role: 'User' | 'Admin'; }

export interface AuthResponse { accessToken: string; refreshToken: string; expiresAtUtc: string; user: User; }

export interface NameValue { name: string; value: number; }

export interface ActivityItem {
    id: string; title: string; category: ActivityCategory; status: ActivityStatus;
    date: string; startTime: string | null; durationMinutes: number;
    cost: number; location: string | null; notes: string | null; order: number;
}

export interface TripListItem {
    id: string; title: string; city: string; country: string;
    startDate: string; endDate: string; status: TripStatus;
    budget: number; currency: string; plannedCost: number; remaining: number; budgetUsagePercent: number;
    durationDays: number; activitiesCount: number; travelersCount: number;
}

export interface TripDetails {
    id: string; title: string; city: string; country: string; description: string | null;
    startDate: string; endDate: string; status: TripStatus;
    budget: number; currency: string; plannedCost: number; bookedCost: number; remaining: number; budgetUsagePercent: number;
    durationDays: number; travelers: string[]; activities: ActivityItem[]; costByCategory: NameValue[];
    version: string; createdAtUtc: string; updatedAtUtc: string;
}

export interface TripWriteModel {
    title: string; city: string; country: string; description: string | null;
    startDate: string; endDate: string; budget: number; currency: string;
    status: TripStatus; travelers: string[]; version?: string | null;
}

export interface ActivityWriteModel {
    title: string; category: ActivityCategory; status: ActivityStatus;
    date: string; startTime: string | null; durationMinutes: number;
    cost: number; location: string | null; notes: string | null; order: number;
}

export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number; }

export interface Dashboard {
    baseCurrency: string;
    totalTrips: number; upcomingTrips: number; inProgressTrips: number; completedTrips: number;
    totalBudget: number; totalPlannedCost: number; totalBookedCost: number;
    tripsByStatus: NameValue[]; costByCategory: NameValue[]; nextTrips: TripListItem[];
}

export interface AppNotification {
    type: string; title: string; message: string; tripId: string | null; createdAtUtc: string;
}

export interface EnumItem { name: string; value: number; }

export interface Meta {
    tripStatuses: EnumItem[]; activityCategories: EnumItem[]; activityStatuses: EnumItem[]; currencies: string[];
}

export interface Conversion { amount: number; from: string; to: string; rate: number; converted: number; }

export interface ProblemDetails {
    title?: string; detail?: string; status?: number; errors?: Record<string, string[]>;
}

export interface TripQueryParams {
    search?: string; status?: string; city?: string; from?: string; to?: string;
    sortBy?: string; desc?: boolean; page?: number; pageSize?: number;
}