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
using WPFHotel.DAO;
using WPFHotel.Modelos;
using MySql.Data.MySqlClient;


namespace WPFHotel
{
    /// <summary>
    /// Lógica de interacción para RegistrarHuespedDialog.xaml
    /// </summary>
    public partial class RegistrarHuespedDialog : Window
    {
        private string _habitacionId;
        private string _tipoHabitacion; // "Suite" o "Estándar"
        private double _costoBase;
        public RegistrarHuespedDialog(string habitacionId, string tipoHabitacion, double costobase)
        {
            InitializeComponent();
            _habitacionId = habitacionId;
            _tipoHabitacion = tipoHabitacion;
            _costoBase = costobase;

            TxtHabitacionInfo.Text = $"Habitación: {habitacionId}";
            TxtTipoHabitacion.Text = tipoHabitacion;
            TxtCostoInfo.Text = $"${costobase:F2} / noche";
            // Configurar ComboBox de estatus
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            double costoPorNoche = _costoBase;
            // ── Validaciones ──
            if (string.IsNullOrWhiteSpace(TxtNombre.Text) ||
                string.IsNullOrWhiteSpace(TxtApellidos.Text) ||
                string.IsNullOrWhiteSpace(TxtNoches.Text) ||
                string.IsNullOrWhiteSpace(TxtCostoInfo.Text))
            {
                MessageBox.Show("Por favor completa todos los campos.",
                                "Campos vacíos", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtNoches.Text, out int noches) || noches <= 0)
            {
                MessageBox.Show("El número de noches debe ser mayor a 0.",
                                "Dato incorrecto", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (costoPorNoche <= 0)
            {
                MessageBox.Show("El costo debe ser un número válido mayor a 0.",
                                "Dato incorrecto", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Estatus lo elige el usuario (Vip o Normal)
            string estatus = ((ComboBoxItem)CbbEstatus.SelectedItem).Content.ToString();

            // Instanciar según estatus — el descuento Vip aplica sin importar la habitación
            AbsHuespedes huesped;


            if (estatus == "Vip")
            {
                huesped = new HuespedVip(0,
                    TxtNombre.Text.Trim(),
                    TxtApellidos.Text.Trim(),
                    noches, _habitacionId, costoPorNoche);
            }
            else
            {
                huesped = new Huesped(0,
                    TxtNombre.Text.Trim(),
                    TxtApellidos.Text.Trim(),
                    noches, _habitacionId, costoPorNoche);
            }

            

            double costoTotal = huesped.CalcularCostoEstancia();

            string descuentoInfo = estatus == "Vip"
                                   ? " (incluye 20% descuento Vip)" : "";

            var confirmacion = MessageBox.Show(
                $"Resumen:\n\n" +
                $"Nombre:           {huesped.Nombre} {huesped.Apellidos}\n" +
                $"Habitación:       {huesped.Habitacion}\n" +
                $"Tipo habitación:  {_tipoHabitacion}\n" +
                $"Estatus:          {estatus}\n" +
                $"Noches:           {huesped.Tiempo}\n" +
                $"Costo por noche:  ${costoPorNoche:F2}\n" +
                $"Costo total:      ${costoTotal:F2}{descuentoInfo}\n" +
                $"Fecha salida:     {huesped.FechaSalida:dd/MM/yyyy HH:mm}\n\n" +
                $"¿Confirmar?",
                "Confirmar registro",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes) return;

            try
            {
                var dao = new HuespedDAO();
                dao.Insertar(huesped, _tipoHabitacion); // pasa tipo habitación al DAO
                MessageBox.Show($"Reservación registrada.\nCosto total: ${costoTotal:F2}",
                                "Éxito", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}",
                                "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }



        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


