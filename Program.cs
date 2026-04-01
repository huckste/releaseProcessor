using ReleaseProcessor;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var app = new ReleaseApp();

await ReleaseApp.Run();
