using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminProfileController(MilanSetuDbContext db, ReviewerAuthorizationService authorization) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<IActionResult> Profiles([FromQuery] string? search, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct);
        if (actor is null || actor.Value.Role != UserRole.Admin) return Forbid();

        take = Math.Clamp(take, 1, 100);
        var query = db.Users.AsNoTracking().Include(x => x.Profile).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(term) ||
                                     (x.PhoneNumber != null && x.PhoneNumber.Contains(search.Trim())) ||
                                     (x.Profile != null && x.Profile.DisplayName.ToLower().Contains(term)));
        }

        var users = await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(ct);
        return Ok(users.Select(x => new
        {
            userId = x.Id,
            email = x.Email,
            phoneNumber = x.PhoneNumber,
            isActive = x.IsActive,
            isEmailVerified = x.IsEmailVerified,
            isPhoneVerified = x.IsPhoneVerified,
            createdAt = x.CreatedAt,
            profile = x.Profile is null ? null : new
            {
                x.Profile.Id,
                x.Profile.DisplayName,
                x.Profile.DateOfBirth,
                x.Profile.Gender,
                x.Profile.AccountType,
                x.Profile.MaritalStatus,
                x.Profile.MotherTongue,
                x.Profile.Bio,
                x.Profile.Visibility,
                x.Profile.CreatedAt,
                x.Profile.UpdatedAt
            }
        }));
    }

    [HttpGet("profiles/{userId:guid}/relationships")]
    public async Task<IActionResult> RelationshipAudit(Guid userId, CancellationToken ct)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct);
        if (actor is null || actor.Value.Role != UserRole.Admin) return Forbid();

        var user = await db.Users.AsNoTracking()
            .Include(x => x.Profile).ThenInclude(x => x!.Locations).ThenInclude(x => x.Location)
            .Include(x => x.Profile).ThenInclude(x => x!.Education)
            .Include(x => x.Profile).ThenInclude(x => x!.Employment)
            .Include(x => x.Profile).ThenInclude(x => x!.FamilyDetails)
            .Include(x => x.Profile).ThenInclude(x => x!.Lifestyle)
            .SingleOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null) return NotFound();

        var outgoing = await db.Interests.AsNoTracking()
            .Where(x => x.SenderUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, targetUserId = x.ReceiverUserId, status = x.Status.ToString(), x.CreatedAt, x.RespondedAt })
            .ToListAsync(ct);

        var incoming = await db.Interests.AsNoTracking()
            .Where(x => x.ReceiverUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, sourceUserId = x.SenderUserId, status = x.Status.ToString(), x.CreatedAt, x.RespondedAt })
            .ToListAsync(ct);

        var ignored = await db.Ignores.AsNoTracking()
            .Where(x => x.IgnorerUserId == userId || x.IgnoredUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.IgnorerUserId, x.IgnoredUserId, x.CreatedAt })
            .ToListAsync(ct);

        var blocked = await db.Blocks.AsNoTracking()
            .Where(x => x.BlockerUserId == userId || x.BlockedUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.BlockerUserId, x.BlockedUserId, x.CreatedAt })
            .ToListAsync(ct);

        var connections = await db.Connections.AsNoTracking()
            .Where(x => x.UserAId == userId || x.UserBId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                matchedUserId = x.UserAId == userId ? x.UserBId : x.UserAId,
                x.CreatedAt
            })
            .ToListAsync(ct);

        var targetIds = outgoing.Select(x => x.targetUserId)
            .Concat(incoming.Select(x => x.sourceUserId))
            .Concat(ignored.SelectMany(x => new[] { x.IgnorerUserId, x.IgnoredUserId }))
            .Concat(blocked.SelectMany(x => new[] { x.BlockerUserId, x.BlockedUserId }))
            .Concat(connections.Select(x => x.matchedUserId))
            .Where(x => x != userId).Distinct().ToArray();

        var targetProfiles = await db.Users.AsNoTracking()
            .Where(x => targetIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Email, x.PhoneNumber, displayName = x.Profile == null ? null : x.Profile.DisplayName })
            .ToDictionaryAsync(x => x.Id, ct);

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.IsEmailVerified,
                user.IsPhoneVerified,
                user.CreatedAt,
                profile = user.Profile
            },
            summary = new
            {
                accepted = outgoing.Count(x => x.status == nameof(InterestStatus.Accepted)) + incoming.Count(x => x.status == nameof(InterestStatus.Accepted)),
                rejected = outgoing.Count(x => x.status == nameof(InterestStatus.Declined)) + incoming.Count(x => x.status == nameof(InterestStatus.Declined)),
                pending = outgoing.Count(x => x.status == nameof(InterestStatus.Pending)) + incoming.Count(x => x.status == nameof(InterestStatus.Pending)),
                ignored = ignored.Count,
                blocked = blocked.Count,
                matches = connections.Count
            },
            outgoingInterests = outgoing,
            incomingInterests = incoming,
            ignored,
            blocked,
            matches = connections,
            relatedProfiles = targetProfiles.Values
        });
    }
}
