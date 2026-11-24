import { FormEvent, useState } from 'react';
import { LockKeyhole } from 'lucide-react';
import { api, renovarCsrf } from '../api';

export default function LoginPage({ onLogin }: { onLogin: (email: string) => void }) {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(false);
  async function entrar(event: FormEvent) {
    event.preventDefault(); setErro(''); setCarregando(true);
    try {
      renovarCsrf();
      await api('/auth/login', { method: 'POST', body: JSON.stringify({ email, senha }) });
      renovarCsrf();
      onLogin(email);
    } catch (e) { setErro(e instanceof Error ? e.message : 'Não foi possível entrar.'); }
    finally { setCarregando(false); }
  }
  return <main className="loginPage"><section className="loginCard"><div className="loginBrand"><span className="brandIcon">☕</span><div><strong>caramello.</strong><small>GESTÃO SUSTENTÁVEL</small></div></div><div className="loginIcon"><LockKeyhole /></div><h1>Acesso administrativo</h1><p>Entre com sua conta para acessar os dados da cafeteria.</p>{erro && <div className="notice error" role="alert">{erro}</div>}<form className="loginForm" onSubmit={e => void entrar(e)}><label>E-mail<input type="email" autoComplete="username" required value={email} onChange={e => setEmail(e.target.value)} /></label><label>Senha<input type="password" autoComplete="current-password" required value={senha} onChange={e => setSenha(e.target.value)} /></label><button className="primary" disabled={carregando}>{carregando ? 'Entrando…' : 'Entrar'}</button></form><small className="loginHelp">Não há cadastro público. Contas são administradas localmente.</small></section></main>;
}
