using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO; //Klasöre yükleme yapılacğı için.
using System.Data.SqlClient;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Data;
using WebApplicationDemo3.Models;

namespace WebApplicationDemo3.Controllers
{
    public class DocumentController : Controller
    {
        SqlConnection con = new SqlConnection();
        SqlCommand com = new SqlCommand();
        SqlDataReader dr;


        public void Connection()
        {
            con.ConnectionString = "data source=localhost\\SQLEXPRESS; database =Account; integrated security=SSPI ";
        }
        //Dosyaları kullanıcı bilgilerinden fakrlı bir veritabanında tuttuğum için bir başlantı daha ekledim.
        public void Connection2()
        {
            con.ConnectionString = "data source=localhost\\SQLEXPRESS; database =FixedDocument; integrated security=SSPI ";
        }

        // GET: Document
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Upload(HttpPostedFileBase postedFile)
        {
            if (postedFile != null)
            {
                string path = Server.MapPath("~/Uploads/"); //Dosyanın yükleneceği klasör.
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path); //Klasör yoksa oluştur.
                }

                string fileName = System.IO.Path.GetFileName(postedFile.FileName);
                string fileExtension = System.IO.Path.GetExtension(fileName);
                string savedFileName = $"{Guid.NewGuid()}{fileExtension}";
                postedFile.SaveAs(path + savedFileName);
                TempData["Url"] = path + savedFileName; //Hesaplacak pdf yi çağırabilmek için.
                TempData["donem"] = TempData["email"].ToString().Substring(1, 2);
                //Dosya yükleme başarılı mesajı.
                ViewBag.Message = "Dosya yükleme başarılı.";
                ViewBag.PdfPath = "/Uploads/" + savedFileName;

                //Veritabanına dosya adını ve uzantısını kaydetme işlemi.
                Connection(); //Veritabanı bağlantısını çağırdım.
                con.Open(); //Bağlantıyı açtım.
                //Veritabı ekleme işlemi
                com.Connection = con;
                com.CommandText = "insert into Document (FileName,FileUrl) values (@fileName,@filePath)";
                com.Parameters.AddWithValue("@fileName", fileName);
                com.Parameters.AddWithValue("@filePath", path + savedFileName);
                com.ExecuteNonQuery();
                con.Close();
                try
                {
                    string donem = TempData["email"].ToString().Substring(1, 2); //Kullanıcıın giriş yılına göre pdf gelecek.
                    TempData["GirisDonemi"] = donem; //l2121032078

                }
                catch (Exception)
                {
                    Response.Write("Yeniden giriş yaptıktan sonra işleminiz gerçekleşecek.");
                    return RedirectToAction("Login", "Account");
                }



