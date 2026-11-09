using Microsoft.Maui.Controls;

namespace FuelControl;

public class MainPage : ContentPage
{
    public MainPage()
    {
        Title = "FuelControl";
        BackgroundColor = Color.FromArgb("#F2F5F9");

        var titulo = new Label
        {
            Text = "FUELCONTROL",
            FontSize = 27,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65"),
            HorizontalOptions = LayoutOptions.Center
        };

        var veiculo = new Entry
        {
            Placeholder = "Nome do veículo"
        };

        var km = new Entry
        {
            Placeholder = "Quilometragem atual",
            Keyboard = Keyboard.Numeric
        };

        var litros = new Entry
        {
            Placeholder = "Litros abastecidos",
            Keyboard = Keyboard.Numeric
        };

        var valor = new Entry
        {
            Placeholder = "Valor total (R$)",
            Keyboard = Keyboard.Numeric
        };

        var resultado = new Label
        {
            Text = "Consumo médio: -- km/l",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkGreen
        };

        var botao = new Button
        {
            Text = "CALCULAR CONSUMO",
            BackgroundColor = Color.FromArgb("#123B65"),
            TextColor = Colors.White
        };

        botao.Clicked += (s, e) =>
        {
            if (double.TryParse(km.Text, out double k) &&
                double.TryParse(litros.Text, out double l) &&
                l > 0)
            {
                resultado.Text =
                    $"Consumo informado: {k / l:F2} km/l";
            }
            else
            {
                DisplayAlert("Atenção",
                    "Informe quilometragem e litros válidos.",
                    "OK");
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 25,
                Spacing = 18,
                Children =
                {
                    titulo,
                    veiculo,
                    km,
                    litros,
                    valor,
                    botao,
                    resultado
                }
            }
        };
    }
}
