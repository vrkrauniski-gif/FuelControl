
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Globalization;
using System.Text.Json;

#if ANDROID
using AndroidPaint = Android.Graphics.Paint;
using AndroidColor = Android.Graphics.Color;
using AndroidPdfDocument = Android.Graphics.Pdf.PdfDocument;
#endif

namespace FuelControl;

public class MainPage : ContentPage
{
    const string Chave = "fuelcontrol_historico";

    List<Abastecimento> historico = new();

    readonly CultureInfo pt = CultureInfo.GetCultureInfo("pt-BR");

    Label lblAbastecimentos = new();
    Label lblGasto = new();
    Label lblKm = new();
    Label lblMedia = new();

    Label lblResultado = new();
    Label lblCusto = new();
    Label lblTotalCombustivel = new();
    Label lblResultadoArla = new();

    VerticalStackLayout lista = new();
    VerticalStackLayout painelCarro = new();
    VerticalStackLayout painelArla = new();

    Entry veiculo = new();
    Entry motorista = new();
    Entry kmAnterior = new();
    Entry kmAtual = new();

    Entry litros = new();
    Entry precoLitro = new();
    Entry litrosArla = new();

    Picker tipoVeiculo = new();
    Picker tipoCombustivel = new();

    CheckBox usarArla = new();

    public MainPage()
    {
        Title = "FuelControl";
        BackgroundColor = Color.FromArgb("#F1F5F9");

        CarregarHistorico();

        Content = CriarTela();

        AtualizarDashboard();
        AtualizarHistorico();
    }

    Entry Campo(string nome, Keyboard teclado)
    {
        return new Entry
        {
            Placeholder = nome,
            TextColor = Colors.Black,
            PlaceholderColor = Color.FromArgb("#64748B"),
            BackgroundColor = Colors.White,
            Keyboard = teclado,
            HeightRequest = 55,
            Margin = new Thickness(0, 4)
        };
    }

    Picker CriarPicker()
    {
        return new Picker
        {
            TextColor = Colors.Black,
            TitleColor = Colors.Gray,
            BackgroundColor = Colors.White,
            HeightRequest = 55,
            Margin = new Thickness(0, 4)
        };
    }

    Label Titulo(string texto)
    {
        return new Label
        {
            Text = texto,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65"),
            Margin = new Thickness(0, 12, 0, 5)
        };
    }

    Label Rotulo(string texto)
    {
        return new Label
        {
            Text = texto,
            TextColor = Color.FromArgb("#334155"),
            FontSize = 14
        };
    }

    Label Indicador(string texto)
    {
        return new Label
        {
            Text = texto,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#123B65")
        };
    }

