using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CreateCustomCareerRequest(
    GameSystem System,
    string Name,
    string? NameRu,
    string? Description,
    List<string> CareerSkillNames,
    int StartingMoneyFixed,
    string? StartingMoneyDice);
