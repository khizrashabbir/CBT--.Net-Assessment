using System.Net;

namespace KoperasiTentera.Application.Common;

/// <summary>
/// Domain-level exception carrying a stable error code and HTTP status
/// so the global exception middleware can translate it into a ProblemDetails response.
/// </summary>
public class AppException : Exception
{
    public string Code { get; }

    public HttpStatusCode StatusCode { get; }

    public object? Details { get; }

    public AppException(string code, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, object? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }
}
