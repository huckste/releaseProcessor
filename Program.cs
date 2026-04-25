using ReleaseProcessor;

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    await ReleaseApp.Run();
}
catch
{
    throw;
}
