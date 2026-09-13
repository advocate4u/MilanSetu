using System.Security.Claims;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public sealed class ReviewerAuthorizationService(MilanSetuDbContext db)
{
    public async Task<UserRole?> GetRoleAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)) return null;
        return await db.UserRoleAssignments.AsNoTracking().Where(x => x.UserId == userId).Select(x => (UserRole?)x.Role).SingleOrDefaultAsync(ct);
    }

    public async Task<(Guid UserId, UserRole Role)?> GetAuthorizedActorAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)) return null;
        var role = await db.UserRoleAssignments.AsNoTracking().Where(x => x.UserId == userId).Select(x => (UserRole?)x.Role).SingleOrDefaultAsync(ct);
        return role is UserRole.Reviewer or UserRole.Admin ? (userId, role.Value) : null;
    }
}
