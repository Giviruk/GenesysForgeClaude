using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

public class RotRuneboundShardApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, Guid CharacterId, ReferenceResponse Reference)>
        CreateCharacterAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var response = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Runes audit", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(
            Json.Options))!["id"];
        return (client, id, reference);
    }

    [Fact]
    public async Task Reference_ContainsExactlySeventeenPricelessNonTradeableShards()
    {
        var (_, _, reference) = await CreateCharacterAsync();
        var shards = reference.Items.Where(x => x.Shard is not null).ToList();

        Assert.Equal(17, shards.Count);
        Assert.Equal(17, shards.Select(x => x.Shard!.Code).Distinct().Count());
        Assert.All(shards, item =>
        {
            Assert.Null(item.Price);
            Assert.Null(item.Rarity);
            Assert.False(item.Purchasable);
            Assert.False(item.Sellable);
            Assert.Equal("Runes", item.Shard!.RequiredMagicSkill);
            Assert.Equal(1, item.Shard.MinimumSkillRank);
        });
    }

    [Fact]
    public async Task Shard_CannotBeBoughtOrStacked_ButCanBeGrantedOnce()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var shard = reference.Items.Single(x => x.Shard?.Code == "arcane-bolt-rune");

        var purchase = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(shard.Id, 1, ItemState.Carried), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, purchase.StatusCode);

        var stack = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(shard.Id, 2, ItemState.Carried, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, stack.StatusCode);

        var grant = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(shard.Id, 1, ItemState.Equipped, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);
        var itemId = (await grant.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(
            Json.Options))!["id"];
        var item = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!.Items!.Single(x => x.Id == itemId);
        Assert.NotNull(item.Shard);
        Assert.False(item.Sellable);
    }

    [Fact]
    public async Task LesserRune_ConfigurationIsPersistedAndImmutable()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var lesser = reference.Items.Single(x => x.Shard?.Code == "lesser-rune");
        var grant = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(lesser.Id, 1, ItemState.Equipped, Free: true), Json.Options);
        var itemId = (await grant.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(
            Json.Options))!["id"];

        var configure = await client.PutAsJsonAsync(
            $"/api/characters/{id}/items/{itemId}/lesser-rune",
            new SetLesserRuneConfigurationRequest(
                "Зажигает небольшой путевой огонь", "Attack", "Range"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, configure.StatusCode);

        var item = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!.Items!.Single(x => x.Id == itemId);
        Assert.False(item.Shard!.Pending);
        Assert.Equal("Attack", item.Shard.EffectAction);
        Assert.Equal("Range", item.Shard.EffectChoice);

        var second = await client.PutAsJsonAsync(
            $"/api/characters/{id}/items/{itemId}/lesser-rune",
            new SetLesserRuneConfigurationRequest("Другой эффект", "Attack", "Range"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
