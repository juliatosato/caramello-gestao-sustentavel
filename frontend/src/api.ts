let csrfToken = '';

async function obterCsrf() {
  if (csrfToken) return csrfToken;
  const response = await fetch('/api/auth/csrf', { credentials: 'same-origin' });
  if (!response.ok) throw new Error('Não foi possível iniciar a validação de segurança.');
  const body = await response.json() as { token: string };
  csrfToken = body.token;
  return csrfToken;
}

export function renovarCsrf() { csrfToken = ''; }

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase();
  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) headers.set('X-CSRF-TOKEN', await obterCsrf());

  const response = await fetch(`/api${path}`, { ...options, headers, credentials: 'same-origin' });
  if (response.status === 401 && path !== '/auth/login') window.dispatchEvent(new Event('caramello:sessao-expirada'));
  if (!response.ok) {
    const text = await response.text();
    try {
      const body = JSON.parse(text) as { mensagem?: string };
      throw new Error(body.mensagem || 'Não foi possível concluir a operação.');
    } catch (error) {
      if (error instanceof SyntaxError) throw new Error(text || 'Não foi possível concluir a operação.');
      throw error;
    }
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export function query(params: Record<string, string | number | undefined>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value));
  });
  return search.toString();
}
