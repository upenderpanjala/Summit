using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
        => _notifications = notifications;

    // Visible to every signed-in user — the shared activity feed.
    public async Task<IActionResult> Index()
        => View(await _notifications.GetRecentAsync(50));
}
