using System.Text;

namespace GerenciadorTasks.Application.Mapping;

/// <summary>
/// Traduz entre os enums do domínio (PascalCase: InProgress, PersonalCare)
/// e as strings que o frontend usa (snake_case: in_progress, personal_care).
///
/// Por que mapear? O domínio é C# idiomático (enums PascalCase). A API fala
/// a língua do frontend (snake_case). O DTO é a fronteira onde a tradução
/// acontece — mantendo cada lado limpo no seu próprio padrão.
/// </summary>
public static class EnumMapper
{
    /// InProgress -> "in_progress", PersonalCare -> "personal_care", Low -> "low".
    public static string ToSnakeCase<T>(T value) where T : struct, Enum
    {
        var name = value.ToString();
        // Insere "_" antes de cada maiúscula (exceto a primeira) e tudo minúsculo.
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// "in_progress" -> InProgress. Lança se a string não casar com nenhum valor.
    public static T FromSnakeCase<T>(string snake) where T : struct, Enum
    {
        var pascal = string.Concat(snake
            .Split('_')
            .Select(word => word.Length == 0 ? "" : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));

        return Enum.Parse<T>(pascal, ignoreCase: true);
    }
}
