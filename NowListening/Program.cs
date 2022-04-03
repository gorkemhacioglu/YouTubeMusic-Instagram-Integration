using System.Text;
using Newtonsoft.Json;
using NowListening.Data;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var loginCookiesPath =
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "loginCookies.json");

var nowListening = "";
var previousSong = "";

var chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

var driverService = ChromeDriverService.CreateDefaultService();
driverService.HideCommandPromptWindow = true;
var chromeOptions = new ChromeOptions {AcceptInsecureCertificates = true};
chromeOptions.BinaryLocation = chromePath;
Console.WriteLine("Initializing Instagram Browser... Please login if required...");
var driver = new ChromeDriver(driverService, chromeOptions);
driver.Url = "https://www.instagram.com/accounts/edit/";

var allCookies = GetCookie("cookieInstagram");

if (allCookies != null)
    foreach (var cookie in allCookies)
        driver.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value, cookie.Domain,
            cookie.Path, new DateTime(cookie.Expiry)));

driver.Navigate().Refresh();

chromeOptions.AddArgument("--remote-debugging-port=9222");
chromeOptions.AddArgument($"--user-data-dir=C:/Users/{Environment.UserName}/AppData/Local/Google/Chrome/User Data");

ChromeDriver driverYoutubeChecker = null;

try
{
    Console.WriteLine("Initializing Youtube Music Browser...");
    
    driverYoutubeChecker = new ChromeDriver(driverService, chromeOptions)
        {Url = "https://music.youtube.com/history"};

    allCookies = GetCookie("cookieYoutube");

    if (allCookies != null)
        foreach (var cookie in allCookies)
            driverYoutubeChecker.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value, cookie.Domain,
                cookie.Path, new DateTime(cookie.Expiry)));

    driverYoutubeChecker.Navigate().Refresh();
}
catch
{
    driverYoutubeChecker?.Close();
    driver.Close();
    Console.WriteLine("WARNING---------");
    Console.WriteLine("Close all Chrome tabs to run this app.");
    Console.WriteLine("----------------");
    Console.ReadLine();
}

Console.WriteLine("Working...");

while (true)
{
    try
    {
        var a = new Thread(UpdateLiveSong);
        a.Start();

        var b = new Thread(CheckCurrentSong);
        b.Start();
    }
    catch
    {
        //ignored
    }

    Console.ReadLine();
}

void CheckCurrentSong()
{
    while (true)
    {
        try
        {
            var cookie = driver.Manage().Cookies.GetCookieNamed("SSID");

            if (!string.IsNullOrEmpty(cookie?.Value))
                StoreCookie(new Tuple<string, List<Cookie>>("cookieYoutube",
                    new List<Cookie>(driver.Manage().Cookies.AllCookies)));

            IWebElement songName = null;

            try
            {
                songName = driverYoutubeChecker.FindElement(By.XPath(
                    "/html/body/ytmusic-app/ytmusic-app-layout/div[3]/ytmusic-browse-response/ytmusic-section-list-renderer/div[2]/ytmusic-shelf-renderer[1]/div[2]/ytmusic-responsive-list-item-renderer[1]/div[2]/div[1]/yt-formatted-string/a"));
            }
            catch
            {
                //ignored
            }
            
            IWebElement singer = null;

            try
            {
                singer = driverYoutubeChecker.FindElement(By.XPath(
                    "/html/body/ytmusic-app/ytmusic-app-layout/div[3]/ytmusic-browse-response/ytmusic-section-list-renderer/div[2]/ytmusic-shelf-renderer[1]/div[2]/ytmusic-responsive-list-item-renderer[1]/div[2]/div[3]/yt-formatted-string[1]/a"));
            }
            catch
            {
                //ignored
            }

            try
            {
                singer = driverYoutubeChecker.FindElement(By.XPath(
                    "/html/body/ytmusic-app/ytmusic-app-layout/div[3]/ytmusic-browse-response/ytmusic-section-list-renderer/div[2]/ytmusic-shelf-renderer[1]/div[2]/ytmusic-responsive-list-item-renderer[1]/div[2]/div[3]/yt-formatted-string[1]"));
            }
            catch
            {
                //ignored
            }
            
            if (!string.IsNullOrEmpty(songName?.Text) && !string.IsNullOrEmpty(singer?.Text))
            {
                Console.WriteLine(nowListening);

                nowListening = songName?.Text + " - " + singer?.Text.Replace(Environment.NewLine, " ");
            }
        }
        catch
        {
            //ignored
        }

        try
        {
            Thread.Sleep(10000);
            driverYoutubeChecker?.Navigate().Refresh();
        }
        catch
        {
            //ignored
        }
    }
}

