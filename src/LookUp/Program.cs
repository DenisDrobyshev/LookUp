using LookUp.Ocr;

namespace LookUp;

internal static class Program
{
    private const string MutexName = "LookUp_SingleInstance_{9F1C4A6E-4A2B-4E4E-9C3E-7B2A1D5F8C10}";

    [STAThread]
    private static int Main(string[] args)
    {
        // Hidden headless mode used for CI / local sanity checks of the OCR pipeline.
        if (args.Length > 0 && args[0].Equals("--selftest", StringComparison.OrdinalIgnoreCase))
        {
            string? outFile = args.Length > 1 ? args[1] : null;
            return SelfTest.Run(outFile).GetAwaiter().GetResult();
        }

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // LookUp is already running in the tray. Nothing to do.
            return 0;
        }

        ApplicationConfiguration.Initialize();
        using var context = new TrayApplicationContext();
        Application.Run(context);
        return 0;
    }
}
