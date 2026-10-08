using System.Text.Json;
using System.Text.Json.Serialization;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

/// <summary>Adds history to the unit of work; the handler saves it with the definition.</summary>
public static class CustomContentAudit
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static void Created(IAppDbContext db, string type, Guid id, Guid? packId, string name, Guid userId) =>
        Record(db, type, id, packId, name, userId, CustomContentChangeAction.Created);

    public static void Deleted(IAppDbContext db, string type, Guid id, Guid? packId, string name, Guid userId) =>
        Record(db, type, id, packId, name, userId, CustomContentChangeAction.Deleted);

    public static void Updated(IAppDbContext db, string type, Guid id, Guid? packId, string name,
        Guid userId, object before, object after)
    {
        var a = JsonSerializer.SerializeToElement(before, JsonOptions);
        var b = JsonSerializer.SerializeToElement(after, JsonOptions);
        var changes = b.EnumerateObject().Where(p => p.Name != "id")
            .Select(p => new CustomContentFieldChangeDto(p.Name,
                a.TryGetProperty(p.Name, out var old) ? old.GetRawText() : null, p.Value.GetRawText()))
            .Where(x => x.From != x.To).ToList();
        if (changes.Count == 0) return;
        Record(db, type, id, packId, name, userId, CustomContentChangeAction.Updated,
            JsonSerializer.Serialize(changes, JsonOptions));
    }

    public static List<CustomContentFieldChangeDto> Changes(string json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CustomContentFieldChangeDto>>(json, JsonOptions) ?? [];

    private static void Record(IAppDbContext db, string type, Guid id, Guid? packId, string name,
        Guid userId, CustomContentChangeAction action, string changesJson = "") =>
        db.CustomContentChanges.Add(new CustomContentChange
        {
            Id = Guid.NewGuid(), HomebrewPackId = packId, DefinitionType = type, DefinitionId = id,
            DefinitionName = name, UserId = userId, Action = action, ChangesJson = changesJson,
        });
}
