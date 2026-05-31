using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MySql.Data.MySqlClient;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static WPFHotel.DAO.ConexionBD;

namespace WPFHotel
{
    public partial class Reportes : Window
    {
        private string _rol;
        private string _periodoActual = "Diario";

        public Reportes(string rol)
        {
            InitializeComponent();
            _rol = rol;
            CargarIngresos("Diario");
        }

        // ══ Tabs ══
        private void TabIngresos_Click(object sender, RoutedEventArgs e)
        {
            PanelIngresos.Visibility = Visibility.Visible;
            PanelHabitaciones.Visibility = Visibility.Collapsed;
            TabIngresos.Style = (Style)FindResource("TabBtnActive");
            TabHabitaciones.Style = (Style)FindResource("TabBtn");
            CargarIngresos(_periodoActual);
        }

        private void TabHabitaciones_Click(object sender, RoutedEventArgs e)
        {
            PanelIngresos.Visibility = Visibility.Collapsed;
            PanelHabitaciones.Visibility = Visibility.Visible;
            TabIngresos.Style = (Style)FindResource("TabBtn");
            TabHabitaciones.Style = (Style)FindResource("TabBtnActive");
            CargarHabitacionesMasOcupadas();
        }

        // ══ Período ══
        private void BtnPeriodo_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            _periodoActual = btn?.Tag?.ToString() ?? "Diario";

            BtnDiario.Style = (Style)FindResource("BtnFiltro");
            BtnSemanal.Style = (Style)FindResource("BtnFiltro");
            BtnMensual.Style = (Style)FindResource("BtnFiltro");
            btn.Style = (Style)FindResource("BtnFiltroActivo");

            CargarIngresos(_periodoActual);
        }

