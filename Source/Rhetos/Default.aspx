<%@ Page Language="C#" %>

<html>
<head>
    <title>Rhetos</title>
</head>
<body>
    <h1>Rhetos</h1>
    <p>
        <%
            var snippets = Autofac.ResolutionExtensions.Resolve<IEnumerable<Rhetos.IHomePageSnippet>>(Autofac.Integration.Wcf.AutofacServiceHostFactory.Container);
            foreach (var snippet in snippets)
                Response.Write(snippet.Html);
        %>
    </p>
    <h2>Server status</h2>
    <p>
        Local server time: <%=DateTime.Now %><br />
        Process start time: <%=System.Diagnostics.Process.GetCurrentProcess().StartTime %><br />
        User identity: <%=Context.User.Identity.Name %><br />
        User authentication type: <%=Context.User.Identity.AuthenticationType %><br />
    </p>
</body>
</html>
