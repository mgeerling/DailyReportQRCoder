using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Management;

//TODO get bitness 
//TODO create shortcut to file for 64bit folder? 
//TODO prompt to add a run as admin?: https://social.msdn.microsoft.com/Forums/windows/en-US/db6647a3-85ca-4dc4-b661-fbbd36bd561f/run-as-administrator-c?forum=winforms

namespace QRCoder1
{
    class Program
    {
        public static String getInstalledProducts(DateTime startDate)
        {
            string installedProducts = "";

            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_Product"); // TODO add a where clause 
            foreach (ManagementObject mo in mos.Get())
            {
                //List of all objects inside of a Management Object: https://msdn.microsoft.com/en-us/library/aa394378(v=vs.85).aspx
                String installDateString = (String)mo["InstallDate"];
                DateTime installDate = DateTime.ParseExact(installDateString, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

                //Compare date time against a time TODO CHANGE TO MIDNIGHT TODAY 
                if (DateTime.Compare(installDate, startDate) >= 0)
                { //if greater than 0, t1 is later than t2 -- if 0, then equal 
                    String productName = (String)mo["Name"];
                    String version = (String)mo["Version"];
                    String company = (String)mo["Vendor"];

                    //if the software vendor field contains OSIsoft, it's a PI Product -- therefore, print it 
                    if (company.Contains("OSIsoft"))
                    {
                        installedProducts = installedProducts + productName + ", " + version + "\r\n";
                    }
                }
            }
            return installedProducts;
        }
        public static void writeWorkHistory(String MachineName, String installedProducts)
        {
            //Writing files: https://msdn.microsoft.com/en-us/library/d62kzs03(v=vs.110).aspx
            string pihome = Environment.GetEnvironmentVariable("pihome");

            //test path //string path = @"c:\temp\work-history.txt";
            string path = @pihome + @"\OSI_FSE\work-history.txt";
            System.IO.Directory.CreateDirectory(@pihome + @"\OSI_FSE");

            //Generate text to be written to file 

            //TODO create other variables to be used -- maybe as flags? 
            String header = MachineName + " Site Name " + "Node Type " + "\r\n" + DateTime.Now + " FSE Name \r\n";
            String content = installedProducts;


            try
            {

                // Delete the file if it exists.
                if (File.Exists(path))
                {
                    // Note that no lock is put on the
                    // file and the possibility exists
                    // that another process could do
                    // something with it between
                    // the calls to Exists and Delete.
                    File.Delete(path);
                }

                // Create the file.
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(header + "\r\n" + content);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        static void Main(string[] args)
        {
            //Init variables
            Boolean runSummary = false;
            String installedProducts = "";

            //get machine name
            String machineName = Environment.MachineName;

            //getting operating system: http://stackoverflow.com/questions/577634/how-to-get-the-friendly-os-version-name
            var operatingSystem = (from x in new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                                   select x.GetPropertyValue("Caption")).FirstOrDefault();
            string OS = operatingSystem.ToString();

            //SAMPLE DATE TIME (FOR TESTING) //DateTime todayDate = new DateTime(2016, 3, 1, 0, 0, 0);
            DateTime startDate = DateTime.Today;


            //Check to see if we will run the summary report
            for (int i = 0; i < args.Length; i++)
            {
                if (String.Compare(args[i].ToLower(), "summary") == 0)
                {
                    runSummary = true;
                    try
                    {
                        String date = args[i + 1];
                        startDate = Convert.ToDateTime(date);
                    }
                    catch
                    {
                        Console.WriteLine("Wrong date dumbass");
                    }
                }
            }

            //if runSummary flag is set to false -- then we will generate the QR Code 
            if (runSummary == false)
            {
                //SAMPLE DATE TIME (FOR TESTING) //DateTime todayDate = new DateTime(2016, 3, 1, 0, 0, 0);
                installedProducts = getInstalledProducts(DateTime.Today);

                //build content to be encoded 
                String qrContent = machineName + " " + OS + "\r\n" + installedProducts;

                //try/catch to make sure the length of our string is shorter than max length of QR Code 
                //https://en.wikipedia.org/wiki/QR_code
                try
                {
                    //create QR Code as bitmap
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(20);

                    //create a path based on the current running user 
                    String username = Environment.UserName;
                    String path = String.Format("C:\\Users\\{0}\\Desktop\\qrcode.bmp", username);
                    //save bitmap
                    qrCodeImage.Save(path);
                    //open bitmap
                    Process.Start(@path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("This error is due to the length of the QR Code -- please use the summary report option to gather text for report");
                    Console.ReadLine();
                }
            }
            else //run summary report which will simply write the work-history.txt
            {
                installedProducts = getInstalledProducts(startDate);  
                writeWorkHistory(machineName, installedProducts);
            }
        }
    }

}