void UpdateLiveSong()
{
    while (true)
    {
        try
        {
            var description =
                driver.FindElement(
                    By.XPath("/html/body/div[1]/section/main/div/article/form/div[4]/div/textarea"));

            if (description != null)
            {
                var cookie = driver.Manage().Cookies.GetCookieNamed("sessionid");

                if (!string.IsNullOrEmpty(cookie?.Value))
                    StoreCookie(new Tuple<string, List<Cookie>>("cookieInstagram",
                        new List<Cookie>(driver.Manage().Cookies.AllCookies)));

                previousSong = description.Text;

                if (previousSong == nowListening)
                {
                    continue;
                }
                Console.WriteLine("Setting " + nowListening + " on bio.");

                description.Clear();
                description.SendKeys(nowListening);

                var submit =
                    driver.FindElement(
                        By.XPath("/html/body/div[1]/section/main/div/article/form/div[10]/div/div/button"));

                submit.Click();
            }
        }
        catch
        {
            //ignored
        }

        Thread.Sleep(10000);
    }
}

void StoreCookie(Tuple<string, List<Cookie>> cookie)
{
    var myCookie = new List<MyCookie>();

    foreach (var item in cookie.Item2)
        if (item.Expiry != null)
            myCookie.Add(new MyCookie
            {
                Domain = item.Domain,
                Expiry = item.Expiry.Value.Ticks,
                HttpOnly = item.IsHttpOnly,
                Name = item.Name,
                Path = item.Path,
                Value = item.Value,
                Secure = item.Secure
            });
        else
            myCookie.Add(new MyCookie
            {
                Domain = item.Domain,
                Expiry = DateTime.MaxValue.Ticks,
                HttpOnly = item.IsHttpOnly,
                Name = item.Name,
                Path = item.Path,
                Value = item.Value,
                Secure = item.Secure
            });


    if (!File.Exists(loginCookiesPath))
    {
        var item = new Dictionary<string, List<MyCookie>> {{cookie.Item1, myCookie}};
        File.WriteAllText(loginCookiesPath, JsonConvert.SerializeObject(item), Encoding.UTF8);
        return;
    }

    var readCookiesJson = File.ReadAllText(loginCookiesPath);
    var readCookies =
        JsonConvert.DeserializeObject<Dictionary<string, List<MyCookie>>>(readCookiesJson);

    readCookies.TryGetValue(cookie.Item1, out var value);

    if (value?.Count > 0)
        readCookies[cookie.Item1] = myCookie;
    else
        readCookies.Add(cookie.Item1, myCookie);
    
    File.WriteAllText(loginCookiesPath, JsonConvert.SerializeObject(readCookies), Encoding.UTF8);
}

List<MyCookie> GetCookie(string username)
{
    if (!File.Exists(loginCookiesPath)) return new List<MyCookie>();

    var readCookiesJson = File.ReadAllText(loginCookiesPath);
    var readCookies =
        JsonConvert.DeserializeObject<Dictionary<string, List<MyCookie>>>(readCookiesJson);

    return readCookies.FirstOrDefault(x => x.Key == username).Value;
}