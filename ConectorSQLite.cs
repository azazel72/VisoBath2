using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace VisoBath
{
    class ConectorSQLite
    {
        //Gestor
        static public Gestor gestor { get; set; }

        //bloqueo SQL
        static private readonly object bloqueoSQL = new object();

        //Conexion SQLite
        private const string NombreBBDD = "visobath.sqlite";

        //creacion de tablas
        private const string crearTablaAlbaranes = "CREATE TABLE IF NOT EXISTS albaranes (numeroAlbaran NVARCHAR(20) PRIMARY KEY, numeroPedido INTEGER DEFAULT 0, fechaAlbaran NVARCHAR(20)," +
            "nombreEmpresa NVARCHAR(100), dirEmpresa NVARCHAR(100), cpEmpresa INTEGER, " +
            "razonSocial NVARCHAR(100), direccion NVARCHAR(100), poblacion NVARCHAR(100), provincia NVARCHAR(10), nombreProvincia NVARCHAR(100), pais NVARCHAR(50), telefono NVARCHAR(20), email NVARCHAR(100)," +
            "totalBultos INTEGER DEFAULT 0, bultoActual INTEGER DEFAULT 0,  fechaIniciado NVARCHAR(20) DEFAULT '', fechaFinalizado NVARCHAR(20) DEFAULT '', estado INTEGER DEFAULT 0)";
        private const string crearTablaPalets = "CREATE TABLE IF NOT EXISTS palets (numero INTEGER DEFAULT 1, hora NVARCHAR(20), " +
            "peso INTEGER, volumen INTEGER, numeroAlbaran NVARCHAR(20), estado INTEGER DEFAULT 0, " +
            "PRIMARY KEY (numeroAlbaran, numero), FOREIGN KEY (numeroAlbaran) REFERENCES albaranes(numeroAlbaran) ON DELETE CASCADE)";
        private const string crearTablaConfiguracion = "CREATE TABLE IF NOT EXISTS configuracion (indice INTEGER PRIMARY KEY DEFAULT 0, nombreImpresora NVARCHAR(200))";
        private const string crearTablaRegistros = "CREATE TABLE IF NOT EXISTS registros (numeroAlbaran NVARCHAR(20), numero INTEGER, registro NVARCHAR(13500))";

        //SELECTS
        static private string selectAlbaranes = "SELECT * FROM albaranes WHERE estado >= 0;";
        static private string selectPalets = "SELECT * FROM palets WHERE estado >= 0;";
        static private string selectConfiguracion = "SELECT * FROM configuracion LIMIT 1;";

        //INSERT
        static private string sqlInsertarAlbaranes = "INSERT INTO albaranes ({0}) VALUES ({1});";
        static private string sqlInsertarPalets = "INSERT INTO palets ({0}) VALUES ({1});";
        static private string sqlInsertarConfiguracion = "INSERT OR REPLACE INTO configuracion ({0}) VALUES ({1});";
        static private string sqlInsertarRegistro = "INSERT OR REPLACE INTO registros (numeroAlbaran, numero, registro) VALUES ('{0}',{1},'{2}');";

        //UPDATE
        static private string sqlActualizarBultoActualAlbaran = "UPDATE albaranes SET bultoActual = {0} WHERE numeroAlbaran LIKE '{1}';";
        static private string sqlActualizarFinalizarAlbaran = "UPDATE albaranes SET fechaFinalizado = '{0}', bultoActual = totalBultos, estado = 1 WHERE numeroAlbaran LIKE '{1}';";
        static private string sqlActualizarFinalizarPalets = "UPDATE palets SET estado = 1 WHERE numeroAlbaran LIKE '{0}';";

        //DELETE
        static private string sqlEliminarAlbaran = "PRAGMA foreign_keys = ON; DELETE FROM albaranes WHERE numeroAlbaran LIKE '{0}';";

        /// <summary>
        /// Comprobamos si es posible realizar la conexion
        /// </summary>
        /// <returns></returns>
        static public bool Comprobar()
        {
            try
            {
                // Crea la base de datos si no existe
                if (!File.Exists(Path.GetFullPath(NombreBBDD)))
                {
                    SQLiteConnection.CreateFile(NombreBBDD);
                }
                // Crea las tablas si no existen
                SQLiteConnection conector = GetConector();
                new SQLiteCommand(crearTablaAlbaranes, conector).ExecuteNonQuery();
                new SQLiteCommand(crearTablaPalets, conector).ExecuteNonQuery();
                new SQLiteCommand(crearTablaConfiguracion, conector).ExecuteNonQuery();
                new SQLiteCommand(crearTablaRegistros, conector).ExecuteNonQuery();
                conector.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                string error = "Error (conexion bbdd): " + ex.Number.ToString() + " - " + ex.Message;
                gestor.Estado(error);
                MessageBox.Show(error, "Error en la BBDD", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }

        static public SQLiteDataReader CargarAlbaranes()
        {
            return Consulta(selectAlbaranes);
        }

        static public SQLiteDataReader CargarPalets()
        {
            return Consulta(selectPalets);
        }

        static public SQLiteDataReader CargarConfiguracion()
        {
            return Consulta(selectConfiguracion);
        }

        static public int InsertarAlbaran(Albaran albaran)
        {
            var query = string.Format(sqlInsertarAlbaranes, string.Join(",", albaran.GetCamposSQL()), string.Join(",", albaran.GetValoresSQL()));
            Console.WriteLine(query);
            return Comando(query);
        }

        static public int InsertarPalet(Palet palet)
        {
            var query = string.Format(sqlInsertarPalets, string.Join(",", palet.GetCamposSQL()), string.Join(",", palet.GetValoresSQL()));
            return Comando(query);
        }

        static public int InsertarConfiguracion(Configuracion configuracion)
        {
            var query = string.Format(sqlInsertarConfiguracion, string.Join(",", configuracion.GetCamposSQL()), string.Join(",", configuracion.GetValoresSQL()));
            return Comando(query);
        }
        static public int InsertarRegistro(string albaran, int palet, string trama)
        {
            var query = string.Format(sqlInsertarRegistro, albaran, palet, trama);
            Console.WriteLine(query);
            return Comando(query);
        }

        static public int CambiarBultosAlbaran(Albaran albaran)
        {
            string query;
            if (albaran.estado == 0)
            {
                query = string.Format(sqlActualizarBultoActualAlbaran, albaran.bultoActual.ToString(), albaran.numeroAlbaran);
            }
            else
            {
                query = string.Format(sqlActualizarFinalizarAlbaran, albaran.fechaFinalizado, albaran.numeroAlbaran);
                int i = Comando(query);
                if (i == 1)
                {
                    query = string.Format(sqlActualizarFinalizarPalets, albaran.numeroAlbaran);
                }
                else
                {
                    return i;
                }
            }
            //devolvera el numero de registros cambiados
            return Comando(query);
        }

        static public int EliminarAlbaran(Albaran albaran)
        {
            try
            {
                var query = string.Format(sqlEliminarAlbaran, albaran.numeroAlbaran);
                return Comando(query);
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message);
                return -e.ErrorCode;
            }
        }

        static private int Comando(string query)
        {
            try
            {
                SQLiteConnection conector = GetConector();
                return new SQLiteCommand(query, conector).ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message);
                return -e.ErrorCode;
            }
        }

        static private SQLiteDataReader Consulta(string query)
        {
            try
            {
                SQLiteConnection conector = GetConector();
                return new SQLiteCommand(query, conector).ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        static private SQLiteConnection GetConector()
        {
            var db = new SQLiteConnection(string.Format("Data Source={0};Version=3;", NombreBBDD));
            db.Open();
            return db;
        }
    }
}
