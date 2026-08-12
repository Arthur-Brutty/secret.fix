using System.Windows;
using System.Windows.Controls;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class ServicesView : UserControl
{
    public ServicesView(bool allowed, PlanTier minimumPlan)
    {
        InitializeComponent();
        if (!allowed)
            NotificationService.Show($"Serviços requer {minimumPlan.ToString().ToUpperInvariant()}+.");
    }

    private void Checked(object sender, RoutedEventArgs e) => NotificationService.Show("Serviço marcado como experimental.");
}
