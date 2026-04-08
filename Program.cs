using ReleaseProcessor;

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    await ReleaseApp.Run();
}
catch (Exception ex)
{
    throw new Exception($"Unexpected error occured: {ex.Message}");
}
