using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisoBath.MME;
using VisoBath.Conectores;

namespace VisoBath
{
    public partial class Gestor : Form
    {
        private Servidor servidor;
        private Configuracion configuracion;
        private Albaranes albaranes;
        //private Lidar2300 lidar;
        private Lidar2000 lidar;
        private enum Status { INICIADO, MIDIENDO, PARADO }
        private Status estado;
        private int contadorVueltas;
        private DateTime registro_fecha;

        private int pesoEstable = 0;
        private bool actualizandoTipoConector = false;

        public Gestor()
        {
            InitializeComponent();
            ConectorSQLite.gestor = this;

            //inicio de variables
            this.albaranes = new Albaranes();
            this.configuracion = new Configuracion();
        }

        private void Gestor_Load(object sender, EventArgs e)
        {
            Show();
            Task carga = new Task(CargarDatos);
            carga.Start();
            this.servidor = new Servidor(this);
            this.servidor.Iniciar();
            //this.lidar = new Lidar2300("169.254.244.116");
            this.lidar = new Lidar2000("192.78.70.173");
            //this.lidar = new Lidar2000("192.168.0.9");
            this.lidar.Activar();
            this.estado = Status.PARADO;
        }

        private void Gestor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (servidor != null)
            {
                servidor.Apagar();
            }
            if (this.lidar != null)
            {
                this.lidar.Apagar();
            }
        }

        /// <summary>
        /// Recupera los datos de la BBDD, los almacena en las variables y los muestra en las tablas
        /// </summary>
        private void CargarDatos()
        {
            //Estado(Path.GetFullPath("."));
            Estado("Conectando a BBDD...");
            ConectorSQLite.Comprobar();
            while (true)
            {
                if (ConectorSQLite.Comprobar())
                {
                    Estado("Conexion a BBDD establecida.");
                    break;
                }
                Estado("Reintentando conexión a BBDD...");
                Thread.Sleep(5000);
            }
            //Carga de datos
            this.configuracion.Cargar(ConectorSQLite.CargarConfiguracion());
            Estado("Configuracion cargada.");
            MostrarConfiguracion();

            this.albaranes.Agregar(ConectorSQLite.CargarAlbaranes());
            this.albaranes.RepartirPalets(ConectorSQLite.CargarPalets());
            Estado("Datos correctamente cargados.");

            //Mostrar datos
            MostrarAlbaranes();
        }

