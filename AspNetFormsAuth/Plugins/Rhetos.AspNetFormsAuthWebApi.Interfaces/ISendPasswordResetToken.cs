using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.AspNetFormsAuthWebApi
{
    public interface ISendPasswordResetToken
    {
        void SendPasswordResetToken(string userName, Dictionary<string, string> additionalClientInfo, string passwordResetToken);
    }
}
