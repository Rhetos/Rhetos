<Query Kind="Program">
  <Namespace>System</Namespace>
  <Namespace>System.Collections</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Text</Namespace>
</Query>

// const string testUserName = "u1";
// const string testUserPassword = "u1p";

void Main()
{
    using (var web = new CookieAwareWebClient() { BaseAddress = "http://localhost/Rhetos/" })
    {
        // Reading without logging in:
        
        bool successful = web.DownloadString("").Contains("<title>Rhetos</title>").Dump("Read Rhetos home page");
        if (successful) throw new Exception("Reading without logging in should fail.");
        
        // Log in the test user:
        
        string response = web.Post("Resources/AspNetFormsAuth/Authentication/Login",
            @"{""UserName"":""u1"",""Password"":""notMyPassword"",""PersistCookie"":false}").Dump("Login (invalid password) response");
        if (response != "false") throw new Exception("Login should fail when given invalid password.");
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/Login",
            @"{""UserName"":""u1"",""Password"":""u1p"",""PersistCookie"":false}").Dump("Login (test user) response");
        if (response != "true") throw new Exception(@"Login failed. Check if Rhetos server contains test user ""u1"" with password ""u1p"".");
        Print(web.Cookies);
        if (!web.Cookies.Any(c => c.Name == ".ASPXAUTH")) throw new Exception("Did not get authorization cookie from login service.");
        
        successful = web.DownloadString("").Contains("<title>Rhetos</title>").Dump("Read Rhetos home page");
        if (!successful) throw new Exception("Reading after login should succeed.");
        
        // Change my password:
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/ChangeMyPassword",
            @"{""OldPassword"":""notMyOldPassword"",""NewPassword"":""u1p""}").Dump("ChangeMyPassword (invalid old) response");
        Print(web.Cookies);
        if (response != "false") throw new Exception("ChangeMyPassword should fail when given invalid old password.");
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/ChangeMyPassword",
            @"{""OldPassword"":""u1p"",""NewPassword"":""u1pp""}").Dump("ChangeMyPassword response");
        Print(web.Cookies);
        if (response != "true") throw new Exception("ChangeMyPassword failed");
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/ChangeMyPassword",
            @"{""OldPassword"":""u1pp"",""NewPassword"":""u1p""}").Dump("ChangeMyPassword (back) response");
        Print(web.Cookies);
        if (response != "true") throw new Exception("ChangeMyPassword (back) failed");
        
        // SetPassword (by testuser):
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/SetPassword",
            @"{""UserName"":""u1"",""Password"":""u1p""}").Dump("SetPassword (by testuser) response");
        if (!response.Contains("(400) Bad Request") || !response.Contains("AspNetFormsAuth.AuthenticationService") || !response.Contains("SetPassword"))
            throw new Exception("Test user should not have rights to call SetPassword.");
        
        // Log out the test user:
        
        web.Post("Resources/AspNetFormsAuth/Authentication/Logout", "").Dump("Logout response");
        Print(web.Cookies);
        
        successful = web.DownloadString("").Contains("<title>Rhetos</title>").Dump("Read Rhetos home page");
        if (successful) throw new Exception("Reading after logout should fail.");
        
        // ChangeMyPassword without logging in:
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/ChangeMyPassword",
            @"{""OldPassword"":""u1p"",""OldPassword"":u1pp}").Dump("ChangeMyPassword (without logging in) response");
        Print(web.Cookies);
        if (!response.Contains("(401) Unauthorized")) throw new Exception("ChangeMyPassword without logging in should fail.");
        
        // SetPassword:
        
        response = web.Post("Resources/AspNetFormsAuth/Authentication/SetPassword",
            @"{""UserName"":""u1"",""Password"":""u1p""}").Dump("SetPassword (without logging in) response");
        if (!response.Contains("(401) Unauthorized")) throw new Exception("SetPassword without logging in should fail.");
        
        
        "All tests passed".Dump("Done");
    }
}

public class CookieAwareWebClient : WebClient
{
    private CookieContainer _cookieContainer = new CookieContainer();
    
    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest request = base.GetWebRequest(address);
        HttpWebRequest webRequest = request as HttpWebRequest;
        if (webRequest != null && _cookieContainer != null)
        {
            webRequest.CookieContainer = _cookieContainer;
        }
        return request;
    }
    
    public string Post(string uri, string data)
    {
        try
        {
            Headers.Add("Content-Type","application/json");
            Encoding = System.Text.Encoding.UTF8;
            return UploadString(uri, "POST", data);
        }
        catch (Exception ex)
        {
            string msg = "ERROR: " + ex.ToString();
            if (ex is WebException)
            {
                var response = (HttpWebResponse)((WebException)ex).Response;
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = new StreamReader(receiveStream, new UTF8Encoding());
                string responseText = readStream.ReadToEnd();
                readStream.Close();
                receiveStream.Close();
                
                const int maxLen = 220;
                if (responseText.Length > maxLen) responseText = responseText.Substring(0, maxLen) + "...";
                msg = "RESPONSE: " + responseText + "\r\n" + msg;
            }
            return msg;
        }
    }
    
    public IList<Cookie> Cookies
    {
        get
        {
            var hashtable = (Hashtable) _cookieContainer.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, _cookieContainer, new object[] { });
            return hashtable.Keys.OfType<object>().SelectMany(key => _cookieContainer.GetCookies(new Uri(string.Format("http://{0}/", key))).OfType<Cookie>()).ToList();
        }
    }
}

void Print(IEnumerable<Cookie> cookies)
{
    ("COOKIES (" + cookies.Count() + "): " + string.Join(", ", cookies.Select(c => c.Domain + ":" + c.Name))).Dump();
}