using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace CODE.Framework.Services.Contracts
{
    public static class UserPrincipalHelper
    {
        private static Dictionary<object, IPrincipal> Principals { get; } = new Dictionary<object, IPrincipal>();

        public static IPrincipal GetCurrentPrincipal(this object instance)
        {
#if NETSTANDARD
            lock (Principals)
                if (Principals.ContainsKey(instance))
                    return Principals[instance];

            return new ClaimsPrincipal(new ClaimsIdentity());
#else
            // TODO: return Thread.CurrentPrincipal;
            return null;
#endif
        }

        public static void AddPrincipal(object instance, IPrincipal principal)
        {
#if NETSTANDARD
            lock (Principals) Principals[instance] = principal;
#endif
        }

        public static void RemovePrincipal(object instance)
        {
#if NETSTANDARD
            lock (Principals)
                if (Principals.ContainsKey(instance))
                    Principals.Remove(instance);
#endif
        }
    }
}