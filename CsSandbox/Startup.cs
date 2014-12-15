using System;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(CsSandbox.Startup))]

namespace CsSandbox
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
	        AppDomain.MonitoringIsEnabled = true;
            ConfigureAuth(app);
        }
    }
}
