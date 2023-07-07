using MyProject.Models;

namespace MyProject.Services;

public interface IImportService
{
    public void Import();

    public void ImportSingleCentre(CentreModel centre, int parentId);
    public void UpdateSingleCentre(CentreModel centre, int contentId);
    public void PublishImportFolderAndChildren();
}
