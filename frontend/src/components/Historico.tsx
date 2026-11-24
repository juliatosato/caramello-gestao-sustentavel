import { ArrowDownCircle, ArrowUpCircle } from 'lucide-react';
import type { Movimentacao } from '../types';

export default function Historico({ itens }: { itens: Movimentacao[] }) {
  if (!itens.length) return <div className="emptyState">Nenhuma movimentação no período.</div>;
  return <div className="tablewrap"><table><thead><tr><th>Data</th><th>Insumo</th><th>Tipo</th><th>Quantidade</th><th>Motivo</th></tr></thead><tbody>{itens.map((item) => <tr key={item.id}><td>{new Date(item.data).toLocaleString('pt-BR')}</td><td>{item.insumo}</td><td><span className={`movement ${item.tipo.toLowerCase()}`}>{item.tipo === 'Entrada' ? <ArrowUpCircle size={15} /> : <ArrowDownCircle size={15} />}{item.tipo}</span></td><td>{item.quantidade.toLocaleString('pt-BR')} {item.unidade}</td><td>{item.motivo || '—'}</td></tr>)}</tbody></table></div>;
}