    Button Botao(string texto)
    {
        return new Button
        {
            Text = texto,
            BackgroundColor = Color.FromArgb("#123B65"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 55,
            CornerRadius = 10
        };
    }

    View Cartao(string titulo, Label valorCartao)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCE4ED"),
            Padding = 13,
            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    new Label
                    {
                        Text = titulo,
                        FontSize = 12,
                        TextColor = Colors.Gray
                    },
                    valorCartao
                }
            }
        };
    }

    Grid CriarDashboardGrid()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        grid.Add(Cartao("Abastecimentos", lblAbastecimentos), 0, 0);
        grid.Add(Cartao("Gasto total", lblGasto), 1, 0);
        grid.Add(Cartao("KM percorridos", lblKm), 0, 1);
        grid.Add(Cartao("Consumo médio", lblMedia), 1, 1);

        return grid;
    }

    void AtualizarTipoVeiculo(object? sender, EventArgs e)
    {
        bool caminhão =
            tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

        tipoCombustivel.ItemsSource = caminhão
            ? new List<string> { "Diesel S10" }
            : new List<string> { "Etanol", "Gasolina" };

        tipoCombustivel.SelectedIndex = 0;

        painelCarro.IsVisible = !caminhão;
        usarArla.IsVisible = caminhão;
        painelArla.IsVisible = caminhão && usarArla.IsChecked;

        if (!caminhão)
            usarArla.IsChecked = false;
    }

    void AtualizarPainelArla(object? sender, CheckedChangedEventArgs e)
    {
        bool caminhão =
            tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

        painelArla.IsVisible = caminhão && e.Value;
    }

    View CriarTela()
    {
        veiculo = Campo("Placa ou identificação do veículo", Keyboard.Default);
        motorista = Campo("Nome do motorista", Keyboard.Default);

        kmAnterior = Campo("Quilometragem anterior", Keyboard.Numeric);
        kmAtual = Campo("Quilometragem atual", Keyboard.Numeric);

        litros = Campo("Litros de combustível", Keyboard.Numeric);
        precoLitro = Campo("Preço por litro (R$)", Keyboard.Numeric);
        litrosArla = Campo("Litros de Arla 32", Keyboard.Numeric);

        tipoVeiculo = CriarPicker();
        tipoVeiculo.Title = "Escolha o tipo de veículo";
        tipoVeiculo.ItemsSource = new List<string>
        {
            "Carro",
            "Caminhão"
        };
        tipoVeiculo.SelectedIndex = 0;
        tipoVeiculo.SelectedIndexChanged += AtualizarTipoVeiculo;

        tipoCombustivel = CriarPicker();
        tipoCombustivel.Title = "Escolha o combustível";
        tipoCombustivel.ItemsSource = new List<string>
        {
            "Etanol",
            "Gasolina"
        };
        tipoCombustivel.SelectedIndex = 0;

        usarArla = new CheckBox
        {
            Color = Color.FromArgb("#123B65"),
            IsVisible = false
        };
        usarArla.CheckedChanged += AtualizarPainelArla;

        lblAbastecimentos = Indicador("0");
        lblGasto = Indicador("R$ 0,00");
        lblKm = Indicador("0 km");
        lblMedia = Indicador("-- km/l");

        lblResultado = new Label
        {
            Text = "Consumo médio: -- km/l",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#166534")
        };

        lblCusto = new Label
        {
            Text = "Custo por km: --",
            FontSize = 18,
            TextColor = Color.FromArgb("#123B65")
        };

        lblTotalCombustivel =
            Indicador("Total do combustível: R$ 0,00");

        lblResultadoArla = new Label
        {
            Text = "Consumo de Arla: --",
            FontSize = 17,
            TextColor = Color.FromArgb("#166534")
        };

        litros.TextChanged += (s, e) => AtualizarTotalCarro();
        precoLitro.TextChanged += (s, e) => AtualizarTotalCarro();

        painelCarro = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Rotulo("Preço por litro (R$)"),
                precoLitro,
                lblTotalCombustivel
            }
        };

        painelArla = new VerticalStackLayout
        {
            IsVisible = false,
            Spacing = 8,
            Children =
            {
                Titulo("ABASTECIMENTO DE ARLA 32"),
                Rotulo("Quantidade de Arla 32 (litros)"),
                litrosArla,
                lblResultadoArla
            }
        };

        var linhaArla = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                usarArla,
                new Label
                {
                    Text = "Abasteceu Arla 32?",
                    TextColor = Color.FromArgb("#334155"),
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };

        var calcular = Botao("CALCULAR E SALVAR");
        calcular.Clicked += Calcular;

        var pdf = Botao("GERAR E COMPARTILHAR PDF");
        pdf.Clicked += CompartilharPDF;

        var limpar = new Button
        {
            Text = "LIMPAR HISTÓRICO",
            BackgroundColor = Color.FromArgb("#B91C1C"),
            TextColor = Colors.White,
            HeightRequest = 48,
            CornerRadius = 10
        };

        limpar.Clicked += async (s, e) =>
        {
            bool confirmar = await DisplayAlert(
                "Confirmar",
                "Deseja apagar todos os abastecimentos salvos?",
                "Sim", "Não");

            if (!confirmar)
                return;

            historico.Clear();
            SalvarHistorico();
            AtualizarDashboard();
            AtualizarHistorico();
        };

        lista = new VerticalStackLayout
        {
            Spacing = 8
        };

        return new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "FUELCONTROL",
                        FontSize = 29,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#123B65"),
                        HorizontalOptions = LayoutOptions.Center
                    },

                    new Label
                    {
                        Text = "GESTÃO DE COMBUSTÍVEL",
                        FontSize = 13,
                        TextColor = Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center
                    },

                    Titulo("DASHBOARD"),
                    CriarDashboardGrid(),

                    Titulo("NOVO ABASTECIMENTO"),

                    Rotulo("Tipo de veículo"),
                    tipoVeiculo,

                    Rotulo("Tipo de combustível"),
                    tipoCombustivel,

                    linhaArla,

                    Rotulo("Identificação do veículo"),
                    veiculo,

                    Rotulo("Motorista"),
                    motorista,

                    Rotulo("Quilometragem anterior"),
                    kmAnterior,

                    Rotulo("Quilometragem atual"),
                    kmAtual,

                    Titulo("ABASTECIMENTO"),

                    Rotulo("Quantidade de litros"),
                    litros,

                    painelCarro,
                    painelArla,

                    calcular,
                    lblResultado,
                    lblCusto,

                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Colors.LightGray
                    },

                    Titulo("HISTÓRICO"),
                    lista,
                    pdf,
                    limpar,

                    new Label
                    {
                        Text = "FuelControl - Controle de consumo",
                        TextColor = Colors.Gray,
                        FontSize = 12,
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 15)
                    }
                }
            }
        };
    }

    void AtualizarTotalCarro()
    {
        if (Ler(litros.Text, out double qtd) &&
            Ler(precoLitro.Text, out double preco) &&
            qtd > 0 && preco > 0)
        {
            lblTotalCombustivel.Text =
                "Total do combustível: " +
                (qtd * preco).ToString("C", pt);
        }
        else
        {
            lblTotalCombustivel.Text =
                "Total do combustível: R$ 0,00";
        }
    }

    bool Ler(string? texto, out double numero)
    {
        if (double.TryParse(
            texto?.Trim(),
            NumberStyles.Number,
            pt,
            out numero))
            return true;

        return double.TryParse(
            texto?.Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out numero);
    }

    async void Calcular(object? sender, EventArgs e)
    {
        bool caminhão =
            tipoVeiculo.SelectedItem?.ToString() == "Caminhão";

        if (string.IsNullOrWhiteSpace(veiculo.Text) ||
            !Ler(kmAnterior.Text, out double anterior) ||
            !Ler(kmAtual.Text, out double atual) ||
            !Ler(litros.Text, out double quantidade))
        {
            await DisplayAlert(
                "Atenção",
                "Preencha veículo, quilometragens e litros corretamente.",
                "OK");
            return;
        }

        if (anterior < 0 || atual <= anterior || quantidade <= 0)
        {
            await DisplayAlert(
                "Dados inválidos",
                "Verifique as quilometragens e os litros.",
                "OK");
            return;
        }

        double preco = 0;

        if (!caminhão)
        {
            if (!Ler(precoLitro.Text, out preco) || preco <= 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Informe o preço por litro do combustível.",
                    "OK");
                return;
            }
        }

        bool temArla = caminhão && usarArla.IsChecked;
        double qtdArla = 0;

        if (temArla)
        {
            if (!Ler(litrosArla.Text, out qtdArla) || qtdArla <= 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Informe a quantidade de litros de Arla 32.",
                    "OK");
                return;
            }
        }

        double distancia = atual - anterior;
        double totalCombustivel = caminhão ? 0 : quantidade * preco;
        double consumo = distancia / quantidade;
        double custoKm = caminhão ? 0 : totalCombustivel / distancia;

        double consumoArlaKmL =
            temArla ? distancia / qtdArla : 0;

        double consumoArla100 =
            temArla ? qtdArla * 100 / distancia : 0;

        lblResultado.Text = caminhão
            ? $"Consumo Diesel S10: {consumo:F2} km/l"
            : $"Consumo {tipoCombustivel.SelectedItem}: {consumo:F2} km/l";

        lblCusto.Text = caminhão
            ? "Custo por km: não informado"
            : $"Custo por km: R$ {custoKm:F2}";

        lblResultadoArla.Text = temArla
            ? $"Arla: {consumoArlaKmL:F2} km/l | {consumoArla100:F2} L/100 km"
            : "Consumo de Arla: não informado";

        var registro = new Abastecimento
        {
            Data = DateTime.Now,
            Veiculo = veiculo.Text.Trim(),
            Motorista = motorista.Text?.Trim() ?? "",
            TipoVeiculo = caminhão ? "Caminhão" : "Carro",
            Combustivel = caminhão
                ? "Diesel S10"
                : tipoCombustivel.SelectedItem?.ToString() ?? "",
            KmAnterior = anterior,
            KmAtual = atual,
            Litros = quantidade,
            PrecoLitro = preco,
            Valor = totalCombustivel,
            Consumo = consumo,
            CustoKm = custoKm,
            TemArla = temArla,
            LitrosArla = qtdArla,
            ConsumoArlaKmL = consumoArlaKmL,
            ConsumoArla100 = consumoArla100
        };

        historico.Insert(0, registro);

        SalvarHistorico();
        AtualizarDashboard();
        AtualizarHistorico();

        await DisplayAlert(
            "Sucesso",
            "Abastecimento calculado e salvo!",
            "OK");

        kmAnterior.Text = kmAtual.Text;
        kmAtual.Text = "";
        litros.Text = "";
        precoLitro.Text = "";
        litrosArla.Text = "";

        AtualizarTotalCarro();
    }

    void SalvarHistorico()
    {
        string json = JsonSerializer.Serialize(historico);
        Preferences.Default.Set(Chave, json);
    }

    void CarregarHistorico()
    {
        string json = Preferences.Default.Get(Chave, "");

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            historico =
                JsonSerializer.Deserialize<List<Abastecimento>>(json)
                ?? new List<Abastecimento>();
        }
        catch
        {
            historico = new List<Abastecimento>();
        }
    }

    void AtualizarDashboard()
    {
        lblAbastecimentos.Text = historico.Count.ToString();

        lblGasto.Text =
            historico.Sum(x => x.Valor).ToString("C", pt);

        double distancia = historico.Sum(
            x => x.KmAtual - x.KmAnterior);

        double litrosTotal = historico.Sum(x => x.Litros);

        lblKm.Text = distancia.ToString("N1", pt) + " km";

        lblMedia.Text = litrosTotal > 0
            ? (distancia / litrosTotal).ToString("N2", pt) + " km/l"
            : "-- km/l";
    }

    void AtualizarHistorico()
    {
        lista.Children.Clear();

        if (historico.Count == 0)
        {
            lista.Children.Add(new Label
            {
                Text = "Nenhum abastecimento registrado.",
                TextColor = Colors.Gray
            });
            return;
        }

        foreach (var item in historico)
        {
            var detalhes = new VerticalStackLayout
            {
                Spacing = 5
            };

            detalhes.Children.Add(new Label
            {
                Text = item.Veiculo + " - " +
                       item.Data.ToString("dd/MM/yyyy HH:mm"),
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#123B65")
            });

            detalhes.Children.Add(new Label
            {
                Text = "Tipo: " + item.TipoVeiculo +
                       " | Combustível: " + item.Combustivel,
                TextColor = Colors.Black
            });

            detalhes.Children.Add(new Label
            {
                Text = "Motorista: " + item.Motorista,
                TextColor = Colors.Black
            });

            detalhes.Children.Add(new Label
            {
                Text = $"KM: {item.KmAnterior:N1} → {item.KmAtual:N1}",
                TextColor = Colors.Black
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Combustível: {item.Litros:N2} litros",
                TextColor = Colors.Black
            });

            detalhes.Children.Add(new Label
            {
                Text = $"Consumo: {item.Consumo:N2} km/l",
                TextColor = Colors.Black
            });

            if (item.TipoVeiculo == "Carro")
            {
                detalhes.Children.Add(new Label
                {
                    Text = $"Preço por litro: {item.PrecoLitro:C}",
                    TextColor = Colors.Black
                });

                detalhes.Children.Add(new Label
                {
                    Text = $"Total: {item.Valor:C}",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#166534")
                });
            }

            if (item.TemArla)
            {
                detalhes.Children.Add(new Label
                {
                    Text = $"Arla 32: {item.LitrosArla:N2} litros",
                    TextColor = Colors.Black
                });

                detalhes.Children.Add(new Label
                {
                    Text = $"Consumo Arla: {item.ConsumoArlaKmL:N2} km/l | {item.ConsumoArla100:N2} L/100 km",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#166534")
                });
            }

            lista.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE4ED"),
                Padding = 12,
                Content = detalhes
            });
        }
    }

    async void CompartilharPDF(object? sender, EventArgs e)
    {
        if (historico.Count == 0)
        {
            await DisplayAlert(
                "Atenção",
                "Não existem abastecimentos para gerar o relatório.",
                "OK");
            return;
        }

#if ANDROID
        try
        {
            string caminho = System.IO.Path.Combine(
                FileSystem.CacheDirectory,
                "Relatorio_FuelControl.pdf");

            using (var documento = new AndroidPdfDocument())
            {
                var paginaInfo = new AndroidPdfDocument.PageInfo.Builder(
                    595, 842, 1).Create();

                var pagina = documento.StartPage(paginaInfo);
                var canvas = pagina.Canvas;

                var tinta = new AndroidPaint
                {
                    Color = AndroidColor.Rgb(18, 59, 101),
                    TextSize = 17,
                    FakeBoldText = true,
                    AntiAlias = true
                };

                var texto = new AndroidPaint
                {
                    Color = AndroidColor.Black,
                    TextSize = 10,
                    AntiAlias = true
                };

                int y = 45;

                canvas.DrawText("FUELCONTROL - RELATORIO", 35, y, tinta);
                y += 25;

                canvas.DrawText(
                    "Data: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    35, y, texto);
                y += 25;

                foreach (var item in historico)
                {
                    if (y > 770)
                    {
                        canvas.DrawText(
                            "Demais registros disponíveis no aplicativo.",
                            35, 810, texto);
                        break;
                    }

                    canvas.DrawText(
                        item.Data.ToString("dd/MM/yyyy") +
                        " - " + item.Veiculo,
                        35, y, tinta);
                    y += 18;

                    canvas.DrawText(
                        item.TipoVeiculo + " - " + item.Combustivel,
                        35, y, texto);
                    y += 16;

                    canvas.DrawText(
                        "Motorista: " + item.Motorista,
                        35, y, texto);
                    y += 16;

                    canvas.DrawText(
                        $"KM: {item.KmAnterior:N1} a {item.KmAtual:N1}",
                        35, y, texto);
                    y += 16;

                    canvas.DrawText(
                        $"Combustível: {item.Litros:N2} L | Consumo: {item.Consumo:N2} km/l",
                        35, y, texto);
                    y += 16;

                    if (item.TipoVeiculo == "Carro")
                    {
                        canvas.DrawText(
                            $"Preço/L: {item.PrecoLitro:C} | Total: {item.Valor:C}",
                            35, y, texto);
                        y += 16;
                    }

                    if (item.TemArla)
                    {
                        canvas.DrawText(
                            $"Arla: {item.LitrosArla:N2} L",
                            35, y, texto);
                        y += 16;

                        canvas.DrawText(
                            $"Consumo Arla: {item.ConsumoArlaKmL:N2} km/l | {item.ConsumoArla100:N2} L/100 km",
                            35, y, texto);
                        y += 16;
                    }

                    y += 15;
                }

                documento.FinishPage(pagina);

                using var arquivo = System.IO.File.Create(caminho);
                documento.WriteTo(arquivo);
            }

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Relatório FuelControl",
                    File = new ShareFile(caminho)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro no PDF",
                ex.Message,
                "OK");
        }
#else
        await DisplayAlert(
            "Indisponível",
            "A geração de PDF está configurada para Android.",
            "OK");
#endif
    }
}

public class Abastecimento
{
    public DateTime Data { get; set; }

    public string Veiculo { get; set; } = "";
    public string Motorista { get; set; } = "";

    public string TipoVeiculo { get; set; } = "Carro";
    public string Combustivel { get; set; } = "";

    public double KmAnterior { get; set; }
    public double KmAtual { get; set; }

    public double Litros { get; set; }
    public double PrecoLitro { get; set; }
    public double Valor { get; set; }

    public double Consumo { get; set; }
    public double CustoKm { get; set; }

    public bool TemArla { get; set; }
    public double LitrosArla { get; set; }

    public double ConsumoArlaKmL { get; set; }
    public double ConsumoArla100 { get; set; }
}
