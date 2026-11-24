import { useEffect, useState } from 'react';
import { Download } from 'lucide-react';
import { api, query } from '../api';
import Estado from '../components/Estado';
import PeriodoFiltro from '../components/PeriodoFiltro';
import { dataInput, inicioPadrao } from '../data';
import type { Comparacao, TotalUnidade } from '../types';

function Totais({ titulo, itens }: { titulo: string; itens: TotalUnidade[] }) { return <div className="compareBlock"><strong>{titulo}</strong>{itens.length ? itens.map(x => <span key={x.unidade}>{x.quantidade.toLocaleString('pt-BR')} {x.unidade}</span>) : <span>Sem registros</span>}</div>; }

export default function RelatoriosPage() {
  const [inicio, setInicio] = useState(inicioPadrao()); const [fim, setFim] = useState(dataInput()); const [dados, setDados] = useState<Comparacao | null>(null); const [carregando, setCarregando] = useState(true); const [erro, setErro] = useState('');
  async function carregar() { setCarregando(true); setErro(''); try { setDados(await api(`/relatorios/comparacao?${query({ inicio, fim })}`)); } catch (e) { setErro(e instanceof Error ? e.message : 'Erro ao carregar relatório.'); } finally { setCarregando(false); } }
  useEffect(() => { void carregar(); }, []);
  const params = query({ inicio, fim });
  return <><PeriodoFiltro inicio={inicio} fim={fim} onInicio={setInicio} onFim={setFim} onAplicar={() => void carregar()} carregando={carregando} /><Estado carregando={carregando} erro={erro} />{dados && <><section className="panel"><h2>Comparação entre períodos equivalentes</h2><p className="helper">Os totais permanecem separados por unidade. O sistema não calcula redução percentual sem uma linha de base validada.</p><div className="comparison"><div><h3>Período atual</h3><p>{new Date(dados.periodoAtual.inicio).toLocaleDateString('pt-BR')} a {new Date(dados.periodoAtual.fim).toLocaleDateString('pt-BR')}</p><Totais titulo="Consumo" itens={dados.periodoAtual.consumo} /><Totais titulo="Descarte" itens={dados.periodoAtual.descartes} /></div><div><h3>Período anterior</h3><p>{new Date(dados.periodoAnterior.inicio).toLocaleDateString('pt-BR')} a {new Date(dados.periodoAnterior.fim).toLocaleDateString('pt-BR')}</p><Totais titulo="Consumo" itens={dados.periodoAnterior.consumo} /><Totais titulo="Descarte" itens={dados.periodoAnterior.descartes} /></div></div></section><section className="panel"><h2>Exportar CSV</h2><p className="helper">Arquivos UTF-8 com separador por ponto e vírgula, datas, categorias e unidades, preparados para Excel em português.</p><div className="downloadActions"><a className="primary" href={`/api/relatorios/movimentacoes.csv?${params}`}><Download size={16} /> Movimentações</a><a className="outline" href={`/api/relatorios/indicadores.csv?${params}`}><Download size={16} /> Indicadores</a></div></section></>}</>;
}
