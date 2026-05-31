using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFHotel.DAO;
using WPFHotel.Modelos;
using static WPFHotel.DAO.ConexionBD;

namespace WPFHotel
{
    /// <summary>
    /// Lógica de interacción para Inicio.xaml
    /// </summary>
    public partial class Inicio : Window
    {
        public ObservableCollection<Habitaciones> HabitacionesTipoA { get; set; }
        public ObservableCollection<Habitaciones> HabitacionesTipoB { get; set; }

        private string _rol;

        private System.Windows.Threading.DispatcherTimer _timer;

        private readonly HabitacionDAO _habitacionDAO = new HabitacionDAO();
        public Inicio(string rol)
        {
            InitializeComponent();
            _rol = rol;


            HabitacionesTipoA = new ObservableCollection<Habitaciones>();
            HabitacionesTipoB = new ObservableCollection<Habitaciones>();

            ListaTipoA.ItemsSource = HabitacionesTipoA;
            ListaTipoB.ItemsSource = HabitacionesTipoB;

            if (_rol == "admin")
            {
                BtnAgregarHabitacion.Visibility = Visibility.Visible;
                BtnCambiarContraseña.Visibility = Visibility.Visible;
                BtnregistrarUsuario.Visibility = Visibility.Visible;
            }

            LiberarHabitacionesVencidas();
            CargarHabitaciones();
            IniciarTimer();
        }


        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string habitacion = btn?.Tag?.ToString();
            if (string.IsNullOrEmpty(habitacion)) return;

