using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BAOCAO_369.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(bool success, string message)> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentName, string ccEmail = null)
        {
            try
            {
                var smtpConfig = _configuration.GetSection("SmtpSettings");
                var server = smtpConfig["Server"];
                var port = int.Parse(smtpConfig["Port"] ?? "587");
                var senderEmail = smtpConfig["SenderEmail"];
                var senderName = smtpConfig["SenderName"];
                var password = smtpConfig["Password"];
                var enableSsl = bool.Parse(smtpConfig["EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(senderEmail))
                {
                    return (false, "Chưa cấu hình thông tin SMTP Server.");
                }

                if (string.IsNullOrEmpty(toEmail))
                {
                    return (false, "Đơn vị chưa được cấu hình địa chỉ Email.");
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(senderEmail, senderName);
                    message.To.Add(new MailAddress(toEmail));
                    if (!string.IsNullOrWhiteSpace(ccEmail))
                    {
                        var ccList = ccEmail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var cc in ccList)
                        {
                            message.CC.Add(new MailAddress(cc.Trim()));
                        }
                    }
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    if (attachmentBytes != null && attachmentBytes.Length > 0)
                    {
                        var stream = new MemoryStream(attachmentBytes);
                        var attachment = new Attachment(stream, attachmentName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                        message.Attachments.Add(attachment);
                    }

                    using (var client = new SmtpClient(server, port))
                    {
                        client.EnableSsl = enableSsl;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(smtpConfig["Username"] ?? senderEmail, password);

                        await client.SendMailAsync(message);
                    }
                }

                return (true, "Gửi email thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi gửi email: {ex.Message}");
            }
        }
    }
}
