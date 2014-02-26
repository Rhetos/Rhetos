AspNetFormsAuth
===============

AspNetFormsAuth is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of **ASP.NET forms authentication** to Rhetos server applications.

Table of contents
-----------------

1. [Features](#Features)
2. [Authentication service API](#AuthenticationServiceApi)
3. [Installation](#Installation)
4. [Configuration](#Configuration)
5. [Uninstallation](#Uninstallation)
6. [Sharing the authentication across web applications](#SharingAuthentication)
7. [Troubleshooting](#Troubleshooting)


<a id="Features"></a>
Features
--------

#### Authentication

* For developers and administrators, a simple login and logout web forms are provided.
  Links are available on the Rhetos server home page.
* [Authentication service](#AuthenticationServiceApi) may be used in web applications
  and other services to log in and log out users, and for other related actions. 
* Forms authentication may be utilized for [sharing the authentication](#SharingAuthentication)
  across multiple web applications.

#### Authorization

* Authorization is implemented internally using claim-based permissions system. 
  The users' permissions may be configured for each action or data query, using Rhetos entities: `Principal`, `Role`, `Permission` and `Claim`.

#### Technical notes

* AspNetFormsAuth packages will automatically import all principals and permissions
  form SimpleWindowsAuth package, if used before.
  Note that roles cannot be automatically imported because SimpleWindowsAuth depends on Active Directory user groups.
* Authentication is implemented using Microsoft's `SimpleMembershipProvider` (WebMatrix).
* The log in form and service allow anonymous access (it is a standard forms authentication feature).


<a id="AuthenticationServiceApi"></a>
Authentication service API
--------------------------

The JSON service is available at URI `<rhetos server>/Resources/AspNetFormsAuth/Authentication`, with the following methods.

#### Methods 

**`/Login`** (string UserName, string Password, bool PersistCookie) -> bool

* Example of the request data: `{"UserName":"myusername","Password":"mypassword","PersistCookie":false}`.
* On successful log in, the server response will contain the standard authentication cookie.
  The client browser will automatically use the cookie for following requests.
* Response data is boolean *true* if the login is successful,
  *false* if the login and password does not match,
  or an error message (string) with HTTP error code 4* or 5* in case of any other error.

**`/Logout`**

* No request data is needed, assuming standard authentication cookie is automatically provided. Response is empty.

<a id="SetPassword"></a>
**`/SetPassword`** (string UserName, string Password, bool IgnorePasswordStrengthPolicy)

* Sets or resets the given user's password.
* Requires *SetPassword* [claim](#Permissions).
  If IgnorePasswordStrengthPolicy property is set, *IgnorePasswordStrengthPolicy* [claim](#Permissions) is required.
* Response data is empty if the command is successful,
  an error message (string) with HTTP error code 400 if the password does not match the password strength policy,
  or an error message with HTTP error code 4* or 5* in case of any other error.

**`/ChangeMyPassword`** (string OldPassword, string NewPassword) -> bool

* Changes the current user's password.
* Response data is boolean *true* if the login is successful,
  *false* if the login and password does not match,
  an error message (string) with HTTP error code 400 if the password does not match the password strength policy,
  or an error message with HTTP error code 4* or 5* in case of any other error.

<a id="UnlockUser"></a>
**`/UnlockUser`** (string UserName)

* Reset the number of [failed login attempts](#FailedPasswordAttempts). Response is empty.
* Requires *UnlockUser* [claim](#Permissions).

<a id="Permissions"></a>
#### Permissions

All claims related to the authentication service have resource="*AspNetFormsAuth.AuthenticationService*".
[Admin user](#AdminSetup) has all the necessary permissions (claims) for all authentication service methods.


<a id="Installation"></a>
Installation
------------

Prerequisites:

* AspNetFormsAuth cannot be deployed together with **SimpleWindowsAuth**.

Before or after deploying the AspNetFormsAuth packages, please make the following changes to the web site configuration, in order for forms authentication to work.  

#### 1. Modify Web.config

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

#### 2. Configure IIS

1. Start IIS Manager -> Select the web site -> Open "Authentication" feature.
2. On the Authentication page **enable** *Anonymous Authentication* and *Forms Authentication*, **disable** *Windows Authentication* and every other.

#### 3. Configure IIS Express

*(Only if using IIS Express instead of IIS server)*

If using IIS Express, after adding AspNetFormsAuth package to `ApplyPackages.txt` execute `SetupRhetosServer.bat` utility in Rhetos server's folder to automatically configure `IISExpress.config`, or manually apply the following lines in IISExpress configuration file inside `system.webServer` element or inside `location / system.webServer` (usually at the end of the file):

	<security>
	    <authentication>
	        <anonymousAuthentication enabled="false" />
	        <windowsAuthentication enabled="true" />
	    </authentication>
	</security>

#### 4. Set up HTTPS

HTTPS (or any other) secure transport protocol **should always be enforced** when using forms authentication.
This is necessary because in forms authentication the password is submitted as a plain text.

At least the services inside `/Resources/AspNetFormsAuth` path must use HTTPS to protect user's password.

Consider using a [free SSL certificate](https://www.google.hr/search?q=free+SSL+certificate) (search the web for the providers) in development or QA environment.


<a id="Configuration"></a>
Configuration
-------------

<a id="AdminSetup"></a>
#### "admin" user

`DeployPackages.exe`, when deploying the AspNetFormsAuth packages, automatically creates the *admin* user account and *SecurityAdministrator* role, adds the account to the role and gives it necessary permissions (claims) for all authentication service methods.

1. After deployment, **run the utility** `\bin\Plugins\AdminSetup.exe` to initialize the *admin* user's password.

<a id="FailedPasswordAttempts"></a>
#### Maximum failed password attempts

Use entity *Commmon.AspNetFormsAuthPasswordAttemptsLimit* (*MaxInvalidPasswordAttempts*, *TimeoutInSeconds*) to configure automatic account locking when a number of failed password attempts is reached.

* When *MaxInvalidPasswordAttempts* limit is passed, the user's account is temporarily locked.
* If *TimeoutInSeconds* is set, user's account will be temporarily locked until the specified time period has passed. If the value is not set or 0, the account will be locked permanently.
* Administrator may use [UnlockUser](#UnlockUser) authentication service method to unlock the account, or wait for *TimeoutInSeconds*.
* Multiple limits may be entered. An example with two entries:

> After 3 failed attempts, the account is temporarily locked for 120 seconds;
> after 10 failed attempts, the account is locked until *admin* unlocks it manually (timeout=0).

#### Password strength policy

Use entity *Common.AspNetFormsAuthPasswordStrength* (*RegularExpression*, *RuleDescription*) to configure the policy.

* A new password must pass all the rules in *Common.AspNetFormsAuthPasswordStrength*.
* *RuleDescription* is uses as an error message to the user if the new password breaks the policy.
* When administrator executes [SetPassword](#SetPassword) authorization service method, the property *IgnorePasswordStrengthPolicy* may be used to avoid the policy.

Examples:

RegularExpression|RuleDescription
-----------------|---------------
`.{6,}`          | The password length must be at least six characters.
`\d`             | The password must contain at least one digit.
`(\d.*){3,}`     | The password must contain at least three digits.
`[A-Z]`          | The password must contain at least one uppercase letters.
`\W`             | The password must contain at least one special character (not a letter or a digit).


<a id="Uninstallation"></a>
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


<a id="SharingAuthentication"></a>
Sharing the authentication across web applications
--------------------------------------------------

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
You may use the following C# code to generate the keys:
 
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

    
<a id="Troubleshooting"></a>
Troubleshooting
---------------

**Issue**: Deployment results with error message "DslSyntaxException: Concept with same key is described twice with different values."<br>
**Solution**: Please check if you have deployed both *SimpleWindowsAuth* package and *AspNetFormsAuth* package at the same time. Only one of the packages can be deployed on Rhetos server. Read the [installation](#Installation) instructions above for more information on the issue.

**Issue**: Web service responds with error message "The Role Manager feature has not been enabled."<br>
**Solution**: The error occurs when the necessary modifications of Web.config file are not done. Please check that you have followed the [installation](#Installation) instructions above.

**Issue**: I have accidentally deleted the *admin* user, *SecurityAdministrator* role, or some of its claims. How can I get it back?<br> 
**Solution**: Execute `DeployPackages.exe` again. It will regenerate the default administration settings. See [admin user](#AdminSetup). 

**Other:** In case of a server error, additional information on the error may be found in the Rhetos server log (`RhetosServer.log` file, by default).
If needed, more verbose logging may be switched on by uncommenting the `<logger name="*" minLevel="Trace" writeTo="TraceLog" />` element in Rhetos server's `web.config`. 
