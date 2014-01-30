AspNetFormsAuth
===============

AspNetFormsAuth is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).

AspNetFormsAuth provides an implementation of **ASP.NET forms authentication** to Rhetos server applications.
It can be useful to have same (compatible) authentication systems on application server and separate GUI application in the same domain, allowing the client to use shared authentication cookies between multiple server applications (see [MSDN: Forms Authentication Across Applications](http://msdn.microsoft.com/en-us/library/eb0zx8fc.aspx)).

Features
--------

#### Authentication

* For developers and administrators, a simple web form is provided in order to login to the site: `/Resources/AspNetFormsAuth/Login.html`.
* Other web applications and services may log in by sending a POST request to URI `/Resources/AspNetFormsAuth/Authentication/Login` with JSON serialized login information (UserName, Password, PersistCookie).
  * Example of the request data: `{"UserName":"myusername","Password":"mypassword","PersistCookie":false}`.
  * The server response will contain the standard authentication cookie, and the client browser will automatically use the cookie for following requests.
* See the installation notes below for securing the site and sharing the authentication across multiple web applications.

#### Authorization

Authorization is implemented internally using claim-based permissions system.
The users' permissions may be configured for each action or data query, using Rhetos entities: `Principal`, `Role`, `Permission` and `Claim`.

#### Technical notes

* AspNetFormsAuth packages will automatically import all principals and permissions form SimpleWindowsAuth package, if used before. Note that roles cannot be automatically imported because SimpleWindowsAuth depends on Active Directory user groups.
* Authentication is implemented using Microsoft's `SimpleMembershipProvider` (WebMatrix).
* The log in form and service allow anonymous access (it is a standard forms authentication feature).

Installation
------------

Before or after deploying the AspNetFormsAuth packages, please make the following changes to the web site configuration, in order for forms authentication to work.  

#### Modify Web.config

1. Comment out (or delete) the `security mode="TransportCredentialOnly` elements in all bindings.
2. Remove the `<authentication mode="Windows" />` element.
3. Inside the `<system.web>` element add the following:

	    <authentication mode="Forms" />
	    <roleManager enabled="true" />
	    <membership defaultProvider="SimpleMembershipProvider">
	      <providers>
	        <clear />
	        <add name="SimpleMembershipProvider" type="WebMatrix.WebData.SimpleMembershipProvider, WebMatrix.WebData" />
	      </providers>
	    </membership>
	    <authorization>
	      <deny users="?" />
	    </authorization>

#### Configure IIS

1. Start IIS Manager -> Select the web site -> Open "Authentication" feature.
2. On the Authentication page **enable** *Anonymous Authentication* and *Forms Authentication*, **disable** *Windows Authentication* and every other.

#### AdminSetup

`DeployPackages.exe`, when deploying the AspNetFormsAuth packages, creates the *admin* user account and *SecurityAdministrator* role, adds the account to the role and gives it permission for  *AspNetFormsAuth.AuthenticationService.SetPassword*.

1. After deployment, **run the utility** `\bin\Plugins\AdminSetup.exe` to initialize the *admin* user account.

#### Set up HTTPS

HTTPS (or any other) secure transport protocol **should always be enforced** when using forms authentication.
This is necessary because in forms authentication the password is submitted as a plain text.

At least the services inside `/Resources/AspNetFormsAuth` path must use HTTPS to protect user's password.

Uninstallation
--------------

When returning Rhetos server from Forms Authentication back to **Windows Authentication**, the following configuration changes should be done:

#### Modify Web.config

1. Add (or uncomment) the following element inside all `<binding ...>` elements:

		<security mode="TransportCredentialOnly">
			<transport clientCredentialType="Windows" />
		</security>

2. Inside `<system.web>` remove following elements:

	    <authentication mode="Forms" />
	    <roleManager enabled="true" />
	    <membership defaultProvider="SimpleMembershipProvider">
	      <providers>
	        <clear />
	        <add name="SimpleMembershipProvider" type="WebMatrix.WebData.SimpleMembershipProvider, WebMatrix.WebData" />
	      </providers>
	    </membership>
	    <authorization>
	      <deny users="?" />
	    </authorization>

3. Inside `<system.web>` add the `<authentication mode="Windows" />` element.

#### Configure IIS

1. Start IIS Manager -> Select the web site -> Open "Authentication" feature.
2. On the Authentication page **disable** *Anonymous Authentication* and *Forms Authentication*, **enable** *Windows Authentication*.

Advanced topics
---------------

#### Sharing the authentication across web applications

Sharing the authentication cookie is useful when using separate web sites for web pages and application services, or when using multiple sites for load balancing.
In these scenarios, sharing the forms authentication cookie between the sites will allow a single-point login for the user on any of the sites and seamless use of the cookie on any of the other sites.

* In most cases, for the sites to share the authentication cookie, it is enough to have **same** `machineKey` element configuration in the `web.config`.
For more info, see [MSDN article: Forms Authentication Across Applications](http://msdn.microsoft.com/en-us/library/eb0zx8fc.aspx).

* If you have multiple Rhetos applications on a server and do not want to share the authentication between them, make sure to set **different** `machineKey` configuration for each site.

The machine key in `web.config` may have the following format:

	<machineKey
		validationKey="4F579A4589E986E7AF4D11767160DFBCF15A733F285EEF31B6DD26C7D7E9A8D5"
		decryptionKey="73080E3328B61DC59DE2E3F7FFCA11E2706D62F7BF162E5529728F2C448D8269"
		validation="HMACSHA256" />

It is important to generate new validationKey and decryptionKey for every deployment.
You may use the following C# code (hint: use LinqPad) to generate the keys:
 
	void Main()
	{
	  int len = 64;
	  byte[] buff = new byte[len/2];
	  RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
	  rng.GetBytes(buff);
	  StringBuilder sb = new StringBuilder(len);
	  for (int i=0; i<buff.Length; i++)
	    sb.Append(string.Format("{0:X2}", buff[i]));
	  sb.ToString().Dump();
	}
