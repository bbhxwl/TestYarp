using System.Net;
using System.Text;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

builder.Services.AddHttpForwarder();
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false
});

CustomTransformer c = new CustomTransformer();

var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

app.Map("/vue/{**catch-all}", async (HttpContext httpContext, IHttpForwarder forwarder) =>
{

    var error = await forwarder.SendAsync(httpContext,"https://cn.vuejs.org/", httpClient, requestOptions,
     c);

    if (error != ForwarderError.None)
    {
        var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
        var exception = errorFeature.Exception;
    }
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


public class CustomTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
        var queryContext = new QueryTransformContext(httpContext.Request);
        proxyRequest.RequestUri = new Uri(destinationPrefix + httpContext.Request.Path + queryContext.QueryString);
        proxyRequest.Headers.Host = null;
    }

    public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse)
    {

        if (proxyResponse==null)
        {
            return false;
        }
        string body=await proxyResponse.Content.ReadAsStringAsync();
        body= body.Replace("<title>Vue.js</title>", "<title>bbhxwl</title>");
        var content = new StringContent(body, Encoding.UTF8, "text/html");
        httpContext.Response.ContentLength = body.Length;
        proxyResponse.Content?.Dispose();
        proxyResponse.Content = content;
        return await base.TransformResponseAsync(httpContext, proxyResponse);
    }

}