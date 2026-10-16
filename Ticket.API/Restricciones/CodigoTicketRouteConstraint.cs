using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ticket.API.Restricciones;

public class CodigoTicketRouteConstraint : IRouteConstraint
{
    private static readonly Regex RegexCodigo = new(@"^HD-\d{4}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (values.TryGetValue(routeKey, out var routeValue) && routeValue != null)
        {
            var valueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(valueString))
            {
                return RegexCodigo.IsMatch(valueString);
            }
        }

        return false;
    }
}
