import { activityStatusLabels, tripStatusLabels } from '../labels';
import type { ActivityStatus, TripStatus } from '../api/types';

export function TripStatusBadge({ status }: { status: TripStatus }) {
    return <span className={`badge status-${status}`}>{tripStatusLabels[status]}</span>;
}

export function ActivityStatusBadge({ status }: { status: ActivityStatus }) {
    return <span className={`badge activity-${status}`}>{activityStatusLabels[status]}</span>;
}