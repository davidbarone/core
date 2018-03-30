using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Dbarone.Lake.Web
{
    //http://stackoverflow.com/questions/9298466/can-i-return-custom-error-from-jsonresult-to-jquery-ajax-error-method
    /// <summary>
    /// When places over an Xhr ActionResult method, any exception is caught and 
    /// gets returned as the status text. A server status code of 500 is returned also.
    /// This is used for Xhr requests.
    /// </summary>
    public class XhrExceptionFilterAttribute : System.Web.Mvc.FilterAttribute, System.Web.Mvc.IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            // checks if 'X-Requested-With' header set to 'XmlHttpRequest'
            if (filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.StatusCode = 500;
                filterContext.Result = new ContentResult()
                {
                    Content = filterContext.Exception.Message
                };
                filterContext.ExceptionHandled = true;
            }
        }
    }

}