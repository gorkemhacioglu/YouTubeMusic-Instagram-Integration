// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

string loginCookiesPath =
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "loginCookies.json");

var driverService = ChromeDriverService.CreateDefaultService();
driverService.HideCommandPromptWindow = true;


var chromeOptions = new ChromeOptions { AcceptInsecureCertificates = true };

chromeOptions.AddExcludedArgument("enable-automation");

chromeOptions.AddAdditionalOption("useAutomationExtension", false);

chromeOptions.PageLoadStrategy = PageLoadStrategy.Default;

var driver = new ChromeDriver(driverService, chromeOptions)
    { Url = "https://www.instagram.com/iamhacioglu/" };

var allCookies = GetCookie("iamhacioglu");

foreach (var cookie in allCookies)
{
    driver.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value, cookie.Domain,
        cookie.Path, new DateTime(ticks: cookie.Expiry)));
}
driver.Navigate().Refresh();

while (true)
{
    try
    {
        IWebElement profileIcon = null;
        IWebElement profileEdit = null;
        try
        {
            profileIcon =
                driver.FindElement(
                    By.XPath("/html/body/div[1]/section/main/section/div[3]/div[1]/div/div/div[2]/div[1]/div/div/a"));
        }
        catch (Exception e)
        {
            profileEdit =
                driver.FindElement(By.XPath("/html/body/div[1]/section/main/div/header/section/div[1]/div[1]/div/a"));
        }
        
        if (profileIcon != null || profileEdit != null)
        {
            var cookie = driver.Manage().Cookies.GetCookieNamed("sessionid");

            if (!string.IsNullOrEmpty(cookie?.Value))
            {
                StoreCookie(new Tuple<string, List<Cookie>>("iamhacioglu",
                    new List<Cookie>(driver.Manage().Cookies.AllCookies)));
            }
            
            profileIcon?.Click();

            Thread.Sleep(5000);
            profileEdit =
                driver.FindElement(By.XPath("/html/body/div[1]/section/main/div/header/section/div[1]/div[1]/div/a"));

            if (profileEdit != null)
            {
                profileEdit.Click();
                Thread.Sleep(5000);

                while (true)
                {
                    var description =
                        driver.FindElement(
                            By.XPath("/html/body/div[1]/section/main/div/article/form/div[4]/div/textarea"));

                    if (description != null)
                    {
                        var currentText = description.Text;

                        description.Clear();
                        description.SendKeys("asdasdas");
                        break;
                    }
                }

                break;
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

void StoreCookie(Tuple<string, List<Cookie>> cookie)
{
    var myCookie = new List<MyCookie>();

    foreach (var item in cookie.Item2)
    {
        if (item.Expiry != null)
            myCookie.Add(new MyCookie()
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
        {
            myCookie.Add(new MyCookie()
            {
                Domain = item.Domain,
                Expiry = DateTime.MaxValue.Ticks,
                HttpOnly = item.IsHttpOnly,
                Name = item.Name,
                Path = item.Path,
                Value = item.Value,
                Secure = item.Secure
            });
        }
    }


    if (!File.Exists(loginCookiesPath))
    {
        var item = new Dictionary<string, List<MyCookie>> { { cookie.Item1, myCookie } };
        File.WriteAllText(loginCookiesPath, Newtonsoft.Json.JsonConvert.SerializeObject(item), Encoding.UTF8);
        return;
    }

    string readCookiesJson = File.ReadAllText(loginCookiesPath);
    var readCookies =
        Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<MyCookie>>>(readCookiesJson);

    readCookies.TryGetValue(cookie.Item1, out var value);

    if (value?.Count > 0)
    {
        readCookies[cookie.Item1] = myCookie;
    }
    else
        readCookies.Add(cookie.Item1, myCookie);


    File.WriteAllText(loginCookiesPath, Newtonsoft.Json.JsonConvert.SerializeObject(readCookies), Encoding.UTF8);
}

List<MyCookie> GetCookie(string username)
{
    if (!File.Exists(loginCookiesPath))
    {
        return new List<MyCookie>();
    }

    string readCookiesJson = File.ReadAllText(loginCookiesPath);
    var readCookies =
        Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<MyCookie>>>(readCookiesJson);

    return readCookies.FirstOrDefault(x => x.Key == username).Value;
}

public class MyCookie
{
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public long Expiry { get; set; }
}