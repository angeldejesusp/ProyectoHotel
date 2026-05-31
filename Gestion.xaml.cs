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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFHotel.DAO;

namespace WPFHotel
{
    public partial class Gestion : Window
    {
        private string _rol;

        public Gestion(string rol)
        {
            InitializeComponent();
            _rol = rol;

            // Carga el panel activo por defecto
            MostrarPanel("Historial");
        }

        // ══ Navegación tabs ══
        private void MostrarPanel(string panel)
        {
            PanelReservaciones.Visibility = Visibility.Collapsed;
            PanelHistorial.Visibility = Visibility.Collapsed;
            PanelHabitaciones.Visibility = Visibility.Collapsed;

           
            TabHistorial.Style = (Style)FindResource("TabBtn");
            TabHabitaciones.Style = (Style)FindResource("TabBtn");

            switch (panel)
            {
                case "Historial":
                    PanelHistorial.Visibility = Visibility.Visible;
                    TabHistorial.Style = (Style)FindResource("TabBtnActive");
                    CargarHistorial();
                    break;
                case "Habitaciones":
                    PanelHabitaciones.Visibility = Visibility.Visible;
                    TabHabitaciones.Style = (Style)FindResource("TabBtnActive");
                    CargarHabitaciones();
                    break;
            }
        }

       

        private void TabHistorial_Click(object sender, RoutedEventArgs e)
            => MostrarPanel("Historial");

        private void TabHabitaciones_Click(object sender, RoutedEventArgs e)
            => MostrarPanel("Habitaciones");

        // ══ Carga reservaciones activas ══
        private void CargarReservacionesActivas()
        {
            try
            {
                var dao = new HuespedDAO();
                var lista = dao.ObtenerTodos(); // tabla reservaciones

                var items = lista.Select(h => new
                {
                    Habitacion = h.Habitacion,
                    NombreCompleto = $"{h.Nombre} {h.Apellidos}",
                    TipoHabitacion = h.Vip ? "Suite" : "Estándar",
                    FechaSalidaStr = h.FechaSalida.ToString("dd/MM/yyyy HH:mm"),
                    CostoStr = $"${h.Costo:F2}",
                    Estatus = h.Vip ? "Vip" : "Normal"
                }).ToList();

                // x:Name en GestionWindow.xaml — DataGrid o ItemsControl
                ListaReservacionesGestion.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══ Carga historial desde tabla huespedes ══
        private void CargarHistorial()
        {
            try
            {
                var dao = new HuespedDAO();
                var lista = dao.ObtenerHistorial(); // método nuevo abajo

                var items = lista.Select(h => new
                {
                    Id = h.ID,
                    NombreCompleto = $"{h.Nombre} {h.Apellidos}",
                    Habitacion = h.Habitacion,
                    Estatus = h.Vip ? "Vip" : "Normal",
                    FechaSalidaStr = h.FechaSalida.ToString("dd/MM/yyyy"),
                    CostoStr = $"${h.Costo:F2}",
                    Tipo = h.Vip ? "Suite" : "Estándar"
                }).ToList();

                ListaHistorial.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando historial: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══ Carga habitaciones ══
        private void CargarHabitaciones()
        {
            try
            {
                var dao = new HabitacionDAO();
                var lista = dao.ObtenerTodas();

                var items = lista.Select(h => new
                {
                    Numero = h.NumeroHabitacion,
                    Tipo = h.Tipo == "A" ? "Estándar" : "Suite",
                    Disponible = h.Disponible ? "✔ Disponible" : "✘ Ocupada"
                }).ToList();

                ListaHabitacionesGestion.ItemsSource = items;
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

        private void BtnCheckoutGestion_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string habitacion = btn?.Tag?.ToString();
            if (string.IsNullOrEmpty(habitacion)) return;

            var confirm = MessageBox.Show(
                $"¿Confirmar checkout habitación {habitacion}?",
                "Checkout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var dao = new HuespedDAO();
            var habD = new HabitacionDAO();
            dao.CerrarReservacion(habitacion);
            if (int.TryParse(habitacion, out int num))
                habD.ActualizarDisponibilidad(num, true);

            CargarReservacionesActivas();
        }

    }

}

