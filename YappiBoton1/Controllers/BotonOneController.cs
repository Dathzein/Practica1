using BancoGeneral.Yappy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using YappiBoton1.Models;

namespace YappiBoton1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotonOneController : ControllerBase
    {

        

        [HttpPost]
        public UrlResponse EnviarDatos(BotonYappi yappi)
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

                if (!yappyPayment.success)
                {

                    return yappyPayment;
                }
                else
                {
                    //WebHook
                    return yappyPayment;
                }
                
                
            }
            catch(Exception ex)
            {
                throw new Exception("Revisar: ", ex);
            }
        }

        public static List<string> pruebaWebHook(string url)
        {
            List<string> json = null;
            url.Trim();
            json.Add(url);
            return json;
            //var wr = WebRequest.Create(url);
            //wr.ContentType = "application/json";
            //wr.Method = "post";
            //using (var sw = new StreamWriter(wr.GetRequestStream()))
            //{
            //    sw.write(json);
            //}
            //wr.getresponse();
            //return yappyPayment;
        }
    }
}
