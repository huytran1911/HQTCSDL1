using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

public class UserAuthorizeAttribute : AuthorizeAttribute
{
    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        // Nếu chưa login
        if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
        {
            // xây route về /Account/Login (area root)
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary {
                  { "controller", "Account" },
                  { "action",     "Login" },
                  { "area",       "" },
                  { "returnUrl",  filterContext.HttpContext.Request.RawUrl }
                }
            );
        }
        else
        {
            // đã login nhưng không đủ quyền, hiển thị 403
            base.HandleUnauthorizedRequest(filterContext);
        }
    }
}
