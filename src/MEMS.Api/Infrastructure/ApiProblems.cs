using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using MEMS.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Api.Infrastructure;

public sealed class ApiProblems(RequestDelegate next, ILogger<ApiProblems> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"]="nosniff";
        context.Response.Headers["Referrer-Policy"]="no-referrer";
        context.Response.Headers["X-Frame-Options"]="DENY";
        context.Response.Headers.CacheControl="no-store";
        try
        {
            if (!context.Request.IsHttps && (context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method)))
                throw new ApiFault(400,"Use HTTPS for authentication and modifying requests.");
            await next(context);
            if(context.Response.StatusCode>=400 && !context.Response.HasStarted && string.IsNullOrEmpty(context.Response.ContentType))
                await WriteAsync(context,context.Response.StatusCode,Title(context.Response.StatusCode));
        }
        catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested) { }
        catch(Exception exception) when(!context.Response.HasStarted)
        {
            var (status,detail,errors)=exception switch
            {
                ApiFault x => (x.Status,x.Message,x.Errors),
                DbUpdateConcurrencyException => (409,"The record changed. Reload it and retry.",null),
                DbUpdateException => (409,"The change conflicts with existing records or dependent data.",null),
                ValidationException => (400,"The request contains invalid values.",null),
                BadHttpRequestException x => (x.StatusCode,x.StatusCode==413?"The request is too large.":"The request could not be read.",null),
                _ => (500,"An unexpected error occurred. Use the trace ID when reporting this problem.",null)
            };
            // Never log submitted values, cookies/tokens, raw SQL exception messages or filesystem paths.
            logger.Log(status>=500?LogLevel.Error:LogLevel.Information,"API request failed with status {Status}; error type {ErrorType}; trace {TraceId}",status,exception.GetType().Name,context.TraceIdentifier);
            if(status>=500)Activity.Current?.SetStatus(ActivityStatusCode.Error);
            context.Response.Clear();
            await WriteAsync(context,status,detail,errors);
        }
    }
    public static string Title(int status)=>status switch {400=>"Bad Request",401=>"Unauthorized",403=>"Forbidden",404=>"Not Found",409=>"Conflict",413=>"Payload Too Large",429=>"Too Many Requests",_=>"Request Failed"};
    public static async Task WriteAsync(HttpContext context,int status,string detail,IDictionary<string,string[]>? errors=null)
    {
        ProblemDetails problem=errors is null?new ProblemDetails():new ValidationProblemDetails(errors);
        problem.Status=status;problem.Title=status==500?"Internal Server Error":Title(status);problem.Detail=detail;
        problem.Extensions["traceId"]=Activity.Current?.Id??context.TraceIdentifier;
        context.Response.Headers.CacheControl="no-store";
        context.Response.Headers["X-Content-Type-Options"]="nosniff";
        context.Response.Headers["Referrer-Policy"]="no-referrer";
        context.Response.StatusCode=status;
        await context.Response.WriteAsJsonAsync(problem,options:(System.Text.Json.JsonSerializerOptions?)null,contentType:"application/problem+json",cancellationToken:context.RequestAborted);
    }
}
