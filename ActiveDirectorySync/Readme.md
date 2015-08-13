# ActiveDirectorySync

ActiveDirectorySync is a package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It synchronizes the Rhetos principals and roles with Active Directory by automatically adding or removing principal-role and role-role membership relations.

## Installation

* *CommonConcepts* packages must be deployed along with this package.

## Configuring Rhetos users and user groups

1. To allow a **domain user** to use Rhetos application, insert the record in the `Common.Principal` entity.
   The principal's name must have domain name prefix.
2. To allow a **domain user group** to be used for assigning permissions to users, insert the record in the `Common.Role` entity.
   The role's name must have domain name prefix.
3. ActiveDirectorySync will automatically handle relation between the inserted principals and role, based on information from Active Directory.

To set the users permissions, the following methods are available:

1. Set the users permissions directly, inserting the record in `Common.PrincipalPermission`.
2. Set the user's group permissions, inserting the record in `Common.RolePermission`.
3. Create a group of permissions and assign it to the user or user group:
    * Add a new `Common.Role` without domain name prefix (it will not be bound to the domain user group) that will serve as a permission group.
    * Set the role's permissions in `Common.RolePermission`.
    * Assign the role to the user's group (insert in `Common.RoleInheritsRole`) or directly to the userm (insert in `Common.PrincipalHasRole`).
