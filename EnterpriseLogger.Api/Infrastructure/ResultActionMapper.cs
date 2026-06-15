using EnterpriseLogger.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Infrastructure;

public static class ResultActionMapper
{
    public static ActionResult<Result<T>> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result);

        return new ObjectResult(result) { StatusCode = (int)result.ErrorKind };
    }
}
