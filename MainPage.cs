
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Globalization;
using System.Text;
using System.Text.Json;

#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
#endif

namespace FuelControl;

public class MainPage : ContentPage
{
    const string Chave = "fuelcontrol_historico";

    List<Abastecimento> historico = new();

    Label lblAbastecimentos = new();
    Label lblGasto = new();
    Label lblKm = new();
    Label lblMedia = new();

    Label lblResultado = new();
    Label lblCusto = new();

    VerticalStackLayout lista = new();

    Entry veiculo = new();
    Entry motorista = new();
    Entry kmAnterior = new();
    Entry kmAtual = new();
    Entry litros = new();
    Entry valor = new();

    readonly CultureInfo pt =
        CultureInfo.GetCultureInfo("pt-BR");

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

    View Cartao(string titulo, Label valor)
    {
        return new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#DCE4ED"),
            CornerRadius = 12,
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
                    valor
                }
            }
        };
    }

    View CriarTela()
    {
        veiculo = Campo("Identificação do veículo", Keyboard.Default);
        motorista = Campo("Nome do motorista", Keyboard.Default);

        kmAnterior = Campo("Quilometragem anterior", Keyboard.Numeric);
        kmAtual = Campo("Quilometragem atual", Keyboard.Numeric);
        litros = Campo("Litros abastecidos", Keyboard.Numeric);
        valor = Campo("Valor total (R$)", Keyboard.Numeric);

        lblAbastecimentos = Indicador("0");
        lblGasto = Indicador("R$ 0,00");
        lblKm = Indicador("0 km");
        lblMedia = Indicador("-- km/l");

        lblResultado = new Label
        {
            Text = "Consumo médio: -- km/l",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#166534")
        };

        lblCusto = new Label
        {
            Text = "Custo por km: --",
            FontSize = 18,
            TextColor = Color.FromArgb("#123B65")
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

                    new Grid
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
                        RowSpacing = 10,
                        Children =
                        {
                            { Cartao("Abastecimentos", lblAbastecimentos), 0, 0 },
                            { Cartao("Gasto total", lblGasto), 1, 0 },
                            { Cartao("KM percorridos", lblKm), 0, 1 },
                            { Cartao("Consumo médio", lblMedia), 1, 1 }
                        }
                    },

                    Titulo("NOVO ABASTECIMENTO"),

                    new Label
                    {
                        Text = "Identificação",
                        TextColor = Color.FromArgb("#334155")
                    },
                    veiculo,

                    new Label
                    {
                        Text = "Motorista",
                        TextColor = Color.FromArgb("#334155")
                    },
                    motorista,

                    new Label
                    {
                        Text = "Quilometragem anterior",
                        TextColor = Color.FromArgb("#334155")
                    },
                    kmAnterior,

                    new Label
                    {
                        Text = "Quilometragem atual",
                        TextColor = Color.FromArgb("#334155")
                    },
                    kmAtual,

                    new Label
                    {
                        Text = "Litros abastecidos",
                        TextColor = Color.FromArgb("#334155")
                    },
                    litros,

                    new Label
                    {
                        Text = "Valor total",
                        TextColor = Color.FromArgb("#334155")
                    },
                    valor,

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

    bool Ler(string? texto, out double numero)
    {
        return double.TryParse(
            texto?.Trim(),
            NumberStyles.Number,
            pt,
            out numero);
    }

    async void Calcular(object? sender, EventArgs e)
    {
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

        double total = 0;

        if (!string.IsNullOrWhiteSpace(valor.Text) &&
            (!Ler(valor.Text, out total) || total <= 0))
        {
            await DisplayAlert(
                "Atenção",
                "Informe um valor total válido.",
                "OK");
            return;
        }

        double distancia = atual - anterior;
        double consumo = distancia / quantidade;
        double custoKm = total > 0 ? total / distancia : 0;

        lblResultado.Text =
            $"Consumo médio: {consumo:F2} km/l";

        lblCusto.Text = total > 0
            ? $"Custo por km: R$ {custoKm:F2}"
            : "Custo por km: não informado";

        var registro = new Abastecimento
        {
            Data = DateTime.Now,
            Veiculo = veiculo.Text.Trim(),
            Motorista = motorista.Text?.Trim() ?? "",
            KmAnterior = anterior,
            KmAtual = atual,
            Litros = quantidade,
            Valor = total,
            Consumo = consumo,
            CustoKm = custoKm
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
        valor.Text = "";
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
            historico = JsonSerializer.Deserialize<List<Abastecimento>>(json)
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
            lista.Children.Add(new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#DCE4ED"),
                CornerRadius = 10,
                Padding = 12,
                Content = new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Label
                        {
                            Text = item.Veiculo + " - " +
                                   item.Data.ToString("dd/MM/yyyy HH:mm"),
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#123B65")
                        },

                        new Label
                        {
                            Text = "Motorista: " + item.Motorista,
                            TextColor = Colors.Black
                        },

                        new Label
                        {
                            Text =
                                $"KM: {item.KmAnterior:N1} → {item.KmAtual:N1}",
                            TextColor = Colors.Black
                        },

                        new Label
                        {
                            Text =
                                $"Litros: {item.Litros:N2} | " +
                                $"Consumo: {item.Consumo:N2} km/l",
                            TextColor = Colors.Black
                        },

                        new Label
                        {
                            Text = $"Total: {item.Valor:C}",
                            TextColor = Color.FromArgb("#166534")
                        }
                    }
                }
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
            string caminho = Path.Combine(
                FileSystem.CacheDirectory,
                "Relatorio_FuelControl.pdf");

            using (var documento = new PdfDocument())
            {
                var paginaInfo = new PdfDocument.PageInfo.Builder(
                    595, 842, 1).Create();

                var pagina = documento.StartPage(paginaInfo);
                var canvas = pagina.Canvas;

                var tinta = new Paint
                {
                    Color = Color.Rgb(18, 59, 101),
                    TextSize = 20,
                    FakeBoldText = true,
                    AntiAlias = true
                };

                var texto = new Paint
                {
                    Color = Color.Black,
                    TextSize = 11,
                    AntiAlias = true
                };

                int y = 55;

                canvas.DrawText(
                    "FUELCONTROL - RELATORIO",
                    35, y, tinta);

                y += 35;

                canvas.DrawText(
                    "Data: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    35, y, texto);

                y += 30;

                canvas.DrawText(
                    "Total de abastecimentos: " + historico.Count,
                    35, y, texto);

                y += 22;

                canvas.DrawText(
                    "Gasto total: " +
                    historico.Sum(x => x.Valor).ToString("C", pt),
                    35, y, texto);

                y += 22;

                double km = historico.Sum(
                    x => x.KmAtual - x.KmAnterior);

                double litrosTotal = historico.Sum(x => x.Litros);

                canvas.DrawText(
                    "KM percorridos: " + km.ToString("N1", pt),
                    35, y, texto);

                y += 22;

                canvas.DrawText(
                    "Consumo medio: " +
                    (litrosTotal > 0
                        ? (km / litrosTotal).ToString("N2", pt) + " km/l"
                        : "--"),
                    35, y, texto);

                y += 35;

                tinta.TextSize = 13;

                canvas.DrawText(
                    "HISTORICO DE ABASTECIMENTOS",
                    35, y, tinta);

                y += 25;

                tinta.TextSize = 10;

                foreach (var item in historico)
                {
                    if (y > 790)
                    {
                        texto.TextSize = 9;
                        canvas.DrawText(
                            "Consulte o aplicativo para os demais registros.",
                            35, 810, texto);
                        break;
                    }

                    canvas.DrawText(
                        item.Data.ToString("dd/MM/yyyy") +
                        " - Veiculo: " + item.Veiculo,
                        35, y, tinta);

                    y += 17;

                    canvas.DrawText(
                        "Motorista: " + item.Motorista,
                        35, y, texto);

                    y += 16;

                    canvas.DrawText(
                        $"KM: {item.KmAnterior:N1} a {item.KmAtual:N1}",
                        35, y, texto);

                    y += 16;

                    canvas.DrawText(
                        $"Litros: {item.Litros:N2} | " +
                        $"Consumo: {item.Consumo:N2} km/l | " +
                        $"Total: {item.Valor:C}",
                        35, y, texto);

                    y += 25;
                }

                documento.FinishPage(pagina);

                using var arquivo = File.Create(caminho);
                documento.WriteTo(arquivo);
            }

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Compartilhar relatório FuelControl",
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

    public double KmAnterior { get; set; }
    public double KmAtual { get; set; }
    public double Litros { get; set; }
    public double Valor { get; set; }
    public double Consumo { get; set; }
    public double CustoKm { get; set; }
}


