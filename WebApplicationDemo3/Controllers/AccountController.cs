using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using WebApplicationDemo3.Models; //Models klasörü içindeki Account classına erişim alabilmek için tanımladık.
using System.Web.UI;

namespace WebApplicationDemo3.Controllers
{
    public class AccountController : Controller
    {
        SqlConnection con = new SqlConnection();
        SqlCommand com = new SqlCommand();
        SqlDataReader dr;

        // GET: Account
        [HttpGet] //Veri gönderdiği için
        public ActionResult Login()
        {
            return View();
        }
        void connectionString()
        {
            con.ConnectionString = "data source=localhost\\SQLEXPRESS; database =Account; integrated security=SSPI ";
        }
        [HttpPost] //Veri geldiği için 
        public ActionResult Verify(Account account)
        {
            connectionString();
            con.Open();
            com.CommandText = "select * from UserAccounts where Email= '" + account.Email + "' and Password = '" + account.Password + "'";
            com.Connection = con;
            dr = com.ExecuteReader();
            if (dr.Read()) //Veritabanında kayıt varsa.
            {
                con.Close();
                dr.Close();
                TempData["email"] = account.Email; //Diğer tarafa(HomePage) Email değerini göndermek için.
                
                return RedirectToAction("Upload", "Document");  //Başka bir controller içindeki Action Methodu çağırma işlemi

                // return View("Create"); //Giriş başarılı ise (test için)
            }
            else //Veritabanında kayıt yoksa.
            {
                con.Close();
                ViewBag.Message = "Kullanıcı adı ya da şifre hatalı.";
                dr.Close();
            }
            return View("Login");
        }
        public ActionResult SingIn() //Üye ol ekranını açar.
        {
            return View("SingIn");
        }

        [HttpPost]
        public ActionResult VerifySingIn(SingIn singin)
        {
            connectionString();
            con.Open();
            com.Connection = con;
            com.CommandText = "select * from UserAccounts where Email='" + singin.Email + "'";
            dr = com.ExecuteReader();
            if (dr.Read())
            {
                con.Close();         
                ViewBag.Message = "Girilen mail adresine sahip kullanıcı zaten kayıtlı.";
                dr.Close();
                return View("SingIn");
            }
            else
            {
                com.CommandText = "insert into UserAccounts (Email,Name,Surname,Password) values " +
                    "('" + singin.Email + "','" + singin.Name + "','" + singin.Surname + "','" + singin.Password + "')";
                dr.Close();
                com.ExecuteReader();
                con.Close();              
                ViewBag.Message1 = "Kayıt işlemi başarılı Giriş Yaparak Devam Edebilirsiniz."; 
                return View("Login");
            }
        }

    }

}
