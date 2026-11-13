import { categoryColors, categoryLabels } from '../labels';
import type { ActivityCategory, NameValue } from '../api/types';

interface Props { data: NameValue[]; currency: string; }

/** Кольцевая диаграмма на чистом SVG — без внешних библиотек графиков. */
export default function BudgetDonut({ data, currency }: Props) {
    const total = data.reduce((sum, item) => sum + item.value, 0);
    const radius = 60;
    const circumference = 2 * Math.PI * radius;
    let offset = 0;

    if (total <= 0) return <p className="muted">Расходы ещё не запланированы.</p>;

    return (
        <div className="donut">
            <svg viewBox="0 0 160 160" width="160" height="160">
                <circle cx="80" cy="80" r={radius} fill="none" stroke="#1f2937" strokeWidth="20" />
                {data.map(item => {
                    const fraction = item.value / total;
                    const dash = fraction * circumference;
                    const element = (
                        <circle
                            key={item.name}
                            cx="80" cy="80" r={radius} fill="none"
                            stroke={categoryColors[item.name as ActivityCategory] ?? '#94a3b8'}
                            strokeWidth="20"
                            strokeDasharray={`${dash} ${circumference - dash}`}
                            strokeDashoffset={-offset}
                            transform="rotate(-90 80 80)"
                        />
                    );
                    offset += dash;
                    return element;
                })}
                <text x="80" y="76" textAnchor="middle" className="donut-total">{Math.round(total).toLocaleString('ru-RU')}</text>
                <text x="80" y="94" textAnchor="middle" className="donut-currency">{currency}</text>
            </svg>
            <ul className="legend">
                {data.map(item => (
                    <li key={item.name}>
                        <span className="dot" style={{ background: categoryColors[item.name as ActivityCategory] ?? '#94a3b8' }} />
                        <span>{categoryLabels[item.name as ActivityCategory] ?? item.name}</span>
                        <b>{Math.round((item.value / total) * 100)}%</b>
                    </li>
                ))}
            </ul>
        </div>
    );
}