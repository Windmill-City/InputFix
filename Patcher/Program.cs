using BsDiff;
using System;
using System.IO;
using System.Reflection;

namespace Patcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                //CreateSDVPatch();
                bool Patched = false;
                try
                {
                    Patched = PatchSDV();
                }
                catch (FileNotFoundException e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine("[Error]You need to put it in the game folder!");
                }
                if (Patched)
                {
                    Console.WriteLine("[Info]Creating dlls");
                    GenerateDlls();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine("[Error]Unknow Error!");
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void GenerateDlls()
        {
            FileStream file_TSF = new FileStream("TSF.dll", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file_TSF.Write(Resource1.TSF, 0, Resource1.TSF.Length);
            file_TSF.Close();
            FileStream file_0Harmony = new FileStream("0Harmony.dll", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file_0Harmony.Write(Resource1._0Harmony, 0, Resource1._0Harmony.Length);
            file_0Harmony.Close();
        }

        static void CreateSDVPatch()
        {
            Console.WriteLine("[Info]Creating patch file, please wait...");
            Stream stream = CreateSDVPatch("F:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Stardew Valley_Source.exe", "F:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Stardew Valley_InputFixed.exe");

            byte[] bytes = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, bytes.Length);

            FileStream file_patch = new FileStream("patch", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file_patch.Write(bytes, 0, bytes.Length);
            file_patch.Close();

            Console.WriteLine("[Info]Created patch file");
        }

        static bool PatchSDV()
        {
            Console.WriteLine("[Info]Patching, please wait...");

            var softwareVersion = Assembly.LoadFile(System.IO.Directory.GetCurrentDirectory() + "\\Stardew Valley.exe").GetName().Version.ToString();
            if(softwareVersion != "1.3.7346.34283")
            {
                Console.WriteLine("[Error]Your StardewValley version is " + softwareVersion);
                Console.WriteLine("[Error]This patch only for StardewValley 1.4.5 | 1.3.7346.34283");
                return false;
            }

            Stream stream = PatchSDV("Stardew Valley.exe", "patch");

            byte[] bytes = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, bytes.Length);

            FileStream file_patch = new FileStream("Stardew Valley_InputFixed.exe", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file_patch.Write(bytes, 0, bytes.Length);
            file_patch.Close();

            Console.WriteLine("[Info]Patch succeed");

            return true;
        }

        public static Stream CreateSDVPatch(string sdv_origin, string sdv_patched)
        {
            Stream output = new MemoryStream();
            BinaryPatchUtility.Create(File.ReadAllBytes(sdv_origin), File.ReadAllBytes(sdv_patched), output);
            return output;
        }

        public static Stream PatchSDV(string sdv_origin, string patch)
        {
            FileStream file_sdv_o = new FileStream(sdv_origin, FileMode.Open, FileAccess.Read);

            Stream output = new MemoryStream();
            //BinaryPatchUtility.Apply(file_sdv_o, () => new FileStream(patch, FileMode.Open, FileAccess.Read), output);
            BinaryPatchUtility.Apply(file_sdv_o, () => new MemoryStream(Resource1.patch), output);

            file_sdv_o.Close();

            return output;
        }
    }
}
