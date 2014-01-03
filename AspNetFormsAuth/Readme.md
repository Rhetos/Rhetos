AspNetFormsAuth
===============

AspNetFormsAuth is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).

AspNetFormsAuth provides an implementation of **ASP.NET forms authentication** to Rhetos server applications. It can be useful to have same (compatible) authentication systems on application server and separate GUI application in the same domain, allowing the client to use shared authentication cookies between multiple server applications (see [MSDN: Forms Authentication Across Applications](http://msdn.microsoft.com/en-us/library/eb0zx8fc.aspx)).

Features
--------

* Authentication is implemented using SimpleMembershipProvider (WebMatrix).
* Authorization is implemented using Principal-Role-Permission-Claim Rhetos entities.
