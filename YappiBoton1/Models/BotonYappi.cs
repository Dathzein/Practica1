using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YappiBoton1.Models
{
    public class BotonYappi
    {
        public string Domain { get; set; }
        public double Total { get; set; }
        public double SubTotal { get; set; }
        public double Impuesto { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string OrderId { get; set; }
        public double Discount { get; set; }
        public double Shipping { get; set; }
        public string Telefono { get; set; }
    }

    public class ErrorEx
    {
        public int CodigoError { get; set; }
        public string MensajeError { get; set; }

    }
}
