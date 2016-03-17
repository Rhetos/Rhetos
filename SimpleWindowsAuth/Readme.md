# SimpleWindowsAuth

SimpleWindowsAuth is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides a simple authorization system (Principals & Permissions) and **Windows authentication** to Rhetos server applications.

## Warning: SimpleWindowsAuth package is obsolete

SimpleWindowsAuth package is obsolete since Rhetos v0.9.29, and should be used only for backward compatibility in legacy applications.
The Windows Authentication is now embedded in Rhetos core.

When removing the package from an existing application, the following changes will occur:
* Rhetos admin GUI for setting user's permissions is no longer available (/Resources/Permissions.html).
* User names in Common.Principal must start with domain name prefix (domainname\username).
* Please move domain user groups from Common.Principal to Common.Role.
* Deploy ActiveDirectorySync package to automatically update principal-role membership (Common.PrincipalHasRole) for domain users and groups.
* Each user must be entered in Common.Principal, SimpleWindowsAuth allowed entering only user groups. For backward compatibility enable AuthorizationAddUnregisteredPrincipals option in web.config on Rhetos v0.9.31 or later.
