
using Microsoft.Maui.Controls;
using System.Globalization;

namespace FuelControl;

public class MainPage : ContentPage
{
    public MainPage()
    {
        Title = "FuelControl";
        BackgroundColor = Color.FromArgb("#F1F5F9");

        var titulo = new Label
        {
            Text = "FUELCONTROL",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65"),
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitulo = new Label
        {
            Text = "Controle de consumo de combustível",
            FontSize = 14,
            TextColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Center
        };

        // Método para criar os campos
        Entry CriarCampo(string texto)
        {
            return new Entry
            {
                Placeholder = texto,
                TextColor = Colors.Black,
                PlaceholderColor = Color.FromArgb("#64748B"),
                BackgroundColor = Colors.White,
                Keyboard = Keyboard.Numeric,
                HeightRequest = 55,
                Margin = new Thickness(0, 5)
            };
        }

        // Veículo
        var veiculo = new Entry
        {
            Placeholder = "Nome ou identificação do veículo",
            TextColor = Colors.Black,
            PlaceholderColor = Color.FromArgb("#64748B"),
            BackgroundColor = Colors.White,
            HeightRequest = 55
        };

        // Campos numéricos
        var kmAnterior = CriarCampo("Quilometragem anterior");
        var kmAtual = CriarCampo("Quilometragem atual");
        var litros = CriarCampo("Litros abastecidos");
        var valor = CriarCampo("Valor total (R$)");

        // Resultado do consumo
        var resultado = new Label
        {
            Text = "Consumo médio: -- km/l",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#166534")
        };

        // Resultado do custo
        var custo = new Label
        {
            Text = "Custo por km: --",
            FontSize = 18,
            TextColor = Color.FromArgb("#123B65")
        };

        // Botão calcular
        var botao = new Button
        {
            Text = "CALCULAR CONSUMO",
            BackgroundColor = Color.FromArgb("#123B65"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 55,
            CornerRadius = 10
        };

        // Evento de cálculo
        botao.Clicked += async (s, e) =>
        {
            bool Ler(string texto, out double numero)
            {
                return double.TryParse(
                    texto?.Trim().Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out numero);
            }

            if (!Ler(kmAnterior.Text, out double anterior) ||
                !Ler(kmAtual.Text, out double atual) ||
                !Ler(litros.Text, out double l))
            {
                await DisplayAlert(
                    "Atenção",
                    "Preencha as quilometragens e os litros.",
                    "OK");

                return;
            }

            if (anterior < 0 || atual <= anterior || l <= 0)
            {
                await DisplayAlert(
                    "Dados inválidos",
                    "A quilometragem atual deve ser maior que a anterior e os litros devem ser positivos.",
                    "OK");

                return;
            }

            double distancia = atual - anterior;
            double consumo = distancia / l;

            resultado.Text =
                $"Consumo médio: {consumo:F2} km/l";

            if (Ler(valor.Text, out double total) && total > 0)
            {
                custo.Text =
                    $"Custo por km: R$ {(total / distancia):F2}";
            }
            else
            {
                custo.Text = "Custo por km: não informado";
            }
        };

        // Layout principal
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 22,
                Spacing = 15,

                Children =
                {
                    titulo,
                    subtitulo,

                    new Label
                    {
                        Text = "DADOS DO ABASTECIMENTO",
                        TextColor = Color.FromArgb("#123B65"),
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 16
                    },

                    veiculo,
                    kmAnterior,
                    kmAtual,
                    litros,
                    valor,
                    botao,

                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Colors.LightGray
                    },

                    resultado,
                    custo
                }
            }
        };
    }
}

