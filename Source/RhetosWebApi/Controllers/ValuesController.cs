using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace RhetosWebApi.Controllers
{
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {
        private readonly ILogger _logger;
        private readonly Lazy<ISqlExecuter> _sqlExecuter;
        private readonly ILocalizer _localizer;
        public ValuesController(
            ILogProvider logProvider,
            //Lazy<IAuthorizationManager> authorizationManager,
            //GenericRepositories repositories,
            Lazy<ISqlExecuter> sqlExecuter,
            //Lazy<IEnumerable<ISendPasswordResetToken>> sendPasswordResetTokenPlugins,
            ILocalizer localizer
            )
        {
            _logger = logProvider.GetLogger("AspNetFormsAuthWebApi.AuthenticationController");
            //_authorizationManager = authorizationManager;
            _sqlExecuter = sqlExecuter;
            //_sendPasswordResetTokenPlugin = new Lazy<ISendPasswordResetToken>(() => SinglePlugin(sendPasswordResetTokenPlugins));

            //_passwordStrengthRules = new Lazy<IEnumerable<IPasswordStrength>>(() => repositories.Load<IPasswordStrength>());
            //_passwordAttemptsLimits = new Lazy<IEnumerable<IPasswordAttemptsLimit>>(() =>
            //{
            //    var limits = repositories.Load<IPasswordAttemptsLimit>();
            //    foreach (var limit in limits)
            //        if (limit.TimeoutInSeconds == null || limit.TimeoutInSeconds <= 0)
            //            limit.TimeoutInSeconds = int.MaxValue;
            //    return limits;
            //});
            _localizer = localizer;
        }
        // GET api/values
        [Route("a")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}