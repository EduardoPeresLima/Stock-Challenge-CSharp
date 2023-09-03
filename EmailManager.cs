using System;
using System.IO;
using Newtonsoft.Json;
using MimeKit;
using MailKit.Net.Smtp;

namespace stock_monitoring
{
    internal class EmailManager
    {

        private EmailSenderConfiguration emailConfig;
        public EmailTemplates emailTemplates;
        public EmailManager()
        {
            //Get email configurations
            string emailConfigJSON = File.ReadAllText(@"C:\Users\Pedro\OneDrive\Área de Trabalho\Github\email-configuration.json");
            this.emailConfig = JsonConvert.DeserializeObject<EmailSenderConfiguration>(emailConfigJSON);
            //Get email templates
            string emailTemplatesJSON = File.ReadAllText(@"C:\Users\Pedro\OneDrive\Área de Trabalho\Github\email-templates.json");
            this.emailTemplates = JsonConvert.DeserializeObject<EmailTemplates>(emailTemplatesJSON);
        }
        public Exception SendEmail(EmailTemplate emailTemplate, string stock, float currentValue, float valueLimit)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(emailConfig.senderName, emailConfig.senderEmail));
            foreach (Emailrecipient recipient in emailConfig.emailRecipients)
            {
                email.To.Add(new MailboxAddress(recipient.name, recipient.email));
            }

            email.Subject = string.Format(emailTemplate.title, stock);
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = string.Format(emailTemplate.body, stock, currentValue, valueLimit)
            };
            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Connect(emailConfig.serverSMTP, emailConfig.serverPORT, false);
                    smtp.Authenticate(emailConfig.senderEmail, emailConfig.senderAppPassword);
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }
                catch (Exception e)
                {
                    return e;
                }
            }
            return null;
        }
        //Classes for Email Configuration. Read fromm a JSON Configuration File
        private class EmailSenderConfiguration
        {
            public string serverSMTP { get; set; }
            public int serverPORT { get; set; }
            public string senderName { get; set; }
            public string senderEmail { get; set; }
            public string senderAppPassword { get; set; }
            public Emailrecipient[] emailRecipients { get; set; }
        }

        private class Emailrecipient
        {
            public string name { get; set; }
            public string email { get; set; }
        }

        //Classes for email template. Templates are read from a JSON File
        public class EmailTemplates
        {
            public EmailTemplate sellAlert { get; set; }
            public EmailTemplate buyAlert { get; set; }
        }

        public class EmailTemplate
        {
            public string title { get; set; }
            public string body { get; set; }
        }
    }
}
