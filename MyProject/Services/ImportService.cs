using Microsoft.AspNetCore.Hosting;
using MyProject.Models;
using Newtonsoft.Json;
using Polly;
using System.Net;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace MyProject.Services;

public class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IContentService _contentService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ImportService(ILogger<ImportService> logger, IUmbracoContextFactory umbracoContextFactory,
        IContentService contentService, IWebHostEnvironment webHostEnvironment)
    {
        _logger = logger;
        _umbracoContextFactory = umbracoContextFactory;
        _contentService = contentService;
        _webHostEnvironment = webHostEnvironment;
    }

    public void Import()
    {
        var rootContent = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "contentFolder");
        var contentPages = _contentService.GetPagedChildren(rootContent.Id, 0, 10000, out long totalRecords);

        var existingPages = contentPages.ToDictionary(x => x.GetValue<string>("systemid"));

        if (rootContent == null) throw new Exception("Unable to get parent folder");

        //call a third party api
        using (WebClient httpClient = new WebClient())
        {
            var domain = _webHostEnvironment.IsDevelopment()
                ? "https://localhost:44363"
                : "https://localhost:44363";

            var jsonData = httpClient.DownloadString(domain + "/centres.json");

            //get a set of data to import
            var data = JsonConvert.DeserializeObject<IEnumerable<CentreModel>>(jsonData);

            if (data != null)
            {
                var i = 1;
                foreach (var centre in data)
                {
                    if(!existingPages.ContainsKey(centre.systemid.ToString()))
                    {
                        //create an umbraco node
                        Hangfire.BackgroundJob.Schedule<IImportService>(x => 
                            x.ImportSingleCentre(centre, rootContent.Id), DateTimeOffset.UtcNow.AddSeconds(i * 10));
                    }
                    else
                    {
                        //update the umbraco node
                        if (!existingPages.TryGetValue(centre.systemid.ToString(), out var contentItem)) continue;

                        var lastUpdatedDate = contentItem.GetValue<DateTime>("lastModifiedDate");

                        if (lastUpdatedDate >= centre.lastModifiedDate) continue;
                        
                        Hangfire.BackgroundJob.Schedule<IImportService>(x => 
                            x.UpdateSingleCentre(centre, contentItem.Id), DateTimeOffset.UtcNow.AddSeconds(i * 10));
                    }
                    i++;
                }
            }
        }
    }

    public void ImportSingleCentre(CentreModel centre, int parentId)
    {
        var rootContent = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "contentFolder");

        if (rootContent == null) throw new Exception("Unable to get parent folder");

        var newItem = _contentService.Create(centre.name, parentId, "contentPage");

        newItem.SetValue("latitude", centre.latitude);
        newItem.SetValue("longitude", centre.longitude);
        newItem.SetValue("systemid", centre.systemid.ToString());
        newItem.SetValue("lastModifiedDate", centre.lastModifiedDate);

        _contentService.Save(newItem);
    }

    public void UpdateSingleCentre(CentreModel centre, int contentId)
    {
        var contentItem = _contentService.GetById(contentId);

        if (contentItem == null) throw new Exception("Unable to get content item to update");

        contentItem.SetValue("latitude", centre.latitude);
        contentItem.SetValue("longitude", centre.longitude);
        contentItem.SetValue("systemid", centre.systemid.ToString());
        contentItem.SetValue("lastModifiedDate", centre.lastModifiedDate);

        _contentService.Save(contentItem);
    }
}