        // ══════════════════════════════════════════
        //  REPORTE 1 — Ingresos
        // ══════════════════════════════════════════
        private void CargarIngresos(string periodo)
        {
            try
            {
                DateTime desde = periodo switch
                {
                    "Diario" => DateTime.Today,
                    "Semanal" => DateTime.Today.AddDays(-7),
                    "Mensual" => DateTime.Today.AddMonths(-1),
                    _ => DateTime.Today
                };

                string label = periodo switch
                {
                    "Diario" => "Hoy",
                    "Semanal" => "Últimos 7 días",
                    "Mensual" => "Último mes",
                    _ => ""
                };

                TxtPeriodoLabel.Text = label;
                TxtReservasPeriodo.Text = label;

                // ── Datos por tipo de habitación ──
                var porTipo = new Dictionary<string, double>();
                var porEstatus = new Dictionary<string, double>();
                int totalRes = 0;
                double totalI = 0;

                using (var conn = new MySqlConnection(ConexionDB.Cadena))
                {
                    conn.Open();

                    // Por tipo
                    string q1 = @"SELECT IFNULL(tipo_habitacion,'Sin tipo') AS tipo,
                                         SUM(costo) AS total
                                  FROM huespedes
                                  WHERE fecha_salida >= @desde
                                  GROUP BY tipo_habitacion";
                    var cmd1 = new MySqlCommand(q1, conn);
                    cmd1.Parameters.AddWithValue("@desde", desde);
                    var r1 = cmd1.ExecuteReader();
                    while (r1.Read())
                        porTipo[r1.GetString("tipo")] = (double)r1.GetDecimal("total");
                    r1.Close();

                    // Por estatus
                    string q2 = @"SELECT IFNULL(estatus,'Normal') AS estatus,
                                         SUM(costo) AS total
                                  FROM huespedes
                                  WHERE fecha_salida >= @desde
                                  GROUP BY estatus";
                    var cmd2 = new MySqlCommand(q2, conn);
                    cmd2.Parameters.AddWithValue("@desde", desde);
                    var r2 = cmd2.ExecuteReader();
                    while (r2.Read())
                        porEstatus[r2.GetString("estatus")] = (double)r2.GetDecimal("total");
                    r2.Close();

                    // Totales
                    string q3 = @"SELECT COUNT(*) AS cnt, IFNULL(SUM(costo),0) AS total
                                  FROM huespedes WHERE fecha_salida >= @desde";
                    var cmd3 = new MySqlCommand(q3, conn);
                    cmd3.Parameters.AddWithValue("@desde", desde);
                    var r3 = cmd3.ExecuteReader();
                    if (r3.Read())
                    {
                        totalRes = r3.GetInt32("cnt");
                        totalI = (double)r3.GetDecimal("total");
                    }
                }

                TxtTotalIngresos.Text = $"${totalI:F2}";
                TxtTotalReservas.Text = totalRes.ToString();
                TxtPromedio.Text = totalRes > 0
                                        ? $"${totalI / totalRes:F2}"
                                        : "$0.00";

                // ── Gráfica por tipo ──
                var coloresTipo = new[] {
                    SKColor.Parse("#2563A8"),
                    SKColor.Parse("#4ADE80"),
                    SKColor.Parse("#FBBF24"),
                    SKColor.Parse("#F87171")
                };

                var seriesTipo = porTipo.Select((kv, i) =>
                    new PieSeries<double>
                    {
                        Values = new[] { kv.Value },
                        Name = kv.Key,
                        Fill = new SolidColorPaint(coloresTipo[i % coloresTipo.Length]),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 12,
                        DataLabelsFormatter = p =>
                            $"{kv.Key}\n${kv.Value:F0}"
                    }).Cast<ISeries>().ToArray();

                GraficaIngresos.Series = seriesTipo.Length > 0
                    ? seriesTipo
                    : new ISeries[] { new PieSeries<double>
                      {
                          Values = new[] { 1.0 },
                          Name   = "Sin datos",
                          Fill   = new SolidColorPaint(SKColor.Parse("#3B3B50"))
                      }};

                // ── Gráfica por estatus ──
                var coloresEst = new[] {
                    SKColor.Parse("#60A5FA"),
                    SKColor.Parse("#A78BFA")
                };

                var seriesEst = porEstatus.Select((kv, i) =>
                    new PieSeries<double>
                    {
                        Values = new[] { kv.Value },
                        Name = kv.Key,
                        Fill = new SolidColorPaint(coloresEst[i % coloresEst.Length]),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 12,
                        DataLabelsFormatter = p =>
                            $"{kv.Key}\n${kv.Value:F0}"
                    }).Cast<ISeries>().ToArray();

                GraficaEstatusIngresos.Series = seriesEst.Length > 0
                    ? seriesEst
                    : new ISeries[] { new PieSeries<double>
                      {
                          Values = new[] { 1.0 },
                          Name   = "Sin datos",
                          Fill   = new SolidColorPaint(SKColor.Parse("#3B3B50"))
                      }};
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando ingresos: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════
        //  REPORTE 2 — Habitaciones más ocupadas
        // ══════════════════════════════════════════
        private void CargarHabitacionesMasOcupadas()
        {
            try
            {
                var datos = new List<(string Hab, int Veces, double Ingresos)>();

                using (var conn = new MySqlConnection(ConexionDB.Cadena))
                {
                    conn.Open();
                    string query = @"SELECT IFNULL(habitacion,'?') AS hab,
                                            COUNT(*)               AS veces,
                                            SUM(costo)             AS ingresos
                                     FROM huespedes
                                     GROUP BY habitacion
                                     ORDER BY veces DESC
                                     LIMIT 8";
                    var cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        datos.Add((
                            reader.GetString("hab"),
                            reader.GetInt32("veces"),
                            (double)reader.GetDecimal("ingresos")));
                }

                if (datos.Count == 0) return;

                var top = datos.First();
                TxtHabMasOcupada.Text = $"Hab. {top.Hab}";
                TxtHabVeces.Text = $"{top.Veces} veces ocupada";
                TxtTotalOcupaciones.Text = datos.Sum(d => d.Veces).ToString();
                TxtIngresosTopHab.Text = $"${top.Ingresos:F2}";

                // Paleta
                var colores = new[]
                {
                    "#2563A8","#4ADE80","#FBBF24","#F87171",
                    "#A78BFA","#FB923C","#22D3EE","#F472B6"
                };

                var series = datos.Select((d, i) =>
                    new PieSeries<double>
                    {
                        Values = new[] { (double)d.Veces },
                        Name = $"Hab. {d.Hab}",
                        Fill = new SolidColorPaint(SKColor.Parse(colores[i % colores.Length])),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 11,
                        DataLabelsFormatter = p =>
                            $"Hab.{d.Hab}\n{d.Veces}x"
                    }).Cast<ISeries>().ToArray();

                GraficaHabitaciones.Series = series;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            new Inicio(_rol).Show();
            this.Close();
        }
    }
}

