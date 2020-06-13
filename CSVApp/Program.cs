using ServiceStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using MathNet.Numerics.Statistics;
using System.Net.Mime;

namespace CsvApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //Application is build to be working with Gmail. 
            //The Gmail Mailbox that is sending the email should have the "Less Secure Apps" option enabled from the Mailbox settings
            //Reference: https://support.google.com/accounts/answer/6010255?hl=en

            Console.WriteLine("Please input the full path to the CSV file:");
            string fileToPath = Console.ReadLine();
            Console.WriteLine("From Email Address:");
            string emailToSendFrom = Console.ReadLine();
            Console.WriteLine("Please input your email address password");
            string password = Console.ReadLine();
            Console.WriteLine("To Email Address:");
            string emailToSendTo = Console.ReadLine();

            var sortedData = SortFile(fileToPath);
            if (sortedData != null)
            {
                SendMail(emailToSendFrom, password, emailToSendTo, new MemoryStream(Encoding.ASCII.GetBytes(sortedData)));
            }
            else
            {
                Console.WriteLine("File path is not correct or file does not exist!");
            }
            
        }

        static string SortFile(string filePath)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);

                string headers = "Country" +
                ";" + "AverageScore" +
                ";" + "MedianScore" +
                ";" + "MaxScore" +
                ";" + "MaxScorePerson" +
                ";" + "MinScore" +
                ";" + "MinScorePerson" +
                ";" + "RecordsCount";

                var sb = new StringBuilder();
                List<Country> countries = new List<Country>();


                foreach (var line in lines.Skip(1))
                {
                    var colums = line.Split(";");

                    Country country;

                    if (!countries.Any(c => c.CountryName == colums[2]))
                    {
                        country = new Country()
                        {
                            CountryName = colums[2],
                        };

                        countries.Add(country);
                    }
                    else
                    {
                        country = countries.Where(n => n.CountryName == colums[2]).FirstOrDefault();
                    }

                    var person = new Person
                    {
                        FirstName = colums[0],
                        LastName = colums[1],
                        Score = double.Parse(colums[4]),
                    };

                    country.People.Add(person);
                }

                var sorted = countries.OrderByDescending(p => p.AverageScore);

                sb.AppendLine(headers);

                foreach (var row in countries)
                {
                    sb.AppendLine(row.ToString());
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public class Country
        {
            private double score => this.People.Sum(p => p.Score);

            public Country()
            {
                People = new List<Person>();
            }
            public string CountryName { get; set; }

            public List<Person> People { get; set; }

            public double AverageScore => score / People.Count;
            public double MedianScore => this.People.Select(c => c.Score).ToList().Median();
            public double MaxScore => this.People.Max(p => p.Score);
            public string MaxScorePerson => this.People.Where(p => p.Score == MaxScore).Select(p => p.FirstName + " " + p.LastName).FirstOrDefault();
            public double MinScore => this.People.Min(p => p.Score);
            public string MinScorePerson => this.People.Where(p => p.Score == MinScore).Select(p => p.FirstName + " " + p.LastName).FirstOrDefault();
            public int RecordsCount => People.Count;
            public override string ToString()
            {
                return string.Join("; ", this.CountryName , this.AverageScore , this.MedianScore , this.MaxScore , this.MaxScorePerson
                    , this.MinScore , this.MinScorePerson , this.RecordsCount);
            }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public double Score { get; set; }
        }
        static void SendMail(string emailToSendFrom, string password, string emailToSendTo, MemoryStream stream)
        {
            try
            {
                MailMessage message = new MailMessage(emailToSendFrom, emailToSendTo);
                message.Subject = "Sending email with attached CVS!";
                message.Body = "Sending email with attached CVS!";
                message.IsBodyHtml = false;

                using (stream)
                {
                    Attachment attachment = new Attachment(stream, new ContentType("text/csv"));
                    attachment.Name = "ReportByCountry.csv";
                    message.Attachments.Add(attachment);

                    using (var smtp = new SmtpClient())
                    {
                        NetworkCredential loginInfo = new NetworkCredential(emailToSendFrom, password);
                        smtp.Host = "smtp.gmail.com";
                        smtp.Port = 587;
                        smtp.EnableSsl = true;
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = loginInfo;
                        smtp.Send(message);
                        Console.WriteLine("Message send successfully!");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Email Input format is not correct!");
                return;
            }
        }
    }
}
