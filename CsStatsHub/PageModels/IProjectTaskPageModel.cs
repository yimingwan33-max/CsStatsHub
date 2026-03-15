using CommunityToolkit.Mvvm.Input;
using CsStatsHub.Models;

namespace CsStatsHub.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}