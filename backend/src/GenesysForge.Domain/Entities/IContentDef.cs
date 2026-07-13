namespace GenesysForge.Domain.Entities;

/// <summary>
/// Общая content-model справочной сущности: стабильный код, русское/оригинальное имя,
/// полное (private) и safe (public) описания и ссылка на источник. Используется seed-pipeline'ами
/// для проекции контента под <see cref="ContentMode"/>.
/// </summary>
public interface IContentDef
{
    /// <summary>Стабильный код встроенного контента (ключ для private-набора описаний).</summary>
    string Code { get; }
    string NameRu { get; set; }
    /// <summary>Полное (private) описание. В режиме PublicSafe очищается seed-pipeline'ом.</summary>
    string Description { get; set; }
    /// <summary>Copyright-safe описание для публичной версии.</summary>
    string SafeDescription { get; set; }
    /// <summary>Английское описание — собственный copyright-safe парафраз (в обоих режимах).</summary>
    string DescriptionEn { get; set; }
    /// <summary>Ссылка на источник (доступна в обоих режимах).</summary>
    string Source { get; set; }
}
