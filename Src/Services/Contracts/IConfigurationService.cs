using ImageViewer.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ImageViewer.Services.Contracts;

internal interface IConfigurationService
{
    void Initialize(string configFilePath);
    Task<Models.Configuration> LoadConfigurationAsync();
    Task SaveConfigurationAsync();
    Models.Configuration CurrentConfiguration { get; }
}