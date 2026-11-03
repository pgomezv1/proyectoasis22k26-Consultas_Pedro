using System;
using System.Data;
using System.Windows.Forms;
using CapaControlador_AprobacionFinanciera;

namespace CapaVista_AprobacionFinanciera
{
    public partial class FrmCompra : Form
    {
        private ClsAprobacionFinanciera _Controlador = new ClsAprobacionFinanciera();
        private int _IdOrdenCompraBase;
        private decimal _MontoEsperadoBase;
        private DataTable _DetallesOrdenBase;

        public FrmCompra(int IdOrdenCompra, decimal MontoEsperado)
        {
            InitializeComponent();

            _IdOrdenCompraBase = IdOrdenCompra;
            _MontoEsperadoBase = MontoEsperado;

            this.Text = "6006 - ComprobacionCompra";
            this.MaximizeBox = false;

            ComprasMetCargarDatosBase();
        }

        private void ComprasMetCargarDatosBase()
        {
            // 1. Llenar Encabezado
            ComprasTxtNumOrden.Text = _IdOrdenCompraBase.ToString();
            ComprasTxtNumOrden.Enabled = false;

            ComprasTxtMontoEsperado.Text = _MontoEsperadoBase.ToString("N2");
            ComprasTxtMontoEsperado.Enabled = false;

            ComprasTxtBodega.Enabled = false;

            // 2. Llenar DataGridView
            try
            {
                _DetallesOrdenBase = _Controlador.ComprasFuncObtenerDetalleOrden(_IdOrdenCompraBase);
                ComprasDgvComprasrecibido.AutoGenerateColumns = true;
                ComprasDgvComprasrecibido.DataSource = _DetallesOrdenBase;
                ComprasDgvComprasrecibido.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Formato de columnas
                ComprasDgvComprasrecibido.Columns["Precio"].DefaultCellStyle.Format = "N2";
                ComprasDgvComprasrecibido.Columns["Subtotal"].DefaultCellStyle.Format = "N2";
                ComprasDgvComprasrecibido.Columns["CantidadPedida"].DefaultCellStyle.Format = "0.##";
                ComprasDgvComprasrecibido.Columns["CantidadRecibida"].DefaultCellStyle.Format = "0.##";

                // Ocultar columnas técnicas
                ComprasDgvComprasrecibido.Columns["Linea"].Visible = false;
                ComprasDgvComprasrecibido.Columns["IdBodega"].Visible = false;
                ComprasDgvComprasrecibido.Columns["Descuento"].Visible = false;

                // NUEVO: Mostrar automáticamente la bodega del primer producto en el encabezado
                if (_DetallesOrdenBase.Rows.Count > 0)
                {
                    ComprasTxtBodega.Text = _DetallesOrdenBase.Rows[0]["IdBodega"].ToString();
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error al cargar los detalles: " + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 3. Evento: Clic en el DataGridView
        private void ComprasDgvComprasrecibido_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && ComprasDgvComprasrecibido.Rows.Count > 0)
            {
                DataGridViewRow filaSelec = ComprasDgvComprasrecibido.Rows[e.RowIndex];

                // Producto (Ahora es TextBox según tu esquema)
                ComprasTxtProductos.Text = filaSelec.Cells["Producto"].Value.ToString();
                ComprasTxtProductos.Enabled = false;

                // Precio
                decimal precioFila = Convert.ToDecimal(filaSelec.Cells["Precio"].Value);
                ComprasTxtPrecio.Text = precioFila.ToString("N2");
                ComprasTxtPrecio.Enabled = false;

                // Cantidad
                decimal cantidadFila = Convert.ToDecimal(filaSelec.Cells["CantidadRecibida"].Value);
                ComprasTxtCantidad.Text = cantidadFila.ToString("0.##");

                // Bodega (Mostrar el ID de la bodega heredado de la orden)
                if (filaSelec.Cells["IdBodega"].Value != null)
                {
                    ComprasTxtBodega.Text = filaSelec.Cells["IdBodega"].Value.ToString();
                }

                ComprasDgvComprasrecibido.Tag = e.RowIndex;
            }
        }

        // 4. Evento: Botón Modificar
        // 4. Evento: Botón Modificar
        private void ComprasBtnModificarProducto_Click(object sender, EventArgs e)
        {
            if (ComprasDgvComprasrecibido.Tag != null)
            {
                int rowIndex = (int)ComprasDgvComprasrecibido.Tag;

                decimal nuevaCantidad;
                if (!decimal.TryParse(ComprasTxtCantidad.Text, out nuevaCantidad) || nuevaCantidad < 0)
                {
                    MessageBox.Show("Ingrese una cantidad válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataRowView vistaFila = (DataRowView)ComprasDgvComprasrecibido.Rows[rowIndex].DataBoundItem;
                DataRow filaDatos = vistaFila.Row;

                decimal cantidadPedida = Convert.ToDecimal(filaDatos["CantidadPedida"]);

                if (nuevaCantidad > cantidadPedida)
                {
                    MessageBox.Show($"La cantidad no puede superar lo pedido ({cantidadPedida}).", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Iniciar edición, modificar y CERRAR la edición (CRÍTICO)
                vistaFila.BeginEdit();
                filaDatos["CantidadRecibida"] = nuevaCantidad;
                decimal precio = Convert.ToDecimal(filaDatos["Precio"]);
                filaDatos["Subtotal"] = nuevaCantidad * precio;
                vistaFila.EndEdit(); // ESTO FIJA EL CAMBIO EN LA MEMORIA

                ComprasDgvComprasrecibido.Refresh();

                // Limpiar campos
                ComprasTxtProductos.Text = "";
                ComprasTxtPrecio.Text = "";
                ComprasTxtCantidad.Text = "";
                ComprasTxtBodega.Text = "";
                ComprasDgvComprasrecibido.Tag = null;
            }
            else
            {
                MessageBox.Show("Seleccione un producto de la tabla.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // 5. Evento: Botón Guardar
        private void ComprasBtnGuardar_Click(object sender, EventArgs e)
        {
            // Forzar a la tabla visual a soltar cualquier edición a medias
            ComprasDgvComprasrecibido.EndEdit();

            string NumeroFactura = ComprasTxtNumFactura.Text.Trim();
            string SerieFactura = ComprasTxtSerieFactura.Text.Trim();
            string MontoFacturaFisicaStr = ComprasTxtMontoFisica.Text.Trim();

            if (string.IsNullOrEmpty(NumeroFactura) || string.IsNullOrEmpty(SerieFactura) || string.IsNullOrEmpty(MontoFacturaFisicaStr))
            {
                MessageBox.Show("Debe llenar todos los campos de la factura.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal MontoFacturaFisicaValor;
            if (!decimal.TryParse(MontoFacturaFisicaStr, out MontoFacturaFisicaValor))
            {
                MessageBox.Show("El monto físico debe ser numérico.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // NUEVO: Sumar directamente desde la vista de la tabla (DataGridView)
            decimal TotalProductosRecibidos = 0;
            foreach (DataGridViewRow filaVisual in ComprasDgvComprasrecibido.Rows)
            {
                if (!filaVisual.IsNewRow) // Ignorar la última fila en blanco de la tabla
                {
                    TotalProductosRecibidos += Convert.ToDecimal(filaVisual.Cells["Subtotal"].Value);
                }
            }

            // Validar restricción dura
            if (TotalProductosRecibidos != MontoFacturaFisicaValor)
            {
                MessageBox.Show(
                    $"¡ERROR DE CUADRE!\n\nLa suma de los productos recibidos en la tabla (Q {TotalProductosRecibidos:N2}) NO coincide con la Factura Física (Q {MontoFacturaFisicaValor:N2}).",
                    "Restricción", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string EstadoCompra = "Pendiente";
            if (MontoFacturaFisicaValor == _MontoEsperadoBase)
            {
                EstadoCompra = "Cuadrada";
            }
            else
            {
                DialogResult Respuesta = MessageBox.Show(
                    "El monto físico no coincide con la Orden Original. ¿Desea registrar como 'No Cuadra'?",
                    "Descuadre", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (Respuesta == DialogResult.No) return;
                EstadoCompra = "No Cuadra";
            }

            // Guardar en BD 
            try
            {
                DateTime FechaEmisionFactura = ComprasDtpFechaemision.Value;
                DateTime FechaRecepcionFisica = ComprasDtpRecibido.Value;

                // Aquí pasamos _DetallesOrdenBase que ahora sí tiene los cambios gracias al EndEdit()
                _Controlador.ComprasFuncRegistrarComprobacion(
                    _IdOrdenCompraBase,
                    SerieFactura,
                    NumeroFactura,
                    FechaRecepcionFisica,
                    FechaEmisionFactura,
                    _MontoEsperadoBase,
                    MontoFacturaFisicaValor,
                    EstadoCompra,
                    "",
                    _DetallesOrdenBase
                );

                MessageBox.Show("Compra guardada en el inventario correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error al guardar en BD: " + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 6. Botón Descartar/Regresar
        private void ComprasBtnDescartar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}