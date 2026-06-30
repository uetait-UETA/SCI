using System;
using System.Configuration;

class Program
{
    static int Main(string[] args)
    {
        string companyId = ConfigurationManager.AppSettings["CompanyId"];
        string sapDb     = ConfigurationManager.AppSettings["SapDb"];
        const string userApp = "BATCH";

        if (string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(sapDb))
        {
            Log("ERROR: CompanyId or SapDb not set in App.config.");
            return 2;
        }

        Log("Brand Replenishment starting — Company: {0} / SAP DB: {1}", companyId, sapDb);

        try
        {
            string errors = BrandReplenishment.Run(companyId, sapDb, userApp);

            if (errors == null)
            {
                Log("Completed successfully.");
                return 0;
            }
            else
            {
                Log("Completed with errors:");
                foreach (string line in errors.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    Log("  " + line);
                return 1;
            }
        }
        catch (Exception ex)
        {
            Log("FATAL: {0}", ex.Message);
            Log(ex.StackTrace);
            return 2;
        }
    }

    static void Log(string format, params object[] args)
    {
        string msg = args.Length > 0 ? string.Format(format, args) : format;
        string line = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg);
        Console.WriteLine(line);
    }
}
