using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace XrayGameListGenerator
{
    public partial class XrayGameListFileGenerator : Form
    {
        public XrayGameListFileGenerator()
        {
            InitializeComponent();
        }

        private void Init()
        {
            string folderPath = @"files"; 

            string baseUrl = textBoxURL.Text;

            string outputFile = $"{textBoxFileOut.Text}.txt";


            string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

            using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                writer.WriteLine("[main]");
                writer.WriteLine("files_count=" + files.Length);
                writer.WriteLine();

                for (int i = 0; i < files.Length; i++)
                {
                    string fileFullPath = files[i];
                    string relativePath = GetRelativePath(folderPath, fileFullPath);
                    long size = new FileInfo(fileFullPath).Length;
                    string crc32 = ComputeCRC32(fileFullPath);
                    string url = baseUrl + relativePath.Replace("\\", "/");

                    writer.WriteLine("[file_" + i + "]");
                    writer.WriteLine("path = " + textBoxPathMods.Text + relativePath);
                    writer.WriteLine("url = " + url);
                    writer.WriteLine("size = " + size);
                    writer.WriteLine("crc32 = " + crc32);
                    writer.WriteLine();
                }
            }
            MessageBox.Show("Обработка файлов завершена. Файл находится рядом с программой" + outputFile, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            basePath = Path.GetFullPath(basePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }
            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }


        static string ComputeCRC32(string filePath)
        {
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                var crc32 = new XrayCrc32();
                byte[] hash = crc32.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Init();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Разработано командой: Team-stalker\n\n\rПрограмма предназначена для автоматического формирования файла gamelist.txt.\n\n\rФайлы мода необходимо поместить в папку files. После чего, будет выполнена обработка.");
        }
    }

    public class XrayCrc32 : HashAlgorithm
    {
        public override int HashSize => 32;

        public const uint DefaultPolynomial = 0xEDB88320u;
        public const uint DefaultSeed = 0xFFFFFFFFu;

        private uint hash;
        private uint seed;
        private uint[] table;

        public XrayCrc32()
        {
            table = InitializeTable(DefaultPolynomial);
            seed = DefaultSeed;
            Initialize();
        }

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = BitConverter.GetBytes(~hash);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(hashBuffer);
            return hashBuffer;
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;
                }
                table[i] = entry;
            }
            return table;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int length)
        {
            uint crc = seed;
            for (int i = start; i < start + length; i++)
            {
                crc = (crc >> 8) ^ table[buffer[i] ^ (crc & 0xFF)];
            }
            return crc;
        }
    }
}
