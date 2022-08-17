using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BancoGeneral.Yappy
{
    public class BGFirma
    {
        private const string YAPPY_PLUGIN_VERSION = "N1.0";
        private const string URL_SITE = "https://pagosbg.bgeneral.com";
        private const string DEFAULT_ORDER_ID = "PEDIDO WEB";
        private const string EMPTY_STRING = "";
        private const double NO_AMOUNT = 0.00;
        public const string YAPPY_ERROR = "Algo salió mal. Contacta con el administrador.";

        public double Total { get; }
        public double Subtotal { get; }
        public double Taxes { get; }
        public string Currency { get; }
        public string PaymentMethod { get; }
        public string TransactionType { get; }
        public string OrderId { get; }
        public string SuccessUrl { get; }
        public string FailUrl { get; }
        public string Domain { get; }
        public string Tel { get; }
        public double Shipping { get; }
        public double Discount { get; }
        public string Sandbox { get; }
        private string MerchantId { get; }
        private string SecretKey { get; }
        private long PaymentDate { get; set; }

        private string JwtToken { get; set; }

        private static readonly Encoding encoding = Encoding.UTF8;

        public BGFirma(
            string domain,
            double total,
            double subtotal = NO_AMOUNT,
            double taxes = NO_AMOUNT,
            double shipping = NO_AMOUNT,
            double discount = NO_AMOUNT,
            string orderId = DEFAULT_ORDER_ID,
            string successUrl = EMPTY_STRING,
            string failUrl = EMPTY_STRING,
            string tel = EMPTY_STRING
            )
        {
            this.Domain = domain;
            this.Total = total;
            this.Subtotal = subtotal;
            this.Taxes = taxes;
            this.Currency = "USD";
            this.TransactionType = "VEN";
            this.PaymentMethod = "YAP";
            this.OrderId = orderId ?? DEFAULT_ORDER_ID;
            this.SuccessUrl = successUrl ?? EMPTY_STRING;
            this.FailUrl = failUrl ?? EMPTY_STRING;
            this.Tel = tel ?? EMPTY_STRING;
            this.Shipping = shipping;
            this.Discount = discount;
            this.Sandbox = Environment.GetEnvironmentVariable("MODO_DE_PRUEBAS") == "true" ? "yes" : "no";
            this.MerchantId = Environment.GetEnvironmentVariable("ID_DEL_COMERCIO");
            this.SecretKey = Environment.GetEnvironmentVariable("CLAVE_SECRETA");
        }

        private CredentialsResponse CheckCredentials()
        {
            CredentialsResponse errorResponse = new CredentialsResponse();

            try
            {
                if (IsInvalidDomain(this.Domain))
                {
                    errorResponse.error = new ErrorResponse("EC-001", "Dominio con formato incorrecto");
                    return errorResponse;
                }

                var bytes = Convert.FromBase64String(this.SecretKey);
                var secret = Encoding.UTF8.GetString(bytes);

                string[] secrets = secret.Split('.');

                WebRequest oRequest = WebRequest.Create(string.Format(URL_SITE + "/validateapikeymerchand/"));
                oRequest.Method = "POST";
                oRequest.ContentType = "application/json";
                oRequest.Headers.Add("x-api-key", $"{secrets[1]}");
                oRequest.Headers.Add("version", YAPPY_PLUGIN_VERSION);

                string postData = "{\"merchantId\":\"" + this.MerchantId + "\"," +
                                   "\"urlDomain\":\"" + this.Domain + "\"}";

                try
                {
                    using (var oSW = new StreamWriter(oRequest.GetRequestStream()))
                    {
                        oSW.Write(postData);
                        oSW.Flush();
                        oSW.Close();
                    }

                    WebResponse oResponse = oRequest.GetResponse();

                    using (var oSR = new StreamReader(oResponse.GetResponseStream()))
                    {
                        var result = oSR.ReadToEnd();
                        CredentialsResponse response = JsonSerializer.Deserialize<CredentialsResponse>(result);

                        if (response.success == false)
                        {
                            response.error = new ErrorResponse("EC-002", "Credenciales inválidas");
                        }
                        return response;
                    }
                }
                catch (WebException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al validar credenciales: {ex}");
                    errorResponse.error = new ErrorResponse("EC-000", YAPPY_ERROR);
                    return errorResponse;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al validar credenciales: {ex}");
                errorResponse.error = new ErrorResponse("EC-002", $"Error al validar credenciales: {ex}");
                return errorResponse;
            }
        }

        private string ConcatElements()
        {
            return string.Format("{0:0.00}", this.Total) +
                this.MerchantId +
                this.PaymentDate.ToString() +
                this.PaymentMethod +
                this.TransactionType +
                this.OrderId +
                this.SuccessUrl +
                this.FailUrl +
                this.Domain;
        }

        private static string CreateHash(string data, string secretKey)
        {
            var bytes = Convert.FromBase64String(secretKey);
            var secret = Encoding.UTF8.GetString(bytes);

            string[] secrets = secret.Split('.');

            var keyByte = encoding.GetBytes(secrets[0]);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(encoding.GetBytes(data));

                return ByteToString(hmacsha256.Hash).ToLower();
            }
        }
        private static string ByteToString(byte[] buff)
        {
            string sbinary = "";
            for (int i = 0; i < buff.Length; i++)
                sbinary += buff[i].ToString("X2"); /* hex format */
            return sbinary;
        }

        public UrlResponse GenerateURL()
        {
            var credentials = CheckCredentials();

            if (credentials.success)
            {
                this.PaymentDate = credentials.unixTimestamp * 1000;
                this.JwtToken = credentials.accessToken;

                var checkFields = CheckFields();
                if (checkFields.valid)
                {
                    var yappyUrl = URL_SITE +
                        $"?merchantId={this.MerchantId}" +
                        $"&total={this.Total}" +
                        $"&subtotal={this.Subtotal}" +
                        $"&taxes={this.Taxes}" +
                        $"&paymentDate={this.PaymentDate}" +
                        $"&paymentMethod={this.PaymentMethod}" +
                        $"&transactionType={this.TransactionType}" +
                        $"&orderId={this.OrderId}" +
                        $"&successUrl={HttpUtility.UrlEncode(this.SuccessUrl, Encoding.UTF8)}" +
                        $"&failUrl={HttpUtility.UrlEncode(this.FailUrl, Encoding.UTF8)}" +
                        $"&domain={HttpUtility.UrlEncode(this.Domain, Encoding.UTF8)}" +
                        $"&jwtToken={this.JwtToken}" +
                        $"&platform=desarrollopropionet" +
                        $"&signature={CreateHash(ConcatElements(), this.SecretKey)}" +
                        $"&tel={this.Tel.Replace("-", EMPTY_STRING)}" +
                        $"&shipping={this.Shipping}" +
                        $"&discount={this.Discount}" +
                        $"&sbx={this.Sandbox}";

                    return new UrlResponse(success: true, yappyUrl);
                }
                else
                {
                    return new UrlResponse(success: false, error: checkFields.error);
                }

            }
            else
            {
                return new UrlResponse(success: false, error: credentials.error);
            }
        }

        private FieldsResponse CheckFields()
        {
            if (this.Total <= 0 || IsInvalidAmount(this.Total))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-005", "Formato del total inválido"));
            }
            else if (this.Shipping < 0 || IsInvalidAmount(this.Shipping))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-009", "Formato de shipping inválido"));
            }
            else if (this.Discount < 0 || IsInvalidAmount(this.Discount))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-008", "Formato de descuento inválido"));
            }
            else if (this.Subtotal < 0 || IsInvalidAmount(this.Subtotal))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-006", "Formato del subtotal inválido"));
            }
            else if (this.Taxes < 0 || IsInvalidAmount(this.Taxes))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-007", "Formato del impuesto inválido"));
            }
            else if (this.OrderId.Length > 15)
            {
                return new FieldsResponse(false, new ErrorResponse("EC-010", "Formato de número de pedido inválido"));
            }
            else if (IsInvalidPhone(this.Tel))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-011", "Formato de celular inválido"));
            }
            else if (IsInvalidUrl(this.SuccessUrl, true))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-012", "Formato de URL de éxito inválido"));
            }
            else if (IsInvalidUrl(this.FailUrl, true))
            {
                return new FieldsResponse(false, new ErrorResponse("EC-013", "Formato de URL de fallo inválido"));
            }
            else
            {
                return new FieldsResponse(true);
            }
        }

        private bool IsInvalidAmount(double amount)
        {
            Regex rx = new Regex(@"^[0-9]{0,4}(\.[0-9]{1,2})?$",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = rx.Matches(amount.ToString());

            return matches.Count == 0;
        }

        private bool IsInvalidUrl(string domain, bool optional = false)
        {
            if (optional && EMPTY_STRING == domain)
            {
                return false;
            }

            return !Uri.IsWellFormedUriString(domain, UriKind.Absolute);
        }

        private bool IsInvalidDomain(string domain)
        {

            Regex rx = new Regex(@"^(https:\/\/www\.|https:\/\/)?[a-zñ0-9]+([\-\.]{1}[a-zñ0-9]+)*\.[a-z]{2,10}(:[0-9]{1,5})?(\/.*)?$",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = rx.Matches(domain);

            return matches.Count == 0;
        }

        private bool IsInvalidPhone(string tel)
        {

            if (EMPTY_STRING == tel)
            {
                return false;
            }

            Regex rx = new Regex(@"^[0-9]{4}([-]?[0-9]{4})?$",
              RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection matches = rx.Matches(tel);

            return matches.Count == 0;
        }

        public static bool VerifyParams(string orderId, string status, string domain, string hash)
        {
            return hash == CreateHash(orderId + status + domain, Environment.GetEnvironmentVariable("CLAVE_SECRETA"));
        }
    }

    public class CredentialsResponse
    {
        public bool success { get; set; }
        public string accessToken { get; set; }
        public long unixTimestamp { get; set; }
        public ErrorResponse error { get; set; }
    }

    public class UrlResponse
    {
        public bool success { get; set; }
        public string url { get; set; }
        public ErrorResponse error { get; set; }

        public UrlResponse(bool success, string url = "", ErrorResponse error = null)
        {
            this.success = success;
            this.url = url;
            this.error = error;
        }
    }

    public class FieldsResponse
    {
        public bool valid { get; set; }
        public ErrorResponse error { get; set; }

        public FieldsResponse(bool valid, ErrorResponse error = null)
        {
            this.valid = valid;
            this.error = error;
        }
    }

    public class ErrorResponse
    {
        public string code { get; set; }
        public string message { get; set; }

        public ErrorResponse(string code, string error)
        {
            this.code = code;
            this.message = error;
        }
    }
}
