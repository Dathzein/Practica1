using BancoGeneral.Yappy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using YappiBoton1.Models;

namespace YappiBoton1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotonOneController : ControllerBase
    {
        [HttpPost]
        public UrlResponse EnviarDatos([FromBody]BotonYappi yappi)
        {
           
            
            try
            {
                
                    var bfFirma = new BGFirma(
                    domain: yappi.Domain,
                    total: yappi.Total,
                    subtotal: yappi.SubTotal,
                    taxes: yappi.Impuesto,
                    shipping: yappi.Shipping,
                    discount: yappi.Discount,
                    orderId: yappi.OrderId,
                    successUrl: yappi.SuccessUrl,
                    failUrl: yappi.FailUrl,
                    tel: yappi.Telefono
                    );

                    var yappyPayment = bfFirma.GenerateURL();

                    return yappyPayment;
                
                

            }
            catch(Exception ex)
            {
                throw new Exception("Revisar: ", ex);
            }
        }
    }
}
