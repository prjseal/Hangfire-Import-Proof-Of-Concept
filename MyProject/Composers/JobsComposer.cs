using Hangfire;
using Hangfire.Common;
using Hangfire.Console;
using Hangfire.Server;
using MyProject.Services;
using Umbraco.Cms.Core.Composing;

namespace MyProject.Composers
{
    public class JobsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            if(AllowRunningHangfireJobs(builder))
            {
                RecurringJob.AddOrUpdate<IImportService>("Import", x => x.Import(), "*/1 * * * *");
            }
        }

        private bool AllowRunningHangfireJobs(IUmbracoBuilder builder)
        {
            var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            return config.GetValue<bool>("AllowRunningHangfireJobs");
        }
    }
}
