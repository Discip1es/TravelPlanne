import { useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { tokens } from '../api/client';
import type { AppNotification } from '../api/types';

/** Подключение к SignalR-хабу и накопление последних пяти уведомлений. */
export function useNotifications(enabled: boolean) {
    const [items, setItems] = useState<AppNotification[]>([]);
    const [connected, setConnected] = useState(false);

    useEffect(() => {
        if (!enabled) return;

        const connection = new HubConnectionBuilder()
            .withUrl('/hubs/notifications', { accessTokenFactory: () => tokens.access ?? '' })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        const handler = (notification: AppNotification) =>
            setItems(previous => [notification, ...previous].slice(0, 5));

        connection.on('notification', handler);
        connection.onreconnected(() => setConnected(true));
        connection.onclose(() => setConnected(false));
        connection.start().then(() => setConnected(true)).catch(() => setConnected(false));

        return () => {
            connection.off('notification', handler);
            void connection.stop();
        };
    }, [enabled]);

    return {
        items,
        connected,
        dismiss: (index: number) => setItems(previous => previous.filter((_, i) => i !== index))
    };
}