using System;
using System.Data;
using System.Windows.Forms;
using CapaControlador_AprobacionFinanciera; // Referencia a tu nueva capa controlador

/*
 * ==================================================================
 * Área : Compras
 * Autor : Pedro José Gómez Villalobos
 * Carné : 0901-23-4868
 * Fecha : 24/10/2026
 * ==================================================================
 * Propósito :
 *  Presenta la consulta de órdenes para su revisión en el proceso de aprobación financiera.
 * ===================================================================
*/

namespace CapaVista_AprobacionFinanciera
{
    public partial class FrmAprobacionFinanciera : System.Windows.Forms.Form
    {
        // 1. Instancia del controlador
        private ClsAprobacionFinanciera _Controlador = new ClsAprobacionFinanciera();

        public FrmAprobacionFinanciera()
        {
            InitializeComponent();

            // Configuraciones visuales originales que ya tenías
            ComprasBtnVer.Enabled = false;
            ComprasBtnEvaluar.Enabled = false;
            ComprasBtnRegresar.Click += (Remitente, Evento) => Close();
            CapaVista_Componentes.Componentes.ClsPresentacionCompras.ComprasMetFijarTamanoFormulario(this);
        }

        private void FrmAprobacionFinanciera_Load(object sender, System.EventArgs e)
        {
            try
            {
                // 2. Pedimos los datos al controlador
                DataTable Datos = _Controlador.ComprasFuncListarPendientes();

                // 3. Forzamos a que el grid dibuje las columnas automáticamente
                ComprasDgvOrdenesDisponibles.AutoGenerateColumns = true;

                // 4. Llenamos el DataGridView
                ComprasDgvOrdenesDisponibles.DataSource = Datos;

                // Asegúrate de cambiar "NombreDeTuDataGridView" por el nombre real de tu tabla en el diseño
                ComprasDgvOrdenesDisponibles.Columns["Total Esperado"].DefaultCellStyle.Format = "N2";

                // Opcional: Si hay datos, habilitamos el botón de evaluar
                if (Datos.Rows.Count > 0)
                {
                    ComprasBtnEvaluar.Enabled = true;
                }
            }
            catch (Exception Ex)
            {
                // Manejo de errores según el estándar EST-10
                MessageBox.Show(Ex.Message, "Ocurrió un error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComprasBtnEvaluar_Click(object sender, EventArgs e)
        {
            // 1. Validar que el usuario realmente haya seleccionado una fila en el DataGridView
            if (ComprasDgvOrdenesDisponibles.SelectedRows.Count == 0)
            {
                // Diálogo ámbar del estándar EST-10
                MessageBox.Show("Por favor, seleccione una orden de la tabla para evaluarla.", "Confirmar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 2. Extraer los datos de la fila seleccionada.
                // NOTA: Usamos los nombres de columna (Alias) exactos que definimos en la consulta SQL de la Capa Modelo ("ID Orden" y "Total Esperado")
                int IdOrden = Convert.ToInt32(ComprasDgvOrdenesDisponibles.SelectedRows[0].Cells["ID Orden"].Value);
                decimal MontoEsperado = Convert.ToDecimal(ComprasDgvOrdenesDisponibles.SelectedRows[0].Cells["Total Esperado"].Value);

                // 3. Instanciar y abrir FrmCompra pasándole los datos de la orden
                FrmCompra frmEvaluacion = new FrmCompra(IdOrden, MontoEsperado);
                frmEvaluacion.ShowDialog(); // ShowDialog bloquea la ventana de atrás hasta que terminen de evaluar

                // 4. Refrescar la tabla al cerrar FrmCompra (así la orden desaparece de la bandeja si se guardó como 'Cuadrada')
                // Volvemos a llamar la lógica de carga que tienes en tu evento Load
                DataTable DatosActualizados = _Controlador.ComprasFuncListarPendientes();
                ComprasDgvOrdenesDisponibles.DataSource = DatosActualizados;
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Ocurrió un error al intentar abrir la evaluación: " + Ex.Message, "Ocurrió un error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}