                //Dosyanın id değerini dosyayı yükleyen kullanıcının DocumentId alanına kaydetme işlemi
                int documentId = 0; //Başlangıç değeri
                con.Open();
                com.CommandText = "select id from Document where FileName='" + fileName + "'";
                com.Connection = con;
                dr = com.ExecuteReader();
                if (dr.Read())
                {
                    documentId = dr.GetInt32(0); //iD DEĞERİNİ ALIR VE DEĞİŞKENE ATAR.
                                                 //Veritabanında sıfırncı indisteki değeri alır.
                                                 //Bu da son yüklenen 
                                                 //dosyanın id değeri olacaktır.
                    con.Close();
                }
                if (documentId != 0) //Dosya yükleme işlemi yapıldıysa. //Çalışıyor.
                {
                    con.Open();
                    com.Connection = con;
                    com.CommandText = "update UserAccounts Set DocumendId='" + documentId + "' where Email='" + TempData["email"] + "'";
                    //null durumda oldugu için Set ettik.
                    com.ExecuteNonQuery();
                    con.Close();
                }
                //Ekranda görünecek yer.
            }
            return View();
        }

        public ActionResult Calculate()
        {
            string gelenPdf = TempData["Url"].ToString();
            string donem = TempData["donem"].ToString();
            //Kıyaslanabilmeleri için dışarı da tanımladım.
            List<string> sabitListe = new List<string>();
            //Giriş Dönemine göre kendi yılına ait ders içeriği ile kıyaslanması işlemi için gerekli olan alan.
            if (donem == "21")
            {
                CheckLessons("2021", sabitListe);
            }
            else if (donem == "20")
            {
                CheckLessons("2017", sabitListe);
            }

            //Öğrenci transktiptinin alınıp okunması işlemi.
            List<string> sonuc = new List<string>();
            sonuc = pdfOkuma(gelenPdf);

            //Gelen Pdf dosyasındaki liste boş değilse kıyaslamayı yapar. 
            if (sabitListe != null)
            {
                List<string> eksikDersler = sabitListe.Except(sonuc).ToList();
                List<Ders> dersler = new List<Ders>(); //Eksik Dersleri bir bütün olarak tutacak olan liste.
                if (donem == "21")
                {
                    GetLessons(dersler, eksikDersler, "2021");
                }
                else if (donem == "20")
                {
                    GetLessons(dersler, eksikDersler, "2017");
                }
            }
            else
            {
                Debug.WriteLine("Dizi Boş");
            }
            return RedirectToAction("Information", "Document"); //Verilerin gösterileceği ActionResult'a ulaşmak için.
        }

        public void CheckLessons(string tabloAdi, List<string> sabitListe)
        {

            Connection2(); //Veritabanına Bağlanabilek İçin bağlantıyı Çağırdık.
            con.Close();
            con.Open();
            com.Connection = con;
            com.CommandText = "select DersKodu FROM [" + tabloAdi + "] ";
            dr = com.ExecuteReader();
            while (dr.Read())
            {
                sabitListe.Add(dr.GetValue(0).ToString()); //Sıfırıncı indisteki verileri alıp listeye ekleyecek.
            }
            con.Close();
            TempData["GomuluDizi"] = sabitListe;
        }
        //Veritabanından ders bilgilerini alma işlemi.
        public void GetLessons(List<Ders> dersler, List<string> eksikDersler, string tabloAdi)
        {
            Connection2();
            con.Open();
            com.Connection = con;
            foreach (var derskodu in eksikDersler)
            {
                com.CommandText = "select * from [" + tabloAdi + "] where DersKodu=@Derskodu";
                com.Parameters.Add("@Derskodu", SqlDbType.NVarChar).Value = derskodu;
                dr = com.ExecuteReader();
                while (dr.Read())
                {
                    dersler.Add(new Ders
                    {
                        DersKodu = dr.GetValue(0).ToString(),
                        DersAdi = dr.GetValue(1).ToString(),
                        Turu = dr.GetValue(2).ToString()
                    });
                }
                dr.Close();
                com.Parameters.Clear();
            }
            con.Close();
            TempData["Dersler"] = dersler;
        }
        public ActionResult Information()
        {
            List<Ders> ders = TempData["Dersler"] as List<Ders>;
            if (ders != null)
            {
                ViewBag.Dersler = ders;
            }

            return View();
        }
        //Pdf okuma işlmelerini yapacak olan methot.
        public List<string> pdfOkuma(string gelenPdf)
        {

            List<string> pdfTextLines = new List<string>();
            PdfReader reader = new PdfReader(gelenPdf);

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                string pageText = PdfTextExtractor.GetTextFromPage(reader, page, strategy);
                pdfTextLines.Add(pageText);
            }

            //Ders Kodlarını ayrıştırılma işlemi.
            List<string> sonuc = new List<string>();
            foreach (string s in pdfTextLines)
            {
                string[] kelimeler = s.Split(' ', ',');
                foreach (string kelime in kelimeler)
                {
                    if (Regex.IsMatch(kelime, @"[A-Z]{3}-\d{3}") || Regex.IsMatch(kelime, @"[A-Z]{3}-\d{4}"))
                    {
                        sonuc.Add(kelime);
                    }
                }
            }

            reader.Close();
            return sonuc;

        }
    }
}
