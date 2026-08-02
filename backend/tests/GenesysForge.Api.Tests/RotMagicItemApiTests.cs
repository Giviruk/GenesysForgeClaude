using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-MITEM-01: каталожные магические предметы — реликвии, а не товар витрины.
/// </summary>
public class RotMagicItemApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid Id)> CreateAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Хранитель реликвий", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, reference, id);
    }

    private static ItemDefDto MagicItem(ReferenceResponse reference, string code) =>
        reference.Items.Single(i => i.Code.EndsWith($".{code}", StringComparison.Ordinal));

    [Fact]
    public async Task AllSeventeenRelics_ArePricelessAndOutOfTheShop()
    {
        var (_, reference, _) = await CreateAsync();

        var relics = reference.Items
            .Where(i => MagicItemRules.IsMagicItem(i.Code)).ToList();

        Assert.Equal(MagicItemRules.Count, relics.Count);
        Assert.All(relics, i =>
        {
            // Ноль монет делал реликвию бесплатным товаром витрины; «цены нет» — это null.
            Assert.Null(i.Price);
            Assert.False(i.Purchasable);
            Assert.False(i.Sellable);
            // Редкость у реликвий в таблице есть и остаётся — в отличие от рун.
            Assert.NotNull(i.Rarity);
        });
    }

    [Fact]
    public async Task Relic_KeepsItsTableStats()
    {
        var (_, reference, _) = await CreateAsync();

        var sword = MagicItem(reference, "soulbound-sword");
        Assert.Equal(ItemKind.Weapon, sword.Kind);
        Assert.Equal(3, sword.Encumbrance);
        Assert.Equal(10, sword.Rarity);
        Assert.Equal("+6", sword.Damage);
        Assert.Equal("2", sword.Crit);

        var talisman = MagicItem(reference, "warding-talisman");
        Assert.Equal(ItemKind.Gear, talisman.Kind);
        Assert.Equal(0, talisman.Encumbrance);
        Assert.Equal(6, talisman.Rarity);
    }

    [Fact]
    public async Task Relic_CannotBeBought_ButTheGmCanAwardIt()
    {
        var (client, reference, id) = await CreateAsync();
        var boots = MagicItem(reference, "winged-boots");

        var bought = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(boots.Id, 1, ItemState.Carried), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, bought.StatusCode);
        Assert.Equal("item.not_purchasable",
            (await bought.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // Выдача ведущим — обычный путь получения реликвии.
        var awarded = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(boots.Id, 1, ItemState.Carried, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, awarded.StatusCode);
    }

    [Fact]
    public async Task AwardedRelic_CannotBeSold()
    {
        var (client, reference, id) = await CreateAsync();
        var lantern = MagicItem(reference, "truelight-lantern");
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(lantern.Id, 1, ItemState.Carried, Free: true), Json.Options);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.ItemDefId == lantern.Id);
        var sold = await client.PostAsJsonAsync($"/api/characters/{id}/items/{item.Id}/sell",
            new SellItemRequest(1), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, sold.StatusCode);
        Assert.Equal("item.not_sellable",
            (await sold.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }
}
