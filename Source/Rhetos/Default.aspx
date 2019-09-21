<%@ Page Language="C#" %>

<html>
<head>
    <title>Rhetos</title>
</head>
<body>
    <h1>Rhetos</h1>
    <div>
<%
    var snippets = Autofac.ResolutionExtensions.Resolve<Rhetos.Extensibility.IPluginsContainer<Rhetos.IHomePageSnippet>>(Autofac.Integration.Wcf.AutofacServiceHostFactory.Container);
    foreach (var snippet in snippets.GetPlugins())
        Response.Write(snippet.Html);
%>    </div>
    <h2>Installed packages</h2>
    <table><tbody>
<%
    var installedPackages = Autofac.ResolutionExtensions.Resolve<Rhetos.Deployment.IInstalledPackages>(Autofac.Integration.Wcf.AutofacServiceHostFactory.Container);
    foreach (var package in installedPackages.Packages)
        Response.Write("        <tr><td>" + Server.HtmlEncode(package.Id) + "</td><td>" + Server.HtmlEncode(package.Version) + "</td></tr>\r\n");
%>    </tbody></table>
    <h2>Server status</h2>
    <p>
        Local server time: <%=System.DateTime.Now %><br />
        Process start time: <%=System.Diagnostics.Process.GetCurrentProcess().StartTime %><br />
        User identity: <%=Context.User.Identity.Name %><br />
        User authentication type: <%=Context.User.Identity.AuthenticationType %><br />
        Is 64-bit process: <%=Environment.Is64BitProcess %><br />
    </p>
</body>
</html>
