using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisoBath
{
    class Etiquetas
    {
        public static string nombreImpresora = "";

        public static void ImprimirEtiqueta(Gestor g, Albaran albaran, Palet palet)
        {
            if (Etiquetas.nombreImpresora != "")
            {
                StringBuilder label = CrearEtiqueta(albaran, palet);
                Imprimir(Etiquetas.nombreImpresora, label.ToString());
            }
        }

        private static string CrearEtiqueta(int x, int y, string fuente, string texto)
        {
            if (true)
            {
                string textoFormateado = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(texto));
                //string textoFormateado = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(texto));
                return string.Format("^FO{0},{1}^{2}^FD{3}^FS", x, y, fuente, textoFormateado);
            }
            else
            {
                return string.Format("^FO{0},{1}^{2}^FD{3}^FS", x, y, fuente, texto);
            }            
        }
        private static string CrearLinea(int x, int y, int ancho, int alto, int grosor)
        {
            return string.Format("^FO{0},{1}^GB{2},{3},{4}^FS", x, y, ancho, alto, grosor);
        }
        private static string CrearCuadro(int x, int y, int ancho, int alto, int grosor)
        {
            return string.Format("^FO{0},{1}^GB{2},{3},{4}^FS", x, y, ancho, alto, grosor);
        }
        private static string CrearCodigoBarras(int x, int y, int ancho, string valor)
        {
            return string.Format("^BY5,2,270^FO{0},{1}^BCN,{2},Y,N,N^FD{3}^FS", x, y, ancho, valor);
        }

        public static string SeleccionImpresora(Gestor g)
        {
            PrintDialog pd = new PrintDialog();
            pd.UseEXDialog = true;
            pd.PrinterSettings = new PrinterSettings();
            if (System.Windows.Forms.DialogResult.OK == pd.ShowDialog(g))
            {
                return pd.PrinterSettings.PrinterName;
            }
            else
            {
                return "";
            }
        }

        private static bool Imprimir(string impresora, string raw)
        {
            try
            {
                return RawPrinterHelper.SendStringToPrinter(impresora, raw);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static StringBuilder CrearEtiqueta(Albaran albaran, Palet palet)
        {
            string fuenteFrom = "ADN,30,12";
            string fuenteProveedor = "A0N,30,40";
            string fuenteCliente = "ADN,30,15";
            string fuenteGrande = "A0N,120,120";
            string fuenteBultos = "A0N,50,50";
            int offsetX = 80;

            StringBuilder label = new StringBuilder();
            label.AppendLine("^XA");
            //bloque proveedor
            label.AppendLine(CrearEtiqueta(offsetX - 10, 40, fuenteFrom, "From:"));
            label.AppendLine(CrearEtiqueta(offsetX, 90, fuenteProveedor, albaran.nombreEmpresa));
            label.AppendLine(CrearEtiqueta(offsetX, 130, fuenteProveedor, albaran.dirEmpresa));
            label.AppendLine(CrearEtiqueta(offsetX, 170, fuenteProveedor, albaran.cpEmpresa.ToString()));
            label.AppendLine(CrearEtiqueta(offsetX, 210, fuenteProveedor, "Ref: " + albaran.numeroPedido.ToString()));
            label.AppendLine(CrearEtiqueta(offsetX + 450, 210, fuenteProveedor, "Fecha: 27/01/2021"));
            //bloque cliente
            label.AppendLine(CrearLinea(offsetX - 20, 270, 810, 3, 3));
            label.AppendLine(CrearEtiqueta(offsetX - 10, 300, fuenteFrom, "To:"));
            label.AppendLine(CrearEtiqueta(offsetX, 350, fuenteCliente, albaran.razonSocial));
            label.AppendLine(CrearEtiqueta(offsetX, 390, fuenteCliente, albaran.direccion));
            label.AppendLine(CrearEtiqueta(offsetX, 430, fuenteCliente, albaran.poblacion));
            label.AppendLine(CrearEtiqueta(offsetX, 470, fuenteCliente, albaran.nombreProvincia));
            label.AppendLine(CrearLinea(offsetX - 20, 530, 810, 3, 3));
            //bloque INICIALES
            //label.AppendLine(CrearEtiqueta(offsetX - 10, 520, fuenteGrande, "CCS"));
            //label.AppendLine(CrearEtiqueta(offsetX + 450, 520, fuenteGrande, "CAC"));
            //bloque codigo barras
            label.AppendLine(CrearCodigoBarras(offsetX + 150, 630, 175, albaran.numeroPedido.ToString()));

            //bloque dimensiones
            label.AppendLine(CrearEtiqueta(offsetX - 10, 960, fuenteCliente, "Peso:"));
            label.AppendLine(CrearEtiqueta(offsetX + 200, 960, fuenteCliente, palet.peso.ToString().PadLeft(5)));
            label.AppendLine(CrearEtiqueta(offsetX - 10, 1010, fuenteCliente, "Volumen:"));
            label.AppendLine(CrearEtiqueta(offsetX + 200, 1010, fuenteCliente, palet.volumen.ToString().PadLeft(5)));

            //bloque bultos
            label.AppendLine(CrearCuadro(offsetX + 480, 930, 250, 150, 3));
            label.AppendLine(CrearEtiqueta(offsetX + 545, 960, fuenteProveedor, "BULTOS"));
            label.AppendLine(CrearEtiqueta(offsetX + 510, 1010, fuenteBultos, palet.numero.ToString().PadLeft(3, '0')));
            label.AppendLine(CrearEtiqueta(offsetX + 592, 1015, fuenteFrom, "de"));
            label.AppendLine(CrearEtiqueta(offsetX + 630, 1010, fuenteBultos, albaran.totalBultos.ToString().PadLeft(3, '0')));

            label.AppendLine("^XZ");
            return label;
        }
        /*label.AppendLine("^XA");
label.AppendLine("^FO600,110");
label.AppendLine("^FO50,50^ADN,30,12^FDSHIP TO:^FS");
label.AppendLine("^FO50,100^A0N,30,30^FD");
label.AppendLine("ERICA RIVERA");
label.AppendLine("^FS~NAME^FS");
label.AppendLine("^FO50,140^A0N,30,30^FD");
label.AppendLine("BETHUNE SCHOOL");
label.AppendLine("^FS~COMPANY^FS");
label.AppendLine("^FO50,180^A0N,30,30^FD");
label.AppendLine("166 S FRUIT AVE");
label.AppendLine("^FS");
label.AppendLine("^FO50,220^A0N,30,30^FD^FS~REF1^FS");
label.AppendLine("^FO50,250^A0N,30,40^FD");
label.AppendLine("FRESNO CA 93706-2899");
label.AppendLine("^FS~CITY ~STATE ~ZIP^FS");
label.AppendLine("^FO50,325^ADN,30,12^FDORDER #:^FS");
label.AppendLine("^FO160,325^A0N,30,50^FD");
label.AppendLine("P045722701017");
label.AppendLine("^FSFDORDER #: ~ORDERNO^FS");
label.AppendLine("^FO50,400^ADN,30,12^FD");
label.AppendLine("GROUND SERVICE");
label.AppendLine("^FS~SHIPMETHOD^FS");
label.AppendLine("^FO650,400^ADN,30,12^FD");
label.AppendLine("BOX 1 of 2");
label.AppendLine("^FSBOX: ~BOXCNT^FS");
label.AppendLine("^FO50,450^B3^BY3N,N,100,Y,N,^FD700830865038^FS");
label.AppendLine("^XZ");*/


    }
}
