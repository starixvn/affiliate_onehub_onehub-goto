namespace ONEHub.Shared.Extensions;
using System;
using System.Text;
using Microsoft.AspNetCore.Http;

public static class WhoamiHelper
{
    public static string GetContent(HttpContext context)
    {
        var content = new StringBuilder();
        content.Append($"Web name: {context.Request.Host.Host}{Environment.NewLine}"
                       + $"Port: {context.Connection.LocalPort}{Environment.NewLine}"
                       + $"Server IP: {context.Connection.LocalIpAddress}{Environment.NewLine}"
                       + $"Server Time: {DateTimeOffset.Now}{Environment.NewLine}"
                       + $"Client IP: {context.Connection.RemoteIpAddress}{Environment.NewLine}"
                       + $"Request Scheme: {context.Request.Scheme}{Environment.NewLine}"
                       + $"Request Path: {context.Request.Path}{Environment.NewLine}{Environment.NewLine}"
                       + $"Request Headers:{Environment.NewLine}");

        foreach (var header in context.Request.Headers)
        {
            content.Append($"- {header.Key}: " +
                           $"{header.Value}{Environment.NewLine}");
        }

        return content.ToString();
    }
}