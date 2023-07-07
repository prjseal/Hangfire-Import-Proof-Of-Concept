using MyProject.Models;
using Newtonsoft.Json;
using System.Net;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

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
                var needToPublish = false;

                foreach (var centre in data)
                {
                    if(!existingPages.ContainsKey(centre.systemid.ToString()))
                    {
                        //create an umbraco node
                        Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.ImportSingleCentre(centre, rootContent.Id));
                        needToPublish = true;
                    }
                    else
                    {
                        //update the umbraco node
                        if (!existingPages.TryGetValue(centre.systemid.ToString(), out var contentItem)) continue;

                        var lastUpdatedDate = contentItem.GetValue<DateTime>("lastModifiedDate");

                        if (lastUpdatedDate >= centre.lastModifiedDate) continue;

                        Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.UpdateSingleCentre(centre, contentItem.Id));
                        needToPublish = true;
                    }
                }

                if(needToPublish)
                {
                    Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.PublishImportFolderAndChildren());
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

    public void PublishImportFolderAndChildren()
    {
        var rootContent = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "contentFolder");

        if (rootContent == null) throw new Exception("Unable to get import folder to publish");

        _contentService.SaveAndPublishBranch(rootContent, force: true);
    }
}
