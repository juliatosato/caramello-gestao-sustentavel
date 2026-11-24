import { FormEvent, useEffect, useRef, useState } from 'react';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { api } from '../api';
import Estado from '../components/Estado';
import Historico from '../components/Historico';
import type { Insumo, Movimentacao, Sugestao } from '../types';

const unidades = ['kg', 'g', 'L', 'ml', 'un'];
const formVazio = {
  nome: '', categoria: '', unidade: 'kg', quantidade: '', estoqueMinimo: '',
  prazoReposicaoDias: '0', estoqueSeguranca: '0',
};

export default function EstoquePage() {
  const [insumos, setInsumos] = useState<Insumo[]>([]);
  const [movimentos, setMovimentos] = useState<Movimentacao[]>([]);
  const [sugestoes, setSugestoes] = useState<Sugestao[]>([]);
  const [editando, setEditando] = useState<Insumo | null>(null);
  const [form, setForm] = useState(formVazio);
  const [mov, setMov] = useState({ insumoId: '', tipo: 'Entrada', quantidade: '', motivo: '' });
  const [carregando, setCarregando] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const formularioRef = useRef<HTMLElement>(null);

  async function carregar() {
    setCarregando(true);
    try {
      const [lista, historico, reposicao] = await Promise.all([
        api<Insumo[]>('/insumos'),
        api<Movimentacao[]>('/movimentacoes'),
        api<Sugestao[]>('/sugestoes-reposicao?dias=30'),
      ]);
      setInsumos(lista);
      setMovimentos(historico);
      setSugestoes(reposicao);
      setErro('');
    } catch (error) {
      setErro(error instanceof Error ? error.message : 'Erro ao carregar estoque.');
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => { void carregar(); }, []);

  function limpar() {
    setEditando(null);
    setForm(formVazio);
  }

  function editar(item: Insumo) {
    setEditando(item);
    setForm({
      nome: item.nome,
      categoria: item.categoria,
      unidade: item.unidade,
      quantidade: '',
      estoqueMinimo: String(item.estoqueMinimo),
      prazoReposicaoDias: String(item.prazoReposicaoDias),
      estoqueSeguranca: String(item.estoqueSeguranca),
    });
    requestAnimationFrame(() => {
      formularioRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      formularioRef.current?.querySelector<HTMLInputElement>('input')?.focus({ preventScroll: true });
    });
  }

  async function executar(acao: () => Promise<unknown>, mensagem: string) {
    setSalvando(true);
    setErro('');
    setSucesso('');
    try {
      await acao();
      setSucesso(mensagem);
      await carregar();
      return true;
    } catch (error) {
      setErro(error instanceof Error ? error.message : 'Erro ao salvar.');
      return false;
    } finally {
      setSalvando(false);
    }
  }

  async function salvarInsumo(event: FormEvent) {
    event.preventDefault();
    const corpo = {
      nome: form.nome,
      categoria: form.categoria,
      unidade: form.unidade,
      estoqueMinimo: Number(form.estoqueMinimo),
      prazoReposicaoDias: Number(form.prazoReposicaoDias),
      estoqueSeguranca: Number(form.estoqueSeguranca),
      ...(editando ? {} : { quantidade: Number(form.quantidade) }),
    };
    const salvo = await executar(
      () => api(editando ? `/insumos/${editando.id}` : '/insumos', {
        method: editando ? 'PUT' : 'POST', body: JSON.stringify(corpo),
      }),
      editando ? 'Insumo atualizado.' : 'Insumo cadastrado.',
    );
    if (salvo) limpar();
  }

  async function movimentar(event: FormEvent) {
    event.preventDefault();
    const salvo = await executar(
      () => api(`/insumos/${mov.insumoId}/movimentacoes`, {
        method: 'POST',
        body: JSON.stringify({ tipo: mov.tipo, quantidade: Number(mov.quantidade), motivo: mov.motivo }),
      }),
      'Movimentação registrada e saldo atualizado.',
    );
    if (salvo) setMov({ ...mov, quantidade: '', motivo: '' });
  }

  async function excluir(item: Insumo) {
    if (!window.confirm(`Remover “${item.nome}” do estoque? O histórico será preservado.`)) return;
    const removido = await executar(
      () => api<void>(`/insumos/${item.id}`, { method: 'DELETE' }),
      'Item removido do estoque.',
    );
    if (removido && editando?.id === item.id) limpar();
  }

  return <>
    <Estado carregando={carregando} erro={erro} sucesso={sucesso} />
    {!carregando && <>
      <section className="panel" ref={formularioRef}>
        <h2>{editando ? 'Editar insumo e reposição' : 'Cadastrar insumo'}</h2>
        <form onSubmit={(event) => void salvarInsumo(event)}>
          <label>Nome<input required maxLength={120} value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} /></label>
          <label>Categoria<input required maxLength={80} value={form.categoria} onChange={(e) => setForm({ ...form, categoria: e.target.value })} /></label>
          <label>Unidade<select value={form.unidade} onChange={(e) => setForm({ ...form, unidade: e.target.value })}>{unidades.map((x) => <option key={x}>{x}</option>)}</select></label>
          {!editando && <label>Quantidade inicial<input required type="number" min="0" step="0.001" value={form.quantidade} onChange={(e) => setForm({ ...form, quantidade: e.target.value })} /></label>}
          <label>Estoque mínimo<input required type="number" min="0" step="0.001" value={form.estoqueMinimo} onChange={(e) => setForm({ ...form, estoqueMinimo: e.target.value })} /></label>
          <label>Prazo de reposição (dias)<input required type="number" min="0" max="365" value={form.prazoReposicaoDias} onChange={(e) => setForm({ ...form, prazoReposicaoDias: e.target.value })} /></label>
          <label>Estoque de segurança<input required type="number" min="0" step="0.001" value={form.estoqueSeguranca} onChange={(e) => setForm({ ...form, estoqueSeguranca: e.target.value })} /></label>
          <div className="formActions"><button className="primary" disabled={salvando}>{editando ? 'Salvar alterações' : 'Cadastrar'}</button>{editando && <button type="button" className="outline" onClick={limpar}>Cancelar</button>}</div>
        </form>
      </section>

      <section className="panel">
        <h2>Registrar movimentação</h2>
        {insumos.length ? <form onSubmit={(event) => void movimentar(event)}>
          <label>Insumo<select required value={mov.insumoId} onChange={(e) => setMov({ ...mov, insumoId: e.target.value })}><option value="">Selecione</option>{insumos.map((x) => <option key={x.id} value={x.id}>{x.nome} · {x.quantidade} {x.unidade}</option>)}</select></label>
          <label>Tipo<select value={mov.tipo} onChange={(e) => setMov({ ...mov, tipo: e.target.value })}><option>Entrada</option><option value="Consumo">Saída por consumo</option><option value="Descarte">Saída por descarte</option></select></label>
          <label>Quantidade<input required type="number" min="0.001" step="0.001" value={mov.quantidade} onChange={(e) => setMov({ ...mov, quantidade: e.target.value })} /></label>
          <label>Motivo<input required={mov.tipo !== 'Entrada'} maxLength={300} value={mov.motivo} onChange={(e) => setMov({ ...mov, motivo: e.target.value })} /></label>
          <div className="formActions"><button className="primary" disabled={salvando}><Plus size={16} /> Registrar</button></div>
        </form> : <div className="emptyState">Cadastre um insumo primeiro.</div>}
      </section>

      <section className="panel">
        <h2>Saldos e parâmetros</h2>
        {insumos.length ? <div className="tablewrap"><table><thead><tr><th>Insumo</th><th>Categoria</th><th>Saldo</th><th>Mínimo</th><th>Reposição</th><th><span className="srOnly">Ações</span></th></tr></thead><tbody>{insumos.map((item) => <tr key={item.id} className={item.quantidade <= item.estoqueMinimo ? 'lowStock' : ''}><td>{item.nome}</td><td>{item.categoria}</td><td><strong>{item.quantidade.toLocaleString('pt-BR')} {item.unidade}</strong></td><td>{item.estoqueMinimo.toLocaleString('pt-BR')} {item.unidade}</td><td>{item.prazoReposicaoDias} dias + {item.estoqueSeguranca} {item.unidade}</td><td><div className="rowActions"><button className="iconButton" onClick={() => editar(item)} aria-label={`Editar ${item.nome}`} title="Editar"><Pencil size={16} /></button><button className="iconButton danger" onClick={() => void excluir(item)} aria-label={`Excluir ${item.nome}`} title="Excluir"><Trash2 size={16} /></button></div></td></tr>)}</tbody></table></div> : <div className="emptyState">Nenhum insumo cadastrado.</div>}
      </section>

      <section className="panel"><h2>Sugestões de reposição</h2><p className="helper">Cálculo transparente com consumo dos últimos 30 dias. Nenhuma compra é realizada automaticamente.</p>{sugestoes.length ? <div className="suggestions">{sugestoes.map((item) => <article key={item.insumoId}><strong>{item.insumo}</strong><span>{item.mensagem}</span>{item.historicoSuficiente && <b>{item.quantidadeSugerida?.toLocaleString('pt-BR')} {item.unidade} sugeridos</b>}<small>{item.metodologia}</small></article>)}</div> : <div className="emptyState">Nenhum insumo para analisar.</div>}</section>
      <section className="panel"><h2>Histórico de estoque</h2><Historico itens={movimentos} /></section>
    </>}
  </>;
}
