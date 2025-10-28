using System;
using Prism.Dialogs;

namespace TrackerOfflineSearch.ForumSelector;

public static class DialogServiceForumSelectorExtensions
{
    public static void ShowSelectForumDialog(this IDialogService dialogService, string selectedForum, Action<string> callback)
    {
        var parameters = new DialogParameters { { nameof(ForumSelectorViewModel.SelectedPath), selectedForum } };

        dialogService.ShowDialog(
            nameof(ForumSelectorView),
            parameters,
            r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    callback(r.Parameters.GetValue<string>(nameof(ForumSelectorViewModel.SelectedPath)));
                }
            },
            nameof(ForumSelectorWindow)
        );
    }
}
