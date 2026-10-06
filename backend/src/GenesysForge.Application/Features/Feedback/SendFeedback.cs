using System.Net.Mail;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Feedback;

/// <summary>
/// Форма обратной связи. <c>Website</c> — скрытое поле-ловушка: люди его не видят и не заполняют,
/// а боты заполняют все поля подряд.
/// </summary>
public record SendFeedbackRequest(string Message, string? Email = null, string? Page = null, string? Website = null);

public record SendFeedbackCommand(Guid? UserId, SendFeedbackRequest Request) : ICommand<Unit>;

public class SendFeedbackHandler(IAppDbContext db, IEmailSender email) : ICommandHandler<SendFeedbackCommand, Unit>
{
    public const int MinLength = 5;
    public const int MaxLength = 4000;

    public async Task<Unit> Handle(SendFeedbackCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        // Бот заполнил ловушку: отвечаем успехом, чтобы он не подбирал обход, но письмо не шлём.
        if (!string.IsNullOrWhiteSpace(request.Website)) return Unit.Value;

        var text = (request.Message ?? "").Trim();
        if (text.Length < MinLength)
            throw new DomainRuleException("Напишите сообщение (не короче 5 символов).");
        if (text.Length > MaxLength)
            throw new DomainRuleException($"Сообщение слишком длинное: не больше {MaxLength} символов.");

        var replyTo = request.Email?.Trim();
        if (string.IsNullOrEmpty(replyTo)) replyTo = null;
        else if (replyTo.Length > 254 || !MailAddress.TryCreate(replyTo, out _))
            throw new DomainRuleException("Похоже, в адресе почты опечатка.");

        var accountEmail = command.UserId is { } userId
            ? await db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(ct)
            : null;
        var page = request.Page?.Trim() is { Length: > 0 } p ? p[..Math.Min(p.Length, 300)] : null;

        await email.SendFeedbackAsync(new FeedbackMessage(text, replyTo ?? accountEmail, accountEmail, page), ct);
        return Unit.Value;
    }
}
