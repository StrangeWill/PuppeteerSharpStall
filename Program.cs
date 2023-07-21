
using System.Diagnostics;
using PuppeteerSharp;
using Flurl.Http;

var stopwatch = new Stopwatch();
stopwatch.Start();
var fetcher = new BrowserFetcher();
await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

//await fetcher.DownloadAsync("1115057");
//var path = fetcher.GetExecutablePath("1115057");
Console.WriteLine($"Downloaded: {stopwatch.ElapsedMilliseconds}");

var browserContext = await Puppeteer.LaunchAsync(new()
{
    Headless = true,
    //ExecutablePath = path,
    Args = new[] { "--no-sandbox", "--no-zygote" },
    EnqueueAsyncMessages = false,
});
Console.WriteLine($"New context: {stopwatch.ElapsedMilliseconds}");

var url = "https://legend-bracelet.myshopify.com/collections/legend-hats?view=hats-1688039304264&slScreenshot=true";
var page = await browserContext.NewPageAsync();
Console.WriteLine($"New page: {stopwatch.ElapsedMilliseconds}");

page.Request += (object sender, RequestEventArgs e) =>
{
    Console.WriteLine($"{e.Request.Url}: {stopwatch.ElapsedMilliseconds}");
};

await page.SetViewportAsync(new ViewPortOptions { Height = 1050, Width = 1400 });
//await page.SetViewportAsync(new ViewPortOptions { Height = 720, Width = 991 });
await page.SetRequestInterceptionAsync(true);


page.Request += async (_, args) =>
{
    switch (args.Request.ResourceType)
    {
        case ResourceType.StyleSheet:
            var response = await args.Request.Url.GetAsync();
            var text = await response.GetStringAsync();
            // Replacing the first 9 occurrences will also fix it
            //var text = new Regex("992px").Replace(text, "", 9);
            await args.Request.RespondAsync(new ResponseData
            {
                // Will stall
                //Body = text,

                // Will not stall!
                Body = text.Replace("column-count: 3;", "column-count: 4;"),
                Status = (System.Net.HttpStatusCode)response.StatusCode
            });
            break;
        default:
            await args.Request.ContinueAsync();
            return;
    }
};

Console.WriteLine($"Set viewport: {stopwatch.ElapsedMilliseconds}");

try
{
    await page.GoToAsync(url, new NavigationOptions
    {
        Timeout = 5000,
        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
    });
}
catch (NavigationException e)
{
    Console.WriteLine($"{e.Message}");
}

Console.WriteLine($"Navigated: {stopwatch.ElapsedMilliseconds}");

await page.ScreenshotAsync("./output.png");
Console.WriteLine($"Done: {stopwatch.ElapsedMilliseconds}");