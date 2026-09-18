using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/users")]
public sealed class AdminUsersController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? active, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        search = search?.Trim();

        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Length > 120 ? search[..120] : search;
            query = query.Where(x => x.Email.Contains(term) || (x.PhoneNumber != null && x.PhoneNumber.Contains(term)));
        }
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Email, x.PhoneNumber, x.IsActive, x.IsEmailVerified, x.IsPhoneVerified, x.CreatedAt, x.UpdatedAt,
                role = db.UserRoleAssignments.Where(r => r.UserId == x.Id).Select(r => r.Role.ToString()).SingleOrDefault() ?? "User" })
            .ToListAsync(ct);

        await Audit(actor.Value, "admin.users.view", null, new { search, active, page, pageSize }, ct);
        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();

        var item = await db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Email, x.PhoneNumber, x.IsActive, x.IsEmailVerified, x.IsPhoneVerified, x.CreatedAt, x.UpdatedAt,
                role = db.UserRoleAssignments.Where(r => r.UserId == x.Id).Select(r => r.Role.ToString()).SingleOrDefault() ?? "User",
                profile = x.Profile == null ? null : new { x.Profile.DisplayName, x.Profile.Gender, x.Profile.DateOfBirth, x.Profile.Visibility } })
            .SingleOrDefaultAsync(ct);

        if (item is null) return NotFound();
        await Audit(actor.Value, "admin.user.view", id, null, ct);
        return Ok(item);
    }

    [HttpPost("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, UserRoleRequest input, CancellationToken ct)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();
        if (actor.Value == id) return BadRequest(new { message = "An administrator cannot change their own role." });
        var actorRole = await db.UserRoleAssignments.AsNoTracking().Where(x => x.UserId == actor.Value).Select(x => (UserRole?)x.Role).SingleOrDefaultAsync(ct);
        if (actorRole == UserRole.Admin && input.Role == UserRole.SuperAdmin) return Forbid();
        if (actorRole == UserRole.Admin && await db.UserRoleAssignments.AsNoTracking().AnyAsync(x => x.UserId == id && x.Role == UserRole.SuperAdmin, ct)) return Forbid();
        if (!Enum.IsDefined(input.Role)) return BadRequest(new { message = "Invalid role." });
        if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == id, ct)) return NotFound();
        var assignment = await db.UserRoleAssignments.SingleOrDefaultAsync(x => x.UserId == id, ct);
        if (assignment is null) db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = id, Role = input.Role, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        else { assignment.Role = input.Role; assignment.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
        await Audit(actor.Value, "admin.user.role-change", id, new { role = input.Role.ToString() }, ct);
        return Ok(new { id, role = input.Role.ToString() });
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, UserStatusRequest input, CancellationToken ct)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();
        if (actor.Value == id) return BadRequest(new { message = "An administrator cannot deactivate their own account." });

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        user.IsActive = input.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await Audit(actor.Value, input.Active ? "admin.user.activate" : "admin.user.deactivate", id, null, ct);
        return Ok(new { id, active = user.IsActive });
    }

    private async Task<Guid?> GetAdminId(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var id)) return null;
        var role = await db.UserRoleAssignments.AsNoTracking().Where(x => x.UserId == id)
            .Select(x => (UserRole?)x.Role).SingleOrDefaultAsync(ct);
        return role is UserRole.Admin or UserRole.SuperAdmin ? id : null;
    }

    private async Task Audit(Guid actor, string action, Guid? resourceId, object? metadata, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actor, Action = action, ResourceType = "User", ResourceId = resourceId,
            Metadata = metadata == null ? null : JsonSerializer.Serialize(metadata), CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}

public sealed record UserStatusRequest(bool Active);
public sealed record UserRoleRequest(UserRole Role);
