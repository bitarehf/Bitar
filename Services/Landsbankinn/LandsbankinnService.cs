using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Bitar.Models;
using Bitar.Models.Settings;
using Landsbankinn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bitar.Services
{
    public class LandsbankinnService
    {
        private readonly ILogger _logger;
        private readonly LandsbankinnSettings _options;
        private CookieContainer _cookies = null;
        private readonly string _url = "https://b2b.fbl.is/lib2b.dll?processXML";
        public string sessionId;
        private X509Certificate2 _certificate;
        public HashSet<Transaction> transactions = new HashSet<Transaction>();

        public LandsbankinnService(ILogger<LandsbankinnService> logger, IOptions<LandsbankinnSettings> options)
        {
            _logger = logger;
            _options = options.Value;
            _certificate = new X509Certificate2(Convert.FromBase64String(_options.Certificate), _options.CertificatePassword);

            Login(_options.Username, _options.Password);
        }

        private bool signed
        {
            get { return _certificate != null; }
        }

        private void Login(string username, string password)
        {
            sessionId = null;

            LI_Innskra login = new LI_Innskra();
            login.notandanafn = username;
            login.lykilord = password;
            login.version = 1.1m;

            LI_Innskra_svar oResponse = (LI_Innskra_svar)SendAndReceive(login, Type.GetType("Landsbankinn.LI_Innskra_svar"));
            sessionId = oResponse.seta;
        }

        public async Task<List<LI_Fyrirspurn_reikningsyfirlit_svarFaersla>> FetchTransactions()
        {
            var request = new LI_Fyrirspurn_reikningsyfirlit()
            {
                dags_fra = DateTime.Today,
                dags_til = DateTime.Today,
                reikningur = new LI_reikningur_type() { utibu = "0133", hb = "15", reikningsnr = "200640" },
                seta = "",
                version = 1.1m,
            };

            try
            {
                LI_Fyrirspurn_reikningsyfirlit_svar response = (LI_Fyrirspurn_reikningsyfirlit_svar)SendAndReceive(request, Type.GetType("Landsbankinn.LI_Fyrirspurn_reikningsyfirlit_svar"));
                if (response != null)
                {
                    _logger.LogInformation($"Account balance: {response.stada_reiknings}");
                    if (response.faerslur != null)
                    {
                        _logger.LogCritical($"Count: {response.faerslur.Count()}");
                        return response.faerslur.ToList();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return null; // No transactions received.
        }

        public async Task<List<Stock>> FetchCurrencyUpdates()
        {
            var request = new LI_Fyrirspurn_gengi_gjaldmidla()
            {
                dags = DateTime.Today,
                gengistegund = LI_gengistegund_type.A, // Almennt Landsbankagengi.
                seta = "",
                version = 1.1m
            };

            List<Stock> stocks = new List<Stock>();

            try
            {
                LI_Fyrirspurn_gengi_gjaldmidla_svar response = (LI_Fyrirspurn_gengi_gjaldmidla_svar)SendAndReceive(request, Type.GetType("Landsbankinn.LI_Fyrirspurn_gengi_gjaldmidla_svar"));
                if (response != null)
                {
                    foreach (var item in response.gjaldmidlar)
                    {
                        if (item.iso_takn == LI_ISO_takn_gjaldmidils_type.EUR)
                        {
                            stocks.Add(new Stock()
                            {
                                Symbol = Symbol.EUR,
                                    Price = item.solugengi
                            });
                        }

                        if (item.iso_takn == LI_ISO_takn_gjaldmidils_type.USD)
                        {
                            stocks.Add(new Stock()
                            {
                                Symbol = Symbol.USD,
                                    Price = item.solugengi
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return stocks;
        }

        public bool Pay(string utibu, string hb, string reikingsnr, string kennitala, decimal amount)
        {
            try
            {
                LI_Greidsla payment = new LI_Greidsla();
                payment.version = 1.1m;
                payment.seta = "";
                payment.upphaed = new LI_GreidslaUpphaed();
                payment.upphaed.mynt = LI_ISO_takn_gjaldmidils_type.ISK;
                payment.upphaed.Value = amount;

                // Withdrawal account.
                payment.ut = new LI_GreidslaUT();
                LI_reikningur_type withdraw = new LI_reikningur_type()
                {
                    utibu = "0133",
                    hb = "15",
                    reikningsnr = "200640"
                };
                payment.ut.reikningur = withdraw;

                // Deposit account.
                payment.inn = new LI_GreidslaInn();
                LI_millifaersla_type transfer = new LI_millifaersla_type();
                LI_reikningur_type deposit = new LI_reikningur_type()
                {
                    utibu = utibu,
                    hb = hb,
                    reikningsnr = reikingsnr
                };
                transfer.reikningur = deposit;
                transfer.kennitala = kennitala;
                transfer.textalykill = "03";
                transfer.tilvisun = "1337";
                payment.inn.Item = transfer;

                LI_Greidsla_svar response = (LI_Greidsla_svar)SendAndReceive(payment, Type.GetType("Landsbankinn.LI_Greidsla_svar"));
                if (response != null)
                {
                    _logger.LogInformation("Payment has been performed: id = " + response.id_bokun.ToString("F0"));
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            return false;
        }

        //public LI_Fyrirspurn_reikningsyfirlit_svar AccountStatement(string utibu, string hb, string reikingsnr)
        //{
        //    try
        //    {
        //        LI_reikningur_type account = new LI_reikningur_type()
        //        {
        //            utibu = utibu,
        //            hb = hb,
        //            reikningsnr = reikingsnr
        //        };

        //        LI_Fyrirspurn_reikningsyfirlit accountStatement = new LI_Fyrirspurn_reikningsyfirlit()
        //        {
        //            reikningur = account,
        //            dags_fra = DateTime.Today,
        //            dags_til = DateTime.Today,
        //            seta = ""
        //        };

        //        LI_Fyrirspurn_reikningsyfirlit_svar response = (LI_Fyrirspurn_reikningsyfirlit_svar)SendAndReceive(accountStatement, Type.GetType("Landsbankinn.LI_Fyrirspurn_reikningsyfirlit_svar"));
        //        return response;
        //    }
        //    catch (Exception e)
        //    {
        //        throw e;
        //    }
        //}

        public object SendAndReceive(object oOutObj, Type typeResponse)
        {
            try
            {
                // Serialize output object to memory.
                MemoryStream ms = new MemoryStream();
                XmlTextWriter memWriter = new XmlTextWriter(ms, Encoding.UTF8);
                XmlSerializer xs = new XmlSerializer(oOutObj.GetType());
                xs.Serialize(memWriter, oOutObj);

                // Prepare document to send.
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                ms.Position = 0;
                doc.Load(ms);
                ms.Close();

                if (sessionId != null)
                    replaceSessionId(doc);

                if (signed)
                {
                    // embedd the signature into original xml, add extra node
                    // payload signing is posted to Process.ashx, specific xml
                    // embedded with original xml
                    doc = signXmlDocumentWithPayload(doc);
                    //doc = signXmlDocumentEmbedded(doc);
                }

                HttpWebRequest request = GetRequest();
                Stream os = request.GetRequestStream();
                XmlTextWriter serviceWriter = new XmlTextWriter(os, Encoding.UTF8);
                serviceWriter.Formatting = Formatting.Indented;
                doc.WriteTo(serviceWriter);
                serviceWriter.Close();
                os.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogDebug($"response StatuseCode: {response.StatusCode}");
                    _logger.LogDebug($"response StatusDescription: {response.StatusDescription}");
                }
                //doc.Save("request.xml");

                return Deserialize(response.GetResponseStream(), typeResponse);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                //Login(_options.Username, _options.Password);
            }

            return null;
        }

        private object Deserialize(Stream s, Type expectedType)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(s);
            //doc.Save("response.xml");

            XmlNodeReader reader = new XmlNodeReader(doc.DocumentElement);

            if (doc.DocumentElement.Name == "LI_Villa")
            {
                XmlSerializer xsError = new XmlSerializer(Type.GetType("Landsbankinn.LI_Villa"));
                LI_Villa error = (LI_Villa)xsError.Deserialize(reader);
                throw new Exception(error.villa + " " + error.villubod);
            }

            XmlSerializer xs = new XmlSerializer(expectedType);
            return xs.Deserialize(reader);
        }

        private HttpWebRequest GetRequest()
        {
            if (_cookies == null)
                _cookies = new CookieContainer();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.UserAgent = "XMLClient 1.0";
            request.CookieContainer = _cookies;

            return request;
        }

        private bool replaceSessionId(XmlDocument xmlDoc, string sNodeName)
        {
            XmlNode node = xmlDoc.DocumentElement.SelectSingleNode(sNodeName);
            if (node == null)
                return false;

            node.InnerText = sessionId;
            return true;
        }

        private void replaceSessionId(XmlDocument xmlDoc)
        {
            if (!replaceSessionId(xmlDoc, "seta"))
                replaceSessionId(xmlDoc, "session_id");
        }

        private XmlElement createSignature(XmlDocument xmlDoc)
        {
            if (_certificate == null)
                throw new Exception("Error: No certificate has been selected!");

            // sign the xml document
            SignedXml signedXml = new SignedXml(xmlDoc);
            signedXml.SigningKey = _certificate.PrivateKey;
            Reference refs = new Reference("");
            refs.AddTransform(new XmlDsigC14NTransform());
            refs.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(refs);
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(_certificate));
            signedXml.KeyInfo = keyInfo;
            signedXml.ComputeSignature();

            return signedXml.GetXml();
        }

        private XmlDocument signXmlDocumentEmbedded(XmlDocument xmlDoc)
        {
            XmlElement sig = createSignature(xmlDoc);
            xmlDoc.DocumentElement.AppendChild(sig);
            return xmlDoc;
        }

        private XmlDocument signXmlDocumentWithPayload(XmlDocument xmlDoc)
        {
            XmlDocument signedXmlDoc = new XmlDocument();
            XmlElement signedXmlElement = signedXmlDoc.CreateElement("SignedPayload");
            XmlElement documentElement = signedXmlDoc.CreateElement("Payload");

            documentElement.AppendChild(signedXmlDoc.ImportNode(xmlDoc.DocumentElement, true));
            signedXmlElement.AppendChild(documentElement);
            signedXmlDoc.AppendChild(signedXmlElement);

            XmlNode documentNode = signedXmlDoc.ImportNode(createSignature(signedXmlDoc), true);
            signedXmlElement.AppendChild(documentNode);

            return signedXmlDoc;
        }
    }
}