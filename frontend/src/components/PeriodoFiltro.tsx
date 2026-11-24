type Props = { inicio: string; fim: string; onInicio: (value: string) => void; onFim: (value: string) => void; onAplicar: () => void; carregando?: boolean };

export default function PeriodoFiltro({ inicio, fim, onInicio, onFim, onAplicar, carregando }: Props) {
  return <div className="periodFilter"><label>De<input type="date" value={inicio} onChange={(e) => onInicio(e.target.value)} /></label><label>Até<input type="date" value={fim} onChange={(e) => onFim(e.target.value)} /></label><button className="primary" onClick={onAplicar} disabled={carregando}>Aplicar período</button></div>;
}
