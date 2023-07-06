using MyProject.Services;
using Umbraco.Cms.Core.Composing;

namespace MyProject.Composers;

public class RegisterServicesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IImportService, ImportService>();
    }
}
