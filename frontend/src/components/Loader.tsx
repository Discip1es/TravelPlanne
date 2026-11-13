export default function Loader({ text = 'Загрузка…' }: { text?: string }) {
    return (
        <div className="loader">
            <span className="spinner" />
            <span>{text}</span>
        </div>
    );
}