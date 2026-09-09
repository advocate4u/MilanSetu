using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var items = await db.Conversations.AsNoTracking()
            .Where(x => x.UserAId == userId || x.UserBId == userId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                otherUserId = x.UserAId == userId ? x.UserBId : x.UserAId,
                x.CreatedAt,
                x.LastMessageAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("conversations/{otherUserId:guid}")]
    public async Task<IActionResult> OpenConversation(Guid otherUserId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (userId == otherUserId) return BadRequest(new { message = "You cannot start a conversation with yourself." });
        if (!await db.Users.AnyAsync(x => x.Id == otherUserId, ct)) return NotFound();
        if (await IsBlocked(userId, otherUserId, ct))
            return Conflict(new { message = "This conversation is unavailable." });

        var (a, b) = NormalizePair(userId, otherUserId);
        if (!await db.Connections.AnyAsync(x => x.UserAId == a && x.UserBId == b, ct))
            return Forbid();

        var conversation = await db.Conversations.SingleOrDefaultAsync(x => x.UserAId == a && x.UserBId == b, ct);
        if (conversation is null)
        {
            conversation = new Conversation { Id = Guid.NewGuid(), UserAId = a, UserBId = b };
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(ct);
        }

        return Ok(new { conversation.Id, otherUserId });
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> Messages(
        Guid conversationId,
        [FromQuery] int limit = MessagingRules.DefaultPageSize,
        [FromQuery] DateTimeOffset? before = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var conversation = await db.Conversations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == conversationId && (x.UserAId == userId || x.UserBId == userId), ct);
        if (conversation is null) return NotFound();

        limit = Math.Clamp(limit, 1, MessagingRules.MaxPageSize);
        var query = db.Messages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.DeletedAt == null);

        if (before.HasValue)
            query = query.Where(x => x.CreatedAt < before.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(x => new { x.Id, x.SenderUserId, x.Body, x.CreatedAt })
            .ToListAsync(ct);

        return Ok(items.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id));
    }

    [HttpPost("conversations/{conversationId:guid}")]
    public async Task<IActionResult> Send(Guid conversationId, SendMessageRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var body = request.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "Message cannot be empty." });
        if (body.Length > MessagingRules.MaxMessageLength)
            return BadRequest(new { message = "Message is too long." });

        var conversation = await db.Conversations.SingleOrDefaultAsync(
            x => x.Id == conversationId && (x.UserAId == userId || x.UserBId == userId), ct);
        if (conversation is null) return NotFound();

        var otherUserId = conversation.UserAId == userId ? conversation.UserBId : conversation.UserAId;
        if (await IsBlocked(userId, otherUserId, ct))
            return Conflict(new { message = "Messaging is unavailable for this connection." });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = userId,
            Body = body
        };

        conversation.LastMessageAt = message.CreatedAt;
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        return Ok(new { message.Id, message.SenderUserId, message.Body, message.CreatedAt });
    }

    [HttpDelete("conversations/{conversationId:guid}/{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid conversationId, Guid messageId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var message = await db.Messages.SingleOrDefaultAsync(
            x => x.Id == messageId && x.ConversationId == conversationId && x.SenderUserId == userId, ct);
        if (message is null) return NotFound();

        message.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> IsBlocked(Guid userId, Guid otherUserId, CancellationToken ct) =>
        await db.Blocks.AnyAsync(x =>
            (x.BlockerUserId == userId && x.BlockedUserId == otherUserId) ||
            (x.BlockerUserId == otherUserId && x.BlockedUserId == userId), ct);

    private static (Guid A, Guid B) NormalizePair(Guid first, Guid second) =>
        first.CompareTo(second) < 0 ? (first, second) : (second, first);

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}

public sealed record SendMessageRequest(string? Body);
