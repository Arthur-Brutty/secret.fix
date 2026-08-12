using System.Windows;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;

namespace SecretFix;

public partial class MainWindow : Window
{
    private readonly WindowsInputService _input = new();
    private readonly BackupService _backup = new();
    private MouseSnapshot? _sessionMouseSnapshot;

    public MainWindow()
    {
        InitializeComponent();
        Opacity = 0;
        Loaded += (_, _) => BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        var page = element.Tag?.ToString() ?? "Mouse";
        PageTitle.Text = page switch
        {
            "Keyboard" => "TecladoFix",
            "FiveM" => "FiveM",
            "Flick" => "Flick",
            "Sensi" => "Sensi",
            "Aim" => "Mira",
            "Services" => "Serviços",
            "Display" => "Display",
            "Account" => "Account",
            _ => "MouseFix"
        };
        PageSubtitle.Text = page == "Mouse" ? "Precisão e resposta do mouse" : "Módulo em construção — use esta rota para a próxima iteração com Codex";
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _sessionMouseSnapshot ??= _input.ReadMouse();
            _backup.SaveMouse(_sessionMouseSnapshot);
            if (MousePrecision.IsChecked == true)
                _input.ApplyLinearMouse(10);
            StatusText.Text = "Configurações selecionadas aplicadas. Backup salvo em LocalAppData/SecretFix/backups.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Falha: {ex.Message}";
        }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionMouseSnapshot is null)
        {
            StatusText.Text = "Nenhum snapshot desta sessão para restaurar.";
            return;
        }

        try
        {
            _input.Restore(_sessionMouseSnapshot);
            StatusText.Text = "Configuração do mouse restaurada para o estado anterior desta sessão.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Falha ao restaurar: {ex.Message}";
        }
    }
}
