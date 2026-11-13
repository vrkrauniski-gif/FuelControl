
using Microsoft.Maui.Controls;

namespace FuelControl;

public class App : Application
{
    public App()
    {
        MainPage = CriarTelaInicial();
    }

    private ContentPage CriarTelaInicial()
    {
        var titulo = new Label
        {
            Text = "FUELCONTROL",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitulo = new Label
        {
            Text = "Acesso ao sistema",
            FontSize = 20,
            HorizontalOptions = LayoutOptions.Center
        };

        var layout = new VerticalStackLayout
        {
            Padding = 30,
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children = { titulo, subtitulo }
        };

        if (!ContextoTurno.AcessoConfigurado)
        {
            var configurar = new Button
            {
                Text = "CONFIGURAR ACESSOS",
                HeightRequest = 60
            };

            configurar.Clicked += async (s, e) =>
                await ConfigurarAcessosAsync();

            layout.Children.Add(configurar);
        }
        else
        {
            var admin = new Button
            {
                Text = "ADMINISTRADOR",
                HeightRequest = 60
            };

            admin.Clicked += async (s, e) =>
                await EntrarAdministradorAsync();

            layout.Children.Add(admin);

            foreach (string turno in new[] { "A", "B", "C" })
            {
                string selecionado = turno;

                var botao = new Button
                {
                    Text = "TURNO " + selecionado,
                    HeightRequest = 60
                };

                botao.Clicked += async (s, e) =>
                    await EntrarTurnoAsync(selecionado);

                layout.Children.Add(botao);
            }
        }

        return new ContentPage
        {
            Title = "FuelControl",
            Content = layout
        };
    }

    private async Task ConfigurarAcessosAsync()
    {
        string admin = await MainPage!.DisplayPromptAsync(
            "Administrador",
            "Crie a senha geral (mínimo 6 caracteres):",
            "Continuar",
            "Cancelar",
            maxLength: 30);

        if (string.IsNullOrWhiteSpace(admin) || admin.Length < 6)
        {
            await MainPage!.DisplayAlert(
                "Atenção",
                "A senha deve ter pelo menos 6 caracteres.",
                "OK");
            return;
        }

        string pinA = await SolicitarPinAsync("A");
        if (string.IsNullOrEmpty(pinA)) return;

        string pinB = await SolicitarPinAsync("B");
        if (string.IsNullOrEmpty(pinB)) return;

        string pinC = await SolicitarPinAsync("C");
        if (string.IsNullOrEmpty(pinC)) return;

        ContextoTurno.ConfigurarAcessos(
            admin, pinA, pinB, pinC);

        await MainPage!.DisplayAlert(
            "Sucesso",
            "Acessos configurados neste aparelho.",
            "OK");

        MainPage = CriarTelaInicial();
    }

    private async Task<string> SolicitarPinAsync(string turno)
    {
        string pin = await MainPage!.DisplayPromptAsync(
            "PIN do turno " + turno,
            "Crie um PIN numérico de 4 a 8 dígitos:",
            "Confirmar",
            "Cancelar",
            maxLength: 8,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(pin) ||
            pin.Length < 4 ||
            !pin.All(char.IsDigit))
        {
            await MainPage!.DisplayAlert(
                "PIN inválido",
                "Informe de 4 a 8 números.",
                "OK");

            return "";
        }

        return pin;
    }

    private async Task EntrarTurnoAsync(string turno)
    {
        string pin = await MainPage!.DisplayPromptAsync(
            "Acesso ao turno " + turno,
            "Digite o PIN do turno:",
            "Entrar",
            "Cancelar",
            maxLength: 8,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(pin))
            return;

        if (!ContextoTurno.ValidarTurno(turno, pin))
        {
            await MainPage!.DisplayAlert(
                "Acesso negado",
                "PIN incorreto.",
                "OK");
            return;
        }

        ContextoTurno.EhAdministrador = false;
        ContextoTurno.Turno = turno;

        AbrirSistema();
    }

    private async Task EntrarAdministradorAsync()
    {
        string senha = await MainPage!.DisplayPromptAsync(
            "Administrador",
            "Digite a senha geral:",
            "Entrar",
            "Cancelar",
            maxLength: 30);

        if (string.IsNullOrEmpty(senha))
            return;

        if (!ContextoTurno.ValidarAdministrador(senha))
        {
            await MainPage!.DisplayAlert(
                "Acesso negado",
                "Senha incorreta.",
                "OK");
            return;
        }

        string escolha = await MainPage!.DisplayActionSheet(
            "Selecione o turno de operação",
            "Cancelar",
            null,
            "Turno A",
            "Turno B",
            "Turno C");

        string turno = escolha switch
        {
            "Turno A" => "A",
            "Turno B" => "B",
            "Turno C" => "C",
            _ => ""
        };

        if (string.IsNullOrEmpty(turno))
            return;

        ContextoTurno.EhAdministrador = true;
        ContextoTurno.Turno = turno;

        AbrirSistema();
    }

    private void AbrirSistema()
    {
        MainPage = new NavigationPage(
            new FuelControl.MainPage());
    }
}
