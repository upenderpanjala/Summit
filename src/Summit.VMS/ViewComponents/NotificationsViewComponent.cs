using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.ViewComponents;

/// <summary>
/// Renders the notification bell shown in the navbar for every signed-in user,
/// with a recent-count badge and a dropdown of the latest notifications.
/// </summary>
public class NotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;

    public NotificationsViewComponent(INotificationService notifications)
        => _notifications = notifications;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        ViewBag.RecentCount = await _notifications.GetRecentCountAsync(7);
        var latest = await _notifications.GetRecentAsync(6);
        return View(latest);
    }
}
