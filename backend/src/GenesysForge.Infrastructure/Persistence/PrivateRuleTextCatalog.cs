using System.Reflection;
using System.Text.Json;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>Optional private prose. Public artifacts contain only structural catalog data.</summary>
internal static class PrivateRuleTextCatalog
{
    internal sealed record Text(string Desc = "", string Safe = "", string DescEn = "",
        string Notes = "", string Requirement = "", string ActivationCost = "",
        string Activation = "", string Duration = "", string Frequency = "", string Trigger = "",
        List<Text>? Upgrades = null);

    private static readonly Dictionary<string, Text> Default = Load(typeof(PrivateRuleTextCatalog).Assembly);

    internal static Text Get(string code, Assembly? assembly = null) =>
        (assembly is null || assembly == typeof(PrivateRuleTextCatalog).Assembly ? Default : Load(assembly))
        .GetValueOrDefault(code) ?? new Text();

    private static Dictionary<string, Text> Load(Assembly assembly)
    {
        var name = assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith("rule-text.ru.json"));
        if (name is null) return [];
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<Dictionary<string, Text>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}
