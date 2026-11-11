
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FuelControl;

public class App : Application
{
    public App()
    {
        if (ContextoTurno.Turno is "A" or "B" or "C")
            AbrirSistema();
        else
            MainPage = CriarTelaTurno();

        _ = VerificarFirebaseAsync();
    }

    private async Task VerificarFirebaseAsync()
    {
        bool conectado =
            await FirebaseSyncService.ConectarAnonimamenteAsync();

        if (!conectado)
        {
            string erro = Preferences.Default.Get(
                "firebase_erro",
                "O Firebase não informou o motivo do erro.");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await MainPage.DisplayAlert(
                    "Erro de conexão Firebase",
                    erro,
                    "OK");
            });
        }
    }

    private ContentPage CriarTelaTurno()
    {
        var titulo = new Label
        {
            Text = "FUELCONTROL",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitulo = new Label
        {
            Text = "Selecione seu turno",
            FontSize = 20,
            HorizontalOptions = LayoutOptions.Center
        };

        var layout = new VerticalStackLayout
        {
            Padding = 30,
            Spacing = 25,
            VerticalOptions = LayoutOptions.Center,
            Children = { titulo, subtitulo }
        };

        foreach (string turno in new[] { "A", "B", "C" })
        {
            var botao = new Button
            {
                Text = "TURNO " + turno,
                FontSize = 20,
                HeightRequest = 65,
                CornerRadius = 12
            };

            botao.Clicked += (sender, args) =>
            {
                ContextoTurno.Turno = turno;
                AbrirSistema();
            };

            layout.Children.Add(botao);
        }

        return new ContentPage
        {
            Title = "Seleção de turno",
            Content = layout
        };
    }

    private void AbrirSistema()
    {
        MainPage = new NavigationPage(new FuelControl.MainPage());
    }
}
