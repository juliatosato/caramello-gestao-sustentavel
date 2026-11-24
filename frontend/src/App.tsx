import { useEffect, useState } from 'react';
import { ArrowRight, BarChart3, Coffee, FileText, Package, Recycle, Sprout } from 'lucide-react';
import BorraPage from './pages/BorraPage';
import CompostagemPage from './pages/CompostagemPage';
import DashboardPage from './pages/DashboardPage';
import DesperdiciosPage from './pages/DesperdiciosPage';
import EstoquePage from './pages/EstoquePage';
import RelatoriosPage from './pages/RelatoriosPage';
import LoginPage from './components/LoginPage';
import ContaUsuario from './components/ContaUsuario';
import SetupPage from './components/SetupPage';
import { api } from './api';

type Pagina = 'dashboard' | 'estoque' | 'desperdicios' | 'borra' | 'compostagem' | 'relatorios';

const navegacao = [
  ['dashboard', 'Dashboard', BarChart3],
  ['estoque', 'Estoque', Package],
  ['desperdicios', 'Desperdícios', Recycle],
  ['borra', 'Borra de café', Coffee],
  ['relatorios', 'Relatórios', FileText],
] as const;

const titulos: Record<Pagina, [string, string]> = {
  dashboard: ['Indicadores de sustentabilidade', 'Consumo e descarte apresentados por unidade.'],
  estoque: ['Gestão de estoque', 'Saldos, movimentações e reposição transparente.'],
  desperdicios: ['Análise de desperdício', 'Acompanhe descartes vinculados aos insumos.'],
  borra: ['Borra de café', 'Geração e destinação responsável.'],
  compostagem: ['Guia de compostagem', 'Transforme parte dos resíduos orgânicos em composto com segurança.'],
  relatorios: ['Relatórios', 'Compare períodos e exporte dados verificáveis.'],
};

export default function App() {
  const [pagina, setPagina] = useState<Pagina>('dashboard');
  const [usuario, setUsuario] = useState<string | null>(null);
  const [verificando, setVerificando] = useState(true);
  const [configuracaoInicial, setConfiguracaoInicial] = useState(false);
  const [titulo, subtitulo] = titulos[pagina];

  useEffect(() => {
    api<{ necessario: boolean }>('/auth/setup-status').then(status => {
      setConfiguracaoInicial(status.necessario);
      if (!status.necessario) return api<{ email: string }>('/auth/me').then(x => setUsuario(x.email)).catch(() => setUsuario(null));
    }).catch(() => setUsuario(null)).finally(() => setVerificando(false));
    const expirada = () => setUsuario(null);
    window.addEventListener('caramello:sessao-expirada', expirada);
    return () => window.removeEventListener('caramello:sessao-expirada', expirada);
  }, []);

  if (verificando) return <main className="loginPage"><div className="notice loading">Verificando sessão…</div></main>;
  if (configuracaoInicial) return <SetupPage onConcluido={email => { setConfiguracaoInicial(false); setUsuario(email); }} />;
  if (!usuario) return <LoginPage onLogin={email => setUsuario(email)} />;

  return <div className="app">
    <aside>
      <div className="brand"><span className="brandIcon">☕</span><div><strong>caramello.</strong><small>GESTÃO SUSTENTÁVEL</small></div></div>
      <nav aria-label="Navegação principal">{navegacao.map(([chave, rotulo, Icone]) => <button key={chave} className={pagina === chave || (chave === 'borra' && pagina === 'compostagem') ? 'active' : ''} onClick={() => setPagina(chave)}><Icone size={18} />{rotulo}</button>)}</nav>
      <div className="asideBottom">Dados para decisões responsáveis.<br />ODS 12 · Consumo consciente</div>
    </aside>
    <main>
      <header><div><p className="eyebrow">CARAMELLO CAFÉ · PAINEL ADMINISTRATIVO</p><h1>{titulo}</h1><p className="sub">{subtitulo}</p></div><ContaUsuario email={usuario} onLogout={() => setUsuario(null)} /></header>
      {pagina === 'dashboard' && <DashboardPage />}
      {pagina === 'estoque' && <EstoquePage />}
      {pagina === 'desperdicios' && <DesperdiciosPage />}
      {pagina === 'borra' && <>
        <button type="button" className="compostGuideCard" onClick={() => setPagina('compostagem')}>
          <span className="compostGuideIcon"><Sprout size={28} /></span>
          <span className="compostGuideText"><small>GUIA PRÁTICO</small><strong>Compostagem na cafeteria</strong><span>Veja o que pode ser aproveitado e como começar com segurança.</span></span>
          <ArrowRight className="compostGuideArrow" aria-hidden="true" />
        </button>
        <BorraPage />
      </>}
      {pagina === 'compostagem' && <CompostagemPage onVoltar={() => setPagina('borra')} />}
      {pagina === 'relatorios' && <RelatoriosPage />}
      <footer>Caramello Café · Tecnologia a favor do consumo consciente</footer>
    </main>
  </div>;
}
