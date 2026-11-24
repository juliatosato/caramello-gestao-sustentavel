import { useEffect, useState } from 'react';
import { History, Package, Trash2, TriangleAlert } from 'lucide-react';
import { api, query } from '../api';
import Estado from '../components/Estado';
import GraficoBarras from '../components/GraficoBarras';
import PeriodoFiltro from '../components/PeriodoFiltro';
import { dataInput, inicioPadrao } from '../data';
import type { Dashboard } from '../types';

export default function DashboardPage() {
  const [inicio, setInicio] = useState(inicioPadrao());
  const [fim, setFim] = useState(dataInput());
  const [dados, setDados] = useState<Dashboard | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  async function carregar() {
    setCarregando(true); setErro('');
    try { setDados(await api(`/indicadores?${query({ inicio, fim })}`)); }
    catch (e) { setErro(e instanceof Error ? e.message : 'Erro ao carregar indicadores.'); }
    finally { setCarregando(false); }
  }
  useEffect(() => { void carregar(); }, []);

  return <><PeriodoFiltro inicio={inicio} fim={fim} onInicio={setInicio} onFim={setFim} onAplicar={() => void carregar()} carregando={carregando} /><Estado carregando={carregando} erro={erro} />{dados && <><section className="stats"><article><div className="statIcon"><Trash2 /></div><small>Descarte por unidade</small><strong>{dados.descartes.length}</strong><span>{dados.descartes.length ? dados.descartes.map(x => `${x.quantidade.toLocaleString('pt-BR')} ${x.unidade}`).join(' · ') : 'Sem registros'}</span></article><article><div className="statIcon"><History /></div><small>Consumo por unidade</small><strong>{dados.consumo.length}</strong><span>{dados.consumo.length ? dados.consumo.map(x => `${x.quantidade.toLocaleString('pt-BR')} ${x.unidade}`).join(' · ') : 'Sem registros'}</span></article><article><div className="statIcon"><TriangleAlert /></div><small>Estoque baixo</small><strong>{dados.estoqueBaixo.length}</strong><span>Insumos abaixo do mínimo</span></article></section><div className="grid2"><section className="panel"><h2>Descartes por categoria</h2><GraficoBarras itens={dados.descartesPorCategoria.map(x => ({ rotulo: x.rotulo, valor: x.quantidade, unidade: x.unidade }))} vazio="Nenhum descarte no período." /></section><section className="panel"><h2>Descartes por motivo</h2><GraficoBarras itens={dados.descartesPorMotivo.map(x => ({ rotulo: x.rotulo, valor: x.quantidade, unidade: x.unidade }))} vazio="Nenhum motivo registrado no período." /></section></div><section className="panel"><h2>Alertas de estoque</h2>{dados.estoqueBaixo.length ? <div className="alerts">{dados.estoqueBaixo.map(item => <div className="alertItem" key={item.id}><Package size={18} /><span><strong>{item.nome}</strong> · {item.categoria}<br />Saldo {item.saldo.toLocaleString('pt-BR')} {item.unidade} · mínimo {item.minimo.toLocaleString('pt-BR')} {item.unidade}</span></div>)}</div> : <div className="emptyState">Nenhum insumo abaixo do estoque mínimo.</div>}</section></>}</>;
}
