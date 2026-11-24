namespace Caramello.Api.Services;

public class RegraNegocioException(string message) : InvalidOperationException(message);
