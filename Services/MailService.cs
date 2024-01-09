using System.Net;
using System.Net.Mail;
using SendMail.Data;

namespace SendMail.Services
{
    public class MailService
    {
        public async Task SendAsync(string[] receives, string subject, string body)
        {
            // สร้าง mail message instance ขึ้นมา
            MailMessage msg = new MailMessage();

            // ผู้ส่งใส่ TQM New Core ไว้เพื่อตั้งชื่อที่หัวของอีเมล
            msg.From = new MailAddress($"TQM New Core {App.SENDER_EMAIL}");
            
            // เพื่มลงไปใน To เพื่อระบุว่าจะส่งให้ใครบ้าง
            foreach (string receive in receives)
            {
                msg.To.Add(receive);
            }

            msg.Subject = subject;
            msg.Body = body;

            // สร้าง smtp protocol
            using SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.Port = 587;

            // ใช้เป็น falase เพื่อเราจะใช้ credential ของตัวเอง
            smtpClient.UseDefaultCredentials = false;

            // สร้าง credential ของเรา
            smtpClient.Credentials = new NetworkCredential(App.SENDER_EMAIL, App.SENDER_PASSWORD);

            // สั่งให้เปิด ssl
            smtpClient.EnableSsl = true;

            try
            {
                // ส่ง
                await smtpClient.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}