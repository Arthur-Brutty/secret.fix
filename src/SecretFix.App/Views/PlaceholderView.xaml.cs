using System.Windows.Controls;
using SecretFix.Core;

namespace SecretFix.Views;

public partial class PlaceholderView : UserControl
{
    public PlaceholderView(string title, PlanTier minimumPlan, bool allowed)
    {
        InitializeComponent();
        TitleText.Text = title;
        SubtitleText.Text = "Modulo reservado para uma proxima iteracao segura.";
        StateText.Text = allowed
            ? $"Disponivel no seu plano, mas ainda nao implementado. Plano minimo: {minimumPlan.ToString().ToUpperInvariant()}."
            : $"Bloqueado para o plano atual. Plano minimo: {minimumPlan.ToString().ToUpperInvariant()}.";
    }
}
