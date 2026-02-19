using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VisoBath
{
	public class ResultadoTira
	{
		/*
		public int organizacion { get; set; }
		public string orgAlias { get; set; }
		public string orgRazonSocial { get; set; }
		public string usuario { get; set; }
		public int empresa { get; set; }
		public string centro { get; set; }
		public string nombreEmpresa { get; set; }
		public string nombreCentro { get; set; }
		public string nombreUsuario { get; set; }
		public long tiraInit { get; set; }
		*/
		public string tira { get; set; }
	}

	public class ResultadoAlbaran
	{
		public List<Albaran> result { get; set; }
		public string statusCode { get; set; }
		public string statusText { get; set; }
		public ResultadoMensaje infoMsg { get; set; }
		public ResultadoMensaje errorMsg { get; set; }
	}

	public class ResultadoMensaje
	{
		public string message { get; set; }
		public int code { get; set; }
	}
}

