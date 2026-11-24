using Caramello.Api.Domain;
using Caramello.Api.Dtos;

namespace Caramello.Api.Services;

public static class Validacao
{
    private static readonly HashSet<string> UnidadesPermitidas = ["kg", "g", "L", "ml", "un"];

    public static string? Validar(InsumoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Nome)) return "Informe o nome do insumo.";
        if (input.Nome.Trim().Length > 120) return "O nome deve ter no máximo 120 caracteres.";
        if (string.IsNullOrWhiteSpace(input.Categoria)) return "Informe a categoria do insumo.";
        if (input.Categoria.Trim().Length > 80) return "A categoria deve ter no máximo 80 caracteres.";
        if (!UnidadesPermitidas.Contains(input.Unidade)) return "Informe uma unidade de medida válida.";
        if (input.Quantidade < 0) return "A quantidade não pode ser negativa.";
        if (input.EstoqueMinimo < 0) return "O estoque mínimo não pode ser negativo.";
        if (input.PrazoReposicaoDias < 0 || input.PrazoReposicaoDias > 365)
            return "O prazo de reposição deve estar entre 0 e 365 dias.";
        if (input.EstoqueSeguranca < 0) return "O estoque de segurança não pode ser negativo.";
        if (!TemAteTresCasasDecimais(input.Quantidade) || !TemAteTresCasasDecimais(input.EstoqueMinimo) || !TemAteTresCasasDecimais(input.EstoqueSeguranca))
            return "As quantidades devem ter no máximo três casas decimais.";
        return null;
    }

    public static string? Validar(InsumoEdicaoInput input)
    {
        var cadastro = new InsumoInput(input.Nome, input.Categoria, input.Unidade, 0, input.EstoqueMinimo, input.PrazoReposicaoDias, input.EstoqueSeguranca);
        return Validar(cadastro);
    }

    public static string? Validar(RegistroBorraInput input)
    {
        if (!Enum.IsDefined(input.Tipo)) return "Informe um tipo de registro válido.";
        if (input.Quantidade <= 0) return "A quantidade deve ser maior que zero.";
        if (!TemAteTresCasasDecimais(input.Quantidade)) return "A quantidade deve ter no máximo três casas decimais.";
        if (input.Unidade is not ("kg" or "g")) return "A borra deve ser registrada em kg ou g.";
        if (input.Tipo == TipoRegistroBorra.Destinada && string.IsNullOrWhiteSpace(input.Destinacao))
            return "Informe a forma de destinação.";
        if (input.Destinacao?.Trim().Length > 120) return "A destinação deve ter no máximo 120 caracteres.";
        if (input.Observacao?.Trim().Length > 300) return "A observação deve ter no máximo 300 caracteres.";
        if (input.Data == default) return "Informe uma data válida.";
        if (input.Data.ToUniversalTime() > DateTime.UtcNow.AddDays(1)) return "A data não pode estar no futuro.";
        return null;
    }

    public static string? Validar(MovimentacaoInput input)
    {
        if (!Enum.IsDefined(input.Tipo)) return "Informe um tipo de movimentação válido.";
        if (input.Quantidade <= 0) return "A quantidade deve ser maior que zero.";
        if (!TemAteTresCasasDecimais(input.Quantidade))
            return "A quantidade deve ter no máximo três casas decimais.";
        if (input.Motivo?.Trim().Length > 300) return "O motivo deve ter no máximo 300 caracteres.";
        if (input.Tipo is TipoMovimentacao.Consumo or TipoMovimentacao.Descarte && string.IsNullOrWhiteSpace(input.Motivo))
            return "Informe o motivo da saída.";
        return null;
    }

    public static string? Validar(DescarteInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Material)) return "Informe o material descartado.";
        if (input.Material.Trim().Length > 120) return "O material deve ter no máximo 120 caracteres.";
        if (!UnidadesPermitidas.Contains(input.Unidade)) return "Informe uma unidade de medida válida.";
        if (input.Quantidade <= 0) return "A quantidade deve ser maior que zero.";
        if (input.Motivo?.Trim().Length > 300) return "O motivo deve ter no máximo 300 caracteres.";
        return null;
    }

    private static bool TemAteTresCasasDecimais(decimal valor) =>
        decimal.Round(valor, 3, MidpointRounding.ToEven) == valor;
}
