import type { ReactNode } from 'react';

interface Props { title: string; children: ReactNode; onClose: () => void; }

export default function Modal({ title, children, onClose }: Props) {
    return (
        <div className="modal-backdrop" onClick={onClose}>
            <div className="modal" onClick={event => event.stopPropagation()}>
                <header className="modal-head">
                    <h3>{title}</h3>
                    <button className="icon-button" onClick={onClose} aria-label="Закрыть">✕</button>
                </header>
                <div className="modal-body">{children}</div>
            </div>
        </div>
    );
}