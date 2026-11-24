type Item = { rotulo: string; valor: number; unidade: string };

export default function GraficoBarras({ itens, vazio }: { itens: Item[]; vazio: string }) {
  if (!itens.length) return <div className="emptyState">{vazio}</div>;
  const maximo = Math.max(...itens.map((item) => item.valor), 1);
  return <div className="barChart">{itens.map((item, index) => <div className="barRow" key={`${item.rotulo}-${item.unidade}-${index}`}><div className="barLabel"><span>{item.rotulo}</span><strong>{item.valor.toLocaleString('pt-BR')} {item.unidade}</strong></div><div className="barTrack"><div className="barFill" style={{ width: `${(item.valor / maximo) * 100}%` }} /></div></div>)}</div>;
}
