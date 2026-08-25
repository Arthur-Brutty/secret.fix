using SecretFix.State;

namespace SecretFix.Core;

public sealed record ProfileChange(string Module, string Title, string Description, bool Supported = true);

public sealed record ProfileDefinition(OptimizationProfile Profile, string Description, IReadOnlyList<ProfileChange> MouseChanges, IReadOnlyList<ProfileChange> KeyboardChanges);

public static class ProfileCatalog
{
    public static ProfileDefinition Get(OptimizationProfile profile) => profile switch
    {
        OptimizationProfile.Competitive => new(
            profile,
            "Configurações de input seguras, reversíveis e adequadas para jogos.",
            [new("Mouse", "Aceleração do mouse", "Desativar aceleração do Windows."), new("Mouse", "Velocidade do ponteiro", "Manter o perfil linear em 10/20."), new("Mouse", "Thresholds", "Usar thresholds lineares do Windows.")],
            [new("Keyboard", "Repeat delay", "Usar delay mínimo do Windows."), new("Keyboard", "Repeat speed", "Usar repeat speed máximo do Windows."), new("Keyboard", "Accessibility", "Desativar Filter, Sticky e Toggle Keys.")]),
        OptimizationProfile.Custom => new(
            profile,
            "Usa apenas as opções selecionadas manualmente nas telas MouseFix e TecladoFix.",
            [new("Mouse", "Opções selecionadas", "Nenhuma alteração oculta será aplicada.")],
            [new("Keyboard", "Opções selecionadas", "Nenhuma alteração oculta será aplicada.")]),
        _ => new(
            OptimizationProfile.Balanced,
            "Configurações conservadoras e reversíveis para uso geral e jogos.",
            [new("Mouse", "Aceleração do mouse", "Desativar aceleração do Windows."), new("Mouse", "Velocidade do ponteiro", "Manter velocidade atual."), new("Mouse", "Thresholds", "Usar thresholds lineares do Windows.")],
            [new("Keyboard", "Repeat delay", "Usar delay mínimo do Windows."), new("Keyboard", "Accessibility", "Desativar Filter e Sticky Keys.")])
    };
}
