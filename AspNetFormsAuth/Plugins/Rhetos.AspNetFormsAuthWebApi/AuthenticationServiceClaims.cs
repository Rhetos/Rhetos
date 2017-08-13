/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Processing;
using Rhetos.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.AspNetFormsAuthWebApi
{
    /// <summary>
    /// List of admin claims is provided by a IClaimProvider plugin, in order to automatically create the claims on Rhetos deployment.
    /// </summary>
    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DummyCommandInfo))]
    public class AuthenticationServiceClaims : IClaimProvider
    {
        #region IClaimProvider implementation.

        public IList<Claim> GetRequiredClaims(ICommandInfo info)
        {
            return null;
        }

        public IList<Claim> GetAllClaims(IDslModel dslModel)
        {
            return GetDefaultAdminClaims();
        }

        #endregion

        public static IList<Claim> GetDefaultAdminClaims()
        {
            return new[] {
                SetPasswordClaim, UnlockUserClaim, IgnorePasswordStrengthPolicyClaim, GeneratePasswordResetTokenClaim,
                ReadCommonPrincipal, NewCommonPrincipal };
        }

        public static readonly Claim SetPasswordClaim = new Claim("AspNetFormsAuth.AuthenticationService", "SetPassword");
        public static readonly Claim UnlockUserClaim = new Claim("AspNetFormsAuth.AuthenticationService", "UnlockUser");
        public static readonly Claim IgnorePasswordStrengthPolicyClaim = new Claim("AspNetFormsAuth.AuthenticationService", "IgnorePasswordStrengthPolicy");
        public static readonly Claim GeneratePasswordResetTokenClaim = new Claim("AspNetFormsAuth.AuthenticationService", "GeneratePasswordResetToken");

        public static readonly Claim ReadCommonPrincipal = new Claim("Common.Principal", "Read");
        public static readonly Claim NewCommonPrincipal = new Claim("Common.Principal", "New");
    }

    public class DummyCommandInfo : ICommandInfo
    {
    }
}