            var confirm = MessageBox.Show(
                $"¿Confirmar checkout de la habitación {habitacion}?\n\n" +
                $"El huésped será movido al historial y la habitación quedará disponible.",
                "Confirmar checkout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var dao = new HuespedDAO();

                // Mover a historial y borrar de reservaciones
                dao.CerrarReservacion(habitacion);

                // Liberar habitación en BD
                if (int.TryParse(habitacion, out int numHab))
                    _habitacionDAO.ActualizarDisponibilidad(numHab, true);

                MessageBox.Show("Checkout realizado correctamente.",
                                "Éxito", MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // Refrescar UI
                CargarHabitaciones();
                CargarReservaciones();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar checkout: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void TileHabitacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var room = btn?.DataContext as Habitaciones;
            if (room == null) return;

            if (!room.Disponible)
            {
                MessageBox.Show($"La habitación {room.NumeroHabitacion} no está disponible.",
                                "No disponible", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }
            string tipo = room.Tipo == "A" ? "Estándar" : "Suite";
            var dialog = new RegistrarHuespedDialog(
                                room.NumeroHabitacion.ToString(), tipo, room.CostoBase);

            if (dialog.ShowDialog() == true)
            {
                // Marcar como ocupada en BD
                _habitacionDAO.ActualizarDisponibilidad(room.NumeroHabitacion, false);
                CargarHabitaciones();
            }
        }

        private void BtnAgregarHabitacion_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new AgregarHabitacionDialog();
            if (dialog.ShowDialog() != true) return;

            // Verificar que no exista ya
            if (_habitacionDAO.Existe(dialog.Numero))
            {
                MessageBox.Show($"La habitación {dialog.Numero} ya existe.",
                                "Duplicado", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var nueva = new Habitaciones
            {
                NumeroHabitacion = dialog.Numero,
                Tipo = dialog.Tipo,
                CostoBase = dialog.CostoBase,
                Disponible = true
            };

            // Guardar en BD
            if (_habitacionDAO.Insertar(nueva))
                CargarHabitaciones(); // recargar para reflejar cambios
            else
                MessageBox.Show("Error al guardar la habitación.",
                                "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
        }

        // ── Carga todas las habitaciones desde BD ──
        private void CargarHabitaciones()
        {
            HabitacionesTipoA.Clear();
            HabitacionesTipoB.Clear();

            var todas = _habitacionDAO.ObtenerTodas();

            foreach (var hab in todas)
            {
                if (hab.Tipo == "A")
                {
                    HabitacionesTipoA.Add(hab);
                    PanelTipoA.Visibility = Visibility.Visible;
                }
                else
                {
                    HabitacionesTipoB.Add(hab);
                    PanelTipoB.Visibility = Visibility.Visible;
                }
            }

            PanelVacio.Visibility = todas.Count == 0
                                    ? Visibility.Visible
                                    : Visibility.Collapsed;
            CargarReservacionesActivas();
        }

        private void BtnregistrarUsuario_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new RegistrarUsuarioDialog();
            dialog.ShowDialog();
        }


        // ── Libera habitaciones cuya fecha_salida ya pasó ──
        private void LiberarHabitacionesVencidas()
        {
            try
            {
                var dao = new HuespedDAO();
                var vencidas = dao.ObtenerHabitacionesVencidas();

                foreach (var numeroStr in vencidas)
                {
                    dao.CerrarReservacion(numeroStr); // cerrar reservación en BD
                    if (int.TryParse(numeroStr, out int numero))
                        _habitacionDAO.ActualizarDisponibilidad(numero, true);
                }

                if (vencidas.Count == 0) CargarHabitaciones(); // recargar solo si hubo cambios



            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al liberar habitaciones: {ex.Message}",
                                "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ── Timer: revisa cada minuto si hay nuevas vencidas ──
        private void IniciarTimer()
        {
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += (s, e) =>
            {
                LiberarHabitacionesVencidas();
                CargarHabitaciones();
            };
            _timer.Start();
        }

        // Detener timer al cerrar ventana
        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            base.OnClosed(e);
        }

        private void BtnGestion_Click(object sender, RoutedEventArgs e)
        {
            var gestion = new Gestion(_rol);
            gestion.Show();
            this.Close();
        }

        /* private void BtnCambiarContraseña_Click(object sender, RoutedEventArgs e)
         {
             var dialog = new CambiarContraseñaDialog();
             dialog.ShowDialog();
         }
 */
        private void CargarReservacionesActivas()
        {
            try
            {
                var dao = new HuespedDAO();
                var lista = new List<ReservacionItem>();

                using (var conn = new MySqlConnection(ConexionDB.Cadena))
                {
                    conn.Open();
                    string query = @"SELECT nombre, apellidos, habitacion, 
                                    tipo_habitacion, estatus,
                                    fecha_salida, costo
                             FROM reservaciones
                             WHERE fecha_salida > NOW()
                             ORDER BY fecha_salida ASC";

                    var cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        lista.Add(new ReservacionItem
                        {
                            Habitacion = reader.GetString("habitacion"),
                            NombreCompleto = $"{reader.GetString("nombre")} {reader.GetString("apellidos")}",
                            TipoHabitacion = reader.IsDBNull(reader.GetOrdinal("tipo_habitacion"))
                                             ? "—" : reader.GetString("tipo_habitacion"),
                            Estatus = reader.IsDBNull(reader.GetOrdinal("estatus"))
                                             ? "Normal" : reader.GetString("estatus"),
                            FechaSalidaStr = reader.IsDBNull(reader.GetOrdinal("fecha_salida"))
                                             ? "—" : reader.GetDateTime("fecha_salida").ToString("dd/MM HH:mm"),
                            CostoStr = $"${reader.GetDecimal("costo"):F2}"
                        });
                    }
                }

                ListaReservaciones.ItemsSource = lista;
                PanelSinReservaciones.Visibility = lista.Count == 0
                                                       ? Visibility.Visible
                                                       : Visibility.Collapsed;
                TxtTotalReservaciones.Text = $"{lista.Count} activa{(lista.Count != 1 ? "s" : "")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reservaciones: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CargarReservaciones()
        {
            try
            {
                var dao = new HuespedDAO();
                var lista = dao.ObtenerTodos();

                var items = lista.Select(h => new ReservacionItem
                {
                    Habitacion = h.Habitacion,
                    NombreCompleto = $"{h.Nombre} {h.Apellidos}",
                    TipoHabitacion = h.Vip ? "Suite" : "Estándar",
                    FechaSalidaStr = h.FechaSalida.ToString("dd/MM/yyyy HH:mm"),
                    CostoStr = $"${h.Costo:F2}",
                    Estatus = h.Vip ? "Vip" : "Normal"
                }).ToList();

                ListaReservaciones.ItemsSource = items;
                TxtTotalReservaciones.Text = $"{items.Count} activas";
                PanelSinReservaciones.Visibility = items.Count == 0
                                                    ? Visibility.Visible
                                                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando reservaciones: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // En el tab de Reportes de Inicio.xaml
        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            new Reportes(_rol).Show();
            this.Close();
        }

        private void BtnAcercaDE_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AcercadeDiaglog();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void BtnCambiarContraseña_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CambiarContraseñaDialog(); 
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}