        private void MostrarConfiguracion()
        {
            Action actualizarImpresora = () =>
            {
                this.nombreImpresoraTxt.Text = this.configuracion.nombreImpresora;
                Etiquetas.nombreImpresora = this.configuracion.nombreImpresora;
            };

            if (this.nombreImpresoraTxt.InvokeRequired)
            {
                this.nombreImpresoraTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    actualizarImpresora();
                });
            }
            else
            {
                actualizarImpresora();
            }

            Action actualizarConector = () =>
            {
                if (this.tipoConectorCombo.Items.Count == 0)
                {
                    return;
                }

                string valor = string.IsNullOrWhiteSpace(this.configuracion.conexion) ? "SG" : this.configuracion.conexion;
                int indice = this.tipoConectorCombo.Items.IndexOf(valor);
                if (indice < 0)
                {
                    indice = 0;
                    valor = this.tipoConectorCombo.Items[0].ToString();
                    this.configuracion.conexion = valor;
                }

                if (this.tipoConectorCombo.SelectedIndex != indice)
                {
                    this.actualizandoTipoConector = true;
                    this.tipoConectorCombo.SelectedIndex = indice;
                    this.actualizandoTipoConector = false;
                }
            };

            if (this.tipoConectorCombo.InvokeRequired)
            {
                this.tipoConectorCombo.BeginInvoke((MethodInvoker)delegate ()
                {
                    actualizarConector();
                });
            }
            else
            {
                actualizarConector();
            }
        }

        /// <summary>
        /// Agrega un registro a una tabla desde otro hilo
        /// </summary>
        /// <param name="tabla"></param>
        /// <param name="entidades"></param>
        private void MostrarAlbaranes()
        {
            if (listadoAlbaranes.InvokeRequired)
            {
                listadoAlbaranes.BeginInvoke((MethodInvoker)delegate ()
                   {
                       listadoAlbaranes.Rows.Clear();
                       foreach (Albaran a in this.albaranes)
                       {
                           listadoAlbaranes.Rows.Add(a.GetValoresTabla());
                       }
                   }
                );
            }
            else
            {
                listadoAlbaranes.Rows.Clear();
                foreach (Albaran a in this.albaranes)
                {
                    listadoAlbaranes.Rows.Add(a.GetValoresTabla());
                }
            }
        }
        private void AgregarAlbaran(Albaran a, bool seleccionar = false)
        {
            if (listadoAlbaranes.InvokeRequired)
            {
                listadoAlbaranes.BeginInvoke((MethodInvoker)delegate ()
                {
                    listadoAlbaranes.Rows.Add(a.GetValoresTabla());
                    if (seleccionar)
                    {
                        this.listadoAlbaranes.CurrentCell = this.listadoAlbaranes[1, this.listadoAlbaranes.Rows.Count - 1];
                    }
                }
                );
            }
            else
            {
                listadoAlbaranes.Rows.Add(a.GetValoresTabla());
            }
            if (seleccionar)
            {
                this.listadoAlbaranes.CurrentCell = this.listadoAlbaranes[1, this.listadoAlbaranes.Rows.Count - 1];
            }
        }

        private void MostrarPalets(Albaran albaran)
        {
            if (listadoPalets.InvokeRequired)
            {
                listadoPalets.BeginInvoke((MethodInvoker)delegate ()
                {
                    listadoPalets.Rows.Clear();
                    foreach (Palet p in albaran)
                    {
                        listadoPalets.Rows.Add(p.GetValoresTabla());
                    }
                }
                );
            }
            else
            {
                listadoPalets.Rows.Clear();
                foreach (Palet p in albaran)
                {
                    listadoPalets.Rows.Add(p.GetValoresTabla());
                }
            }
        }


        private void ResetHilos_Click(object sender, EventArgs e)
        {
            ResetServidor();
        }

        public void ResetServidor()
        {
            servidor.ReiniciarServidores();
        }

        public void Estado(string texto)
        {
            textoEstado.Text = texto;
        }

        public void Debug(string texto)
        {
            if (this.debugTxt.InvokeRequired)
            {
                this.debugTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.debugTxt.Text = texto;
                }
                );
            }
            else
            {
                this.debugTxt.Text = texto;
            }
        }

        private void seleccionarAlbaranBtn_Click(object sender, EventArgs e)
        {
            //Comprobar que existe un codigo en el cuadro de texto
            string codigo = this.codigoAlbaranTxt.Text;
            if (codigo.Trim().Length == 0)
            {
                MessageBox.Show("Debe introducir un código de albarán", "Error de interfaz", MessageBoxButtons.OK);
            }

            //consultar si ya disponemos del codigo en nuestro listado
            Albaran albaran = this.albaranes.Buscar(codigo);

            if (albaran == null)
            {
                //si no disponemos del albaran, consultar el codigo del albaran en la base de datos
                bloquearFormulario(true);
                Estado("Solicitando información del albarán.");
                _ = ConectorFactory.SolicitarAlbaran(this, codigo, this.configuracion.conexion);
            }
            else
            {
                //lo marcamos en el listado.
                for (int i = 0; i < this.listadoAlbaranes.Rows.Count; i++)
                {
                    if (this.listadoAlbaranes[0, i].Value.ToString() == codigo)
                    {
                        this.listadoAlbaranes.CurrentCell = this.listadoAlbaranes[1, i];
                        break;
                    }
                }
            }

            this.codigoAlbaranTxt.Text = "";
        }

        public void NuevoAlbaran(Albaran albaran)
        {
            //agregamos al listado en memoria
            this.albaranes.Agregar(albaran);
            //agregamos al listado en pantalla
            this.AgregarAlbaran(albaran, true);
        }

        /// <summary>
        /// Esta funcion rellena el formulario con los datos del albarán pasado como argumento
        /// </summary>
        /// <param name="albaran"></param>
        private void MostrarFormulario(Albaran albaran)
        {
            this.numeroAlbaranTxt.Text = albaran.numeroAlbaran;
            this.numeroPedidoTxt.Text = albaran.numeroPedido.ToString();
            this.fechaAlbaranTxt.Text = ConvertirFecha(albaran.fechaAlbaran);
            this.fechaInicioTxt.Text = albaran.fechaIniciado;
            this.fechaFinTxt.Text = albaran.fechaFinalizado;
            this.listadoAlbaranes.Tag = albaran;

            this.nombreEmpresaTxt.Text = albaran.nombreEmpresa;
            this.direccionEmpresaTxt.Text = albaran.dirEmpresa;
            this.cpEmpresaTxt.Text = albaran.cpEmpresa.ToString();

            this.nombreClienteTxt.Text = albaran.razonSocial;
            this.direccionClienteTxt.Text = albaran.direccion;
            this.poblacionClienteTxt.Text = albaran.poblacion;
            this.provinciaClienteTxt.Text = albaran.nombreProvincia;
            this.paisClienteTxt.Text = albaran.pais;

            //comprobacion bultos
            if (albaran.totalBultos == 0)
            {
                this.totalBultosTxt.Value = 0;
                this.totalBultosTxt.Enabled = true;
                this.FijarBultosBtn.Enabled = true;
            }
            else
            {
                this.totalBultosTxt.Value = albaran.totalBultos;
                this.totalBultosTxt.Enabled = false;
                this.FijarBultosBtn.Enabled = false;
            }

            //calculo del proximo bulto
            if (albaran.bultoActual != albaran.totalBultos)
            {
                this.numeroBultoTxt.Text = (albaran.bultoActual + 1).ToString();
                this.asociarBtn.Enabled = true;
            }
            else
            {
                this.numeroBultoTxt.Text = "--";
                this.asociarBtn.Enabled = false;
            }
        }

        public void bloquearFormulario(bool estado)
        {
            grupoAlbaran.Enabled = !estado;
            grupoActivos.Enabled = !estado;
            grupoDatos.Enabled = !estado;
            grupoPalets.Enabled = !estado;
            grupoControl.Enabled = !estado;
        }

        private void listadoAlbaranes_SelectionChanged(object sender, EventArgs e)
        {
            if (this.listadoAlbaranes.SelectedRows.Count > 0)
            {
                string numeroAlbaran = this.listadoAlbaranes.SelectedRows[0].Cells[0].Value.ToString();
                this.MostrarPalets(this.albaranes.Buscar(numeroAlbaran));
                this.MostrarFormulario(this.albaranes.Buscar(numeroAlbaran));
            }
        }

        private void FijarBultosBtn_Click(object sender, EventArgs e)
        {
            Albaran albaran = (Albaran)this.listadoAlbaranes.Tag;
            if (this.totalBultosTxt.Value == 0)
            {
                MessageBox.Show("El número de bultos ha de ser mayor de cero", "Datos no válidos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                albaran.FijarBultos((int)this.totalBultosTxt.Value);
                int r = ConectorSQLite.InsertarAlbaran(albaran);
                if (r == 1)
                {
                    this.MostrarFormulario(albaran);
                    this.listadoAlbaranes.SelectedRows[0].Cells[2].Value = albaran.totalBultos;
                    Estado("Albaran insertado en la BBDD.");
                }
                else
                {
                    MessageBox.Show("No se pudo modificar la BBDD. Error nº: " + r.ToString(), "Error en la BBDD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private string ConvertirFecha(string fecha)
        {
            try
            {
                return DateTime.Parse(fecha).ToString();
                //return DateTime.ParseExact(fecha, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString();
            }
            catch
            {
                return "";
            }
        }

        private void codigoAlbaranTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                seleccionarAlbaranBtn_Click(this, new EventArgs());
            }
        }

        private void asociarBtn_Click(object sender, EventArgs e)
        {
            if (this.listadoAlbaranes.SelectedRows.Count > 0)
            {
                //creamos el nuevo palet
                Albaran albaran = (Albaran)this.listadoAlbaranes.Tag;
                Palet palet = new Palet();
                palet.numero = Int32.Parse(this.numeroBultoTxt.Text);
                palet.peso = Int32.Parse(this.pesoTxt.Text);
                palet.volumen = Int32.Parse(this.volumenTxt.Text);
                palet.numeroAlbaran = albaran.numeroAlbaran;
                albaran.AgregarPalet(palet);
                //actualizar bbdd
                int r = ConectorSQLite.InsertarPalet(palet);
                if (r != 1)
                {
                    MessageBox.Show("No se pudo insertar el palet en la BBDD. Error nº: " + r.ToString(), "Error en la BBDD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                r = ConectorSQLite.CambiarBultosAlbaran(albaran);
                if (r < 1)
                {
                    MessageBox.Show("No se pudo modificar la BBDD. Error nº: " + r.ToString(), "Error en la BBDD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //actualizar listado
                this.listadoAlbaranes.SelectedRows[0].Cells[1].Value = albaran.bultoActual;
                this.listadoAlbaranes.SelectedRows[0].Cells[2].Value = albaran.totalBultos;
                this.listadoAlbaranes.SelectedRows[0].Cells[3].Value = albaran.estado;
                this.MostrarFormulario(albaran);
                this.MostrarPalets(albaran);

                //ponemos a cero el peso y el volumen
                mostrarDatos(0, 0);

                //Etiquetas.ImprimirEtiqueta(this, albaran, palet);
                if (albaran.estado == 1)
                {
                    ConectorFactory.EnviarNotificacion(this, albaran, this.configuracion.conexion);
                }
            }
        }

        private void mostrarDatos(int peso, int volumen)
        {
            if (volumenTxt.InvokeRequired)
            {
                pesoTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.pesoTxt.Text = peso.ToString();
                }
                );
                volumenTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.volumenTxt.Text = volumen.ToString();
                }
                );
            }
            else
            {
                this.pesoTxt.Text = peso.ToString();
                this.volumenTxt.Text = volumen.ToString();
            }
        }

        private void mostrarPeso(int p)
        {
            if (this.pesoTxt.InvokeRequired)
            {
                this.pesoTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.pesoTxt.Text = p.ToString();
                }
                );
            }
            else
            {
                this.pesoTxt.Text = p.ToString();
            }

        }
        private void mostrarVolumen(int v)
        {
            if (this.volumenTxt.InvokeRequired)
            {
                this.volumenTxt.BeginInvoke((MethodInvoker)delegate ()
                {
                    this.volumenTxt.Text = v.ToString();
                }
                );
            }
            else
            {
                this.volumenTxt.Text = v.ToString();
            }
        }


        private void eliminarBtn_Click(object sender, EventArgs e)
        {
            Albaran albaran = (Albaran) this.listadoAlbaranes.Tag;
            if (albaran != null)
            {
                ConectorSQLite.EliminarAlbaran(albaran);
            }
        }

        private void listadoPalets_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            this.listadoPalets.Enabled = false;
            if (e.ColumnIndex == 4)
            {
                try
                {
                    int numeroPalet = Int32.Parse(this.listadoPalets[0, e.RowIndex].Value.ToString());
                    Albaran albaran = (Albaran)this.listadoAlbaranes.Tag;
                    Palet palet = albaran.ObtenerPalet(numeroPalet);
                    if (palet != null)
                    {
                        //Etiquetas.ImprimirEtiqueta(this, albaran, palet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            this.listadoPalets.Enabled = true;
        }

        private void seleccionarImpresoraBtn_Click(object sender, EventArgs e)
        {
            string impresora = Etiquetas.SeleccionImpresora(this);
            if (impresora != "")
            {
                this.configuracion.nombreImpresora = impresora;
                int r = ConectorSQLite.InsertarConfiguracion(this.configuracion);
                if (r == 1)
                {
                    this.MostrarConfiguracion();
                }
                else
                {
                    MessageBox.Show("No se pudo insertar la configuracion en la BBDD. Error nº: " + r.ToString(), "Error en la BBDD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public void EncenderTestigo(bool encender)
        {
            if (testigoServidor.InvokeRequired)
            {
                testigoServidor.BeginInvoke((MethodInvoker)delegate ()
                {
                    testigoServidor.Visible = encender;
                }
                );
            }
            else
            {
                testigoServidor.Visible = encender;
            }
        }

        public void AccionRecibida(NotificacionPLC notificacion)
        {
           // Console.WriteLine(notificacion.accion);
            switch (notificacion.accion)
            {
                case "inicio":
                    //boton de inicio, recogemos las tramas
                    this.lidar.Iniciar();
                    this.contadorVueltas = 0;
                    Console.WriteLine(this.lidar.Tramas());
                    this.estado = Status.INICIADO;
                    Console.WriteLine("Iniciando sensor");
                    break;
                case "peso":
                    Console.WriteLine("Peso: " + notificacion.valor.ToString());
                    Estado("Peso: " + notificacion.valor.ToString());
                    this.pesoEstable = notificacion.valor;
                    break;
                case "vuelta":
                    if (this.estado == Status.INICIADO)
                    {
                        this.contadorVueltas++;
                        if (this.contadorVueltas > 1)
                        {
                            this.estado = Status.MIDIENDO;
                            this.lidar.Registrar(true);

                            Console.WriteLine("INICIAR recogida");
                        }
                    }
                    else if (this.estado == Status.MIDIENDO)
                    {
                        this.estado = Status.PARADO;
                        Console.WriteLine("PARAR sensor");

                        this.lidar.Registrar(false);
                        this.lidar.Parar();
                        this.mostrarPeso(this.pesoEstable);
                        float[] datos = this.lidar.Calcular();
                        if (datos != null)
                        {
                            int vol = (int)(datos[0] * datos[1] / 1000000);
                            this.mostrarVolumen(vol);
                            int lado1 = (int)(datos[3] - datos[2]);
                            int lado2 = (int)(datos[5] - datos[4]);
                            string resumen = "Peso: " + this.pesoEstable + ", Volumen: " + vol.ToString() + ", Lado 1: " + lado1.ToString() + ", Lado 2: " + lado2.ToString() + ", Altura: " + datos[0].ToString();
                            Console.WriteLine(resumen);                        
                        }
                    }
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.lidar.Activar(null, null, false);
        }

        private void tipoConectorCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.actualizandoTipoConector || this.tipoConectorCombo.SelectedItem == null)
            {
                return;
            }

            string seleccionado = this.tipoConectorCombo.SelectedItem.ToString();
            if (seleccionado == this.configuracion.conexion)
            {
                return;
            }

            this.configuracion.conexion = seleccionado;
            Estado("Conector ERP establecido en " + seleccionado + ".");
        }
    }
}
