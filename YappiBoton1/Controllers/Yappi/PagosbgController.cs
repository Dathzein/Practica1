using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using BancoGeneral.Yappy;
using System.Text;

namespace BancoGeneral.Yappy
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagosbgController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(
            [FromQuery] string orderId,
            [FromQuery] string status,
            [FromQuery] string hash,
            [FromQuery] string domain,
            [FromQuery] string confirmationNumber)
        {

            if (BGFirma.VerifyParams(orderId, status, domain, hash))
            {
                // La firma es válida, manejo de la lógica de negocio a discreción del cliente...

                // Ejemplo básico:

                if (status == "E")
                {
                    // Pedido pagado correctamente
                    return Ok(new { success = true, confirmation = confirmationNumber });
                }
                else if (status == "C")
                {
                    // Pedido cancelado por el usuario
                    return Ok(new { success = true });
                }
                else if (status == "R")
                {
                    // Pedido rechazado por el banco
                    return Ok(new { success = true });
                }
                else
                {
                    // Estado no reconocido
                    return Ok(new { success = false });
                }
            }
            else
            {
                return Ok(new { success = false });
            }

        }
    }
}
