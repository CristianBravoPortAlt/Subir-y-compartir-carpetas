using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace NetUser;

class Program
{
    const string PASSWORD = "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4";
    const string CLAVEDES = "12345678";

    struct Credenciales
    {
        public string User { get; set; }
        public string Ruta { get; set; }
        public string RutaCompartida { get; set; }
        public string UserCompartida { get; set; }
        public string PasswordCompartida { get; set; }
    }

    static void CrearYCompartirDirectorio(Credenciales credenciales)
    {
        if (!Directory.Exists(Path.Combine(credenciales.Ruta)))
        {
            Directory.CreateDirectory(Path.Combine(credenciales.Ruta));
        }
        //DirectoryInfo ruta = new("/Compartida");
        EjecutarComando(@$"net share Compartida={credenciales.Ruta} /grant:{credenciales.User},FULL");

        EjecutarComando($"icacls \"{credenciales.Ruta}\" /grant red:(OI)(CI)F /T");
    }

    static void UsarCarpetaEnRed(Credenciales credenciales)
    {
        string rutaCompartida = @"\\desktop-2ds7h38\RedCompartida";
        if (Directory.Exists(rutaCompartida))
        {
            EjecutarComando(@$"net use Z: {rutaCompartida} /user:{credenciales.User} {credenciales.PasswordCompartida}");
            try
            {
                File.Create(Path.Combine(rutaCompartida, "hola.txt"));
                Console.WriteLine("EL arhivo ha sido creado y la conexión ha sido exitosa");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ha ocurrido el siguiente error al crear el archivo: {e.Message}");
            }
            //string[] directorios = Directory.GetDirectories(@"\\DESKTOP-NI88EGD\Compartida");
            //string[] ficheros = Directory.GetFiles(@"\\DESKTOP-NI88EGD\Compartida");
        }
    }
    static void EjecutarComando(string comando)
    {
        Process proceso = new();
        ProcessStartInfo cmd = new()
        {
            FileName = "cmd.exe",
            Arguments = "/C" + comando
        };
        proceso.StartInfo = cmd;
        proceso.Start();
        proceso.WaitForExit();
    }
    static bool VerificarContraseña()
    {
        string? password;
        do
        {
            Console.Write("Escribe la contraseña: ");
            password = Console.ReadLine();
        } while (string.IsNullOrEmpty(password));
        var passwEncriptada = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(passwEncriptada);
        password = Convert.ToHexString(hash);
        return password == PASSWORD;
    }

    static void EncriptarJson()
    {
        var contenido = File.ReadAllText("credenciales.json");
        byte[] contenidoBytes = Encoding.UTF8.GetBytes(contenido);

        using (DES des = DES.Create())
        {
            des.Key = Encoding.UTF8.GetBytes(CLAVEDES);
            des.IV = Encoding.UTF8.GetBytes(CLAVEDES);

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(contenidoBytes, 0, contenidoBytes.Length);
            cs.FlushFinalBlock();

            string contenidoEncriptado = Convert.ToBase64String(ms.ToArray());
            File.WriteAllText("credenciales.json", contenidoEncriptado);
        }
    }

    static void DesencriptarJson()
    {
        var contenidoEncriptado = File.ReadAllText("credenciales.json");
        byte[] contenidoBytes = Convert.FromBase64String(contenidoEncriptado);

        using (DES des = DES.Create())
        {
            des.Key = Encoding.UTF8.GetBytes(CLAVEDES);
            des.IV = Encoding.UTF8.GetBytes(CLAVEDES);

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(contenidoBytes, 0, contenidoBytes.Length);
                cs.FlushFinalBlock();

                string contenido = Encoding.UTF8.GetString(ms.ToArray());
                File.WriteAllText("credenciales.json", contenido);
            }
        }
    }
    static void LlamadaMetodos(string op)
    {
        if (VerificarContraseña())
            {
                DesencriptarJson();
                string json = File.ReadAllText("credenciales.json");
                Credenciales credenciales = JsonSerializer.Deserialize<Credenciales>(json);

                switch (op)
                {
                    case "1":
                        CrearYCompartirDirectorio(credenciales);
                        break;
                    case "2":
                        UsarCarpetaEnRed(credenciales);
                        break;
                    case "3":
                        CrearYCompartirDirectorio(credenciales);
                        UsarCarpetaEnRed(credenciales);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Contraseña incorrecta");
            }
    }
    static void Main(string[] args)
    {
        string op = string.Empty;
        if (args.Length == 0 || args.Length > 2)
            Console.Write("debes poner 1 o 2 parámetros\n\t1 - Crear y compartir directorio\n\t2 - Usar carpeta en red\n\t3 - Ambas opciones");
        else if (args.Length == 1)
            op = args[0];
        else if (args.Length == 2)
        {
            int op1, op2;
            if (int.TryParse(args[0], out op1) && int.TryParse(args[1], out op2))
                op = (op1 + op2).ToString();
        }

        if (args.Length != 0 && args.Length <= 2)
        {
            try
            {
                LlamadaMetodos(op);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ha ocurrido el siguiente error: {e.Message}");
            }
            finally
            {
                EncriptarJson();
            }
        }
    }
}
