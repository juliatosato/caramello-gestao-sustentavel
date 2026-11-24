export default function Estado({ carregando, erro, sucesso }: { carregando?: boolean; erro?: string; sucesso?: string }) {
  return <>{carregando && <div className="notice loading" role="status">Carregando dados…</div>}{erro && <div className="notice error" role="alert">{erro}</div>}{sucesso && <div className="notice success" role="status">{sucesso}</div>}</>;
}
