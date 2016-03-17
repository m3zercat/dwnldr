using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Humanizer;

namespace dwnldr
{
    public class Program
    {
        public static string Url = @"https://download.microsoft.com/download/5/8/9/589A8843-BA4D-4E63-BCB2-B2380E5556FD/vs2015.pro_enu.iso";
        public static String ExpectedSha1 = @"e01f364c3f21cdfcebb25d3c028398741f08eb24";
        //                               fciv ecd2da7c1c81ace418c7e9563c93123f
        public static String FileName = @"vs2015.pro_enu.iso";
        public static Int32 RewindBytes = 1024;

        public static void Main(String[] args)
        {
            FileSize Size = DetermineDownloadSize();
            FileSize BytesRead = DetermineBytesAlreadyDownloaded();

            ExpectedSha1 = ExpectedSha1.ToLowerInvariant();

            if (BytesRead.Equals(Size))
            {
                Console.WriteLine("File Download Already Complete!");
                ConfirmIntegrity();
                Console.ReadKey();
                return;
            }
            else
            {
                BytesRead = RewindChunk(BytesRead);
                Console.WriteLine("File Download at ");
                PrintOutput(BytesRead, Size, true);
                Console.WriteLine();
            }

            while (BytesRead < Size)
            {
                if (BytesRead > 0)
                {
                    Console.WriteLine();
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    Console.WriteLine("Continuing!");
                }

                WebResponse response = BuildHttpWebRequest(BytesRead, Size).GetResponse();

                ConfirmDownloadResumptionPossibleIfNecessary(response, BytesRead);

                //create network stream
                Stream responseStream = response.GetResponseStream();

                Byte[] buffer = new Byte[1024 * 8];
                try
                {
                    using (FileStream file = File.OpenWrite(FileName))
                    {
                        file.Seek(BytesRead, SeekOrigin.Begin);
                        while (file.CanWrite && BytesRead < Size)
                        {
                            Int32 count = responseStream.Read(buffer, 0, buffer.Length);
                            if (count > 0)
                            {
                                file.Write(buffer, 0, count);
                                file.Flush(true);
                                BytesRead += count;
                                PrintOutput(BytesRead, Size);
                            }
                        }
                        Console.WriteLine("Exited Download Loop!");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


                PrintOutput(BytesRead, Size, true);

                Console.WriteLine("Stopped!");
            }
            Console.WriteLine("Download Completed!");
            ConfirmIntegrity();
            Console.ReadKey();
        }

        private static void ConfirmDownloadResumptionPossibleIfNecessary(WebResponse response, FileSize BytesRead)
        {
            var acceptRanges = String.Compare(response.Headers["Accept-Ranges"], "bytes", StringComparison.OrdinalIgnoreCase) == 0;

            if (!acceptRanges && BytesRead > 0)
            {
                throw new Exception("Server doesn't support resuming file downloads!");
            }
        }

        private static HttpWebRequest BuildHttpWebRequest(FileSize BytesRead, FileSize Size)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(Url);

            if (BytesRead > 0)
            {
                request.AddRange(BytesRead.Size, Size);
            }
            return request;
        }

        private static FileSize RewindChunk(FileSize BytesRead)
        {
            BytesRead = BytesRead - RewindBytes;
            if (BytesRead < 0)
            {
                BytesRead = 0;
            }
            return BytesRead;
        }

        private static void ConfirmIntegrity()
        {
            Console.WriteLine("Confirming Integrity!");
            String sha1 = CalculateSha1();
            Console.WriteLine("File SHA1 is {0}", sha1);
            if (ExpectedSha1.Equals(sha1))
            {
                Console.WriteLine("Success!!!");
            }
            else
            {
                Console.WriteLine("Expected sha1 was {0}", ExpectedSha1);
                Console.WriteLine("FAIL!!!!!!");
            }
        }

        private static FileSize DetermineBytesAlreadyDownloaded()
        {
            FileSize result = 0;
            if (File.Exists(FileName))
            {
                FileInfo file = new FileInfo(FileName);
                result = file.Length;
            }
            return result;
        }

        private static FileSize DetermineDownloadSize()
        {
            FileSize Size;
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(Url);
            request.Method = WebRequestMethods.Http.Head;
            WebResponse response = request.GetResponse();
            Size = response.ContentLength;
            return Size;
        }

        private static DateTime _lastDisplayAt = DateTime.UtcNow;
        private static DateTime _displayAtSeconds = DateTime.UtcNow;

        private static FileSize _lastAt = 0;
        private static Decimal _rateMBPS;

        private static void PrintOutput(FileSize BytesRead, FileSize size, Boolean forceOutput = false)
        {
            if (_displayAtSeconds < DateTime.UtcNow || forceOutput)
            {
                _displayAtSeconds = DateTime.UtcNow.AddSeconds(3);
                Int64 sinceLast = BytesRead - _lastAt;
                TimeSpan time = _displayAtSeconds.Subtract(_lastDisplayAt);
                FileSize perSecond = (Int64) Math.Round(sinceLast/time.TotalSeconds);
                _lastDisplayAt = DateTime.UtcNow;
                _lastAt = BytesRead;
                Double estimatedRemainingTime = (Double.Parse("-1"));
                if (perSecond > 0)
                {
                    estimatedRemainingTime = ((Double)size - (Double)BytesRead)/(Double)perSecond;
                }
                Console.WriteLine("{0} Downloaded {1,10} of {2,10} at {3,7} per second. Estimated remaining {4}", DateTime.UtcNow,
                    BytesRead.ToString(SizeType.MegaBytes), size.ToString(SizeType.MegaBytes), perSecond.ToString(SizeType.MegaBytes),
                    TimeSpan.FromSeconds(estimatedRemainingTime).Humanize(2));
            }

        }

        private static String CalculateSha1()
        {
            using (FileStream fs = new FileStream(FileName, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    Byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2*hash.Length);
                    foreach (Byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }
                    return formatted.ToString().ToLowerInvariant();
                }
            }
        }
    }

}