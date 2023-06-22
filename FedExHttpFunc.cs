using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Selenium.WebDriver.UndetectedChromeDriver;
using SeleniumExtras.WaitHelpers;
using System.Threading;
using WindowsInput;
using System.Collections.ObjectModel;
// using MyWebDriverWait = OpenQA.Selenium.Support.UI.WebDriverWait;
using System.Linq;


namespace FedExWebScraper
{

    public static class FedExHttpFunc
    {
        private static readonly string LogFormat = "{0} {1} {2}";

        [FunctionName("FedExHttpFunc")]
        public static async Task<IActionResult> RunProgram(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                List<string> fedexList = new List<string>();
                // fedexList.Add("{\"username\":\"falkosj\",\"password\":\"!Af98r20i\"}");
                // fedexList.Add("{\"username\":\"fiveshipping\",\"password\":\"F1veincsh1p\"}");
                fedexList.Add("{\"username\":\"mstraley\",\"password\":\"Springfield20!\"}");

                // fedexList=[{"username":"KNSINTL","password":"Hawaii50"},{"username":"fiveshipping","password":"F1veincsh1p"}]
                foreach (var user in fedexList)
                {
                    dynamic userObject = JsonConvert.DeserializeObject(user.Trim());
                    string username = userObject.username;
                    string password = userObject.password;
                    await Run(username, password, log);
                }
            }
            catch (Exception Ex)
            {
                log.LogError($"An error occurred while account login. {Ex.Message}", LogLevel.Error);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult("Process completed!!");
        }

        public static async Task<IActionResult> Run(string username, string password, ILogger log)
        {
            // string username = "fiveshipping";
            // string password = "F1veincsh1p";
            // string username = "falkosj";
            // string password = "!Af98r20i";
            string screenshotPath = @"E:\Office\Screenshots\";
            //logger initialization
            string logFolderPath = Path.Combine(@"E:\Office\FedExLogs", "logs", DateTime.Today.ToString("yyyy_MM_dd"), username);
            Directory.CreateDirectory(logFolderPath);
            string logFilePath = Path.Combine(logFolderPath, "log.txt");
            StreamWriter logWriter = new StreamWriter(logFilePath, append: true);
            logWriter.AutoFlush = true;
            Logger logger = new Logger(logWriter, LogLevel.Debug);

            log.LogInformation("Scraper setup for fedEx !!");
            try
            {
                List<string> completedDownload = new List<string>(); // Define and initialize the completedDownload list 
                using (var driver = UndetectedChromeDriver.Instance())
                {
                    driver.DriverArguments.Add("--no-sandbox");
                    driver.DriverArguments.Add("enable-automation");
                    driver.DriverArguments.Add("--disable-dev-shm-usage");
                    driver.DriverArguments.Add("--disable-gpu");
                    driver.DriverArguments.Add("--disable-extensions");
                    // driver.DriverArguments.Add("--headless=new");
                    driver.DriverArguments.Add("--disable-infobars");
                    driver.DriverArguments.Add("--disable-notifications");
                    driver.DriverArguments.Add("--disable-browser-side-navigation");
                    driver.DriverArguments.Add("--disable-popup-blocking");
                    driver.DriverArguments.Add("--ignore-certificate-errors");
                    driver.DriverArguments.Add("--mute-audio");
                    driver.DriverArguments.Add("--privileged");
                    driver.DriverArguments.Add("--network=host");
                    driver.Manage().Window.Maximize();
                    Console.WriteLine("Window Maximized");
                    driver.Navigate().GoToUrl("https://www.fedex.com/en-us/home.html");
                    Console.WriteLine("URL Searched");

                    //SCREENSHOT
                    var FedexPage = ((ITakesScreenshot)driver).GetScreenshot();
                    FedexPage.SaveAsFile(screenshotPath + "FedexPage.png", ScreenshotImageFormat.Png);
                    System.Console.WriteLine("took screenshot of Fedex page !");

                    string name = "";
                    string inv_num = "";
                    int counter = 0;
                    string methodname = "";
                    await PerformLogin(driver, username, password, screenshotPath, logger, name, inv_num, completedDownload, counter, methodname);
                    logger.Log($"Scraping Done !!!", LogLevel.Info);
                    driver.Quit();
                    return new OkObjectResult("Scraping Done!");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"An error occurred during scraping. {ex.Message}", LogLevel.Error);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task PerformLogin(IWebDriver driver, string username, string password, string screenshotPath, Logger logger, string name, string inv_num, List<string> completedDownload, int counter, string methodname)
        {
            try
            {
                var element = new WebDriverWait(driver, TimeSpan.FromSeconds(60))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("fxg-u-modal__content")));

                if (element.Count > 0)
                {
                    driver.FindElements(By.ClassName("fxg-geo-locator__button-label"))[1].Click();
                }
                // SIGN UP / LOG IN dropdown
                var openSignUp = driver.FindElement(By.Id("fxg-dropdown-signIn"));
                openSignUp.Click();
                var lgn_clk = driver.FindElement(By.XPath("//a[@title='SIGN UP / LOG IN']"));
                lgn_clk.Click();
                await Task.Delay(3000);

                //SCREENSHOT
                var loginPage = ((ITakesScreenshot)driver).GetScreenshot();
                loginPage.SaveAsFile(screenshotPath + "loginPage.png", ScreenshotImageFormat.Png);
                System.Console.WriteLine("took screenshot of login page !");

                var text_un = driver.FindElement(By.Id("userId"));
                text_un.Click();
                text_un.Clear();
                await Task.Delay(2000);
                text_un.SendKeys(username);

                var text_pwd = driver.FindElement(By.Id("password"));
                text_pwd.Click();
                text_pwd.Clear();
                // await Task.Delay(2000);
                text_pwd.SendKeys(password);
                await Task.Delay(2000);

                var logBtn = driver.FindElement(By.Id("login-btn"));
                logBtn.Click();
                logger.Log($"Logged-in with {username} !!!", LogLevel.Info);
                await Task.Delay(8000);
                //SCREENSHOT
                var userLogin = ((ITakesScreenshot)driver).GetScreenshot();
                userLogin.SaveAsFile(screenshotPath + "userLogin.png", ScreenshotImageFormat.Png);
                System.Console.WriteLine("took screenshot of User Logged in !");

                if (driver.FindElements(By.XPath("/html/body/wlgn-root/div/ciam-head-foot/fdx-common-core/main/div/div/div/div/wlgn-login-credentials/div/wlgn-reminder-message-popup/div/div[2]")).Count > 0)                
                {
                    if (driver.FindElements(By.Id("confirm-btn")).Count > 0)
                    {
                        var confirmBtn = driver.FindElements(By.Id("confirm-btn"))[0];
                        confirmBtn.Click();
                    }
                    else if (driver.FindElements(By.Id("cancelBtn")).Count > 0)
                    {
                        var cancelBtn = driver.FindElements(By.Id("cancelBtn"))[0];
                        cancelBtn.Click();
                    }
                }

                await Task.Delay(5000);

                var Usrloged = driver.FindElement(By.Id("fxg-dropdown-signIn"));
                Usrloged.Click();
                await Task.Delay(2000);

                var ViewPayBil = driver.FindElement(By.LinkText("View & pay bill"));
                ViewPayBil.Click();
                await Task.Delay(5000);
                logger.Log($"{username} clicked on View & Pay bill !!!", LogLevel.Info);

                string url = "https://www.fedex.com/fedexbillingonline/pages/accountsummary/accountSummaryFBO.xhtml";
                //SCREENSHOT
                var accSum = ((ITakesScreenshot)driver).GetScreenshot();
                accSum.SaveAsFile(screenshotPath + "accSum.png", ScreenshotImageFormat.Png);
                System.Console.WriteLine("took screenshot of accSum Page !");

                await GetPrimaryAccount(counter, driver, logger, username, url, name, inv_num, completedDownload, methodname, screenshotPath);
                // return true;                
            }
            catch (Exception ex)
            {

                // Handle the exception or log an error
                logger.Log($"Error in PerformLogin method !!! , {ex.Message}", LogLevel.Error);
                // return false;
            }
        }

        public static async Task<bool> GetPrimaryAccount(int counter, IWebDriver driver, Logger logger, string username, string url, string name, string inv_num, List<string> completedDownload, string methodname, string screenshotPath)
        {
            List<string> primaryAcct = new List<string>();
            try
            {
                string locator = "//*[@id='mainContentId:newAccount1']";
                bool element = IsElementPresent(locator, driver);
                if (element)
                {
                    IWebElement newAcct = driver.FindElements(By.Id("mainContentId:newAccount1"))[0];
                    SelectElement select = new SelectElement(newAcct);
                    // Determine the options to select
                    List<string> optionsToSelect = new List<string>();
                    IWebElement newAcctElement = driver.FindElement(By.Id("mainContentId:newAccount1"));
                    SelectElement selectElement = new SelectElement(newAcctElement);
                    //To get the list of accounts from the dropdown
                    foreach (IWebElement option in selectElement.Options)
                    {
                        if (!primaryAcct.Contains(option.Text))
                        {
                            optionsToSelect.Add(option.Text);
                        }
                    }
                    //Selects the individual account everytime
                    foreach (string optionText in optionsToSelect)
                    {
                        primaryAcct.Add(optionText);
                        SelectElement dropdown = new SelectElement(driver.FindElement(By.Id("mainContentId:newAccount1")));
                        dropdown.SelectByText(optionText);
                        await Task.Delay(5000);
                        logger.Log($"Pimary Account has been switched", LogLevel.Info);
                        // Call the method to complete the process for the selected account
                        bool success = await SearchSetup(driver, counter, methodname, username, url, name, inv_num, completedDownload, logger, screenshotPath);
                        if (!success)
                        {
                            // Handle the case where the process completion failed for the selected account
                            logger.Log($"Failed to complete the process for account: {optionText} due to no record available.", LogLevel.Info);
                            // await Task.Delay(3000);
                            return true;
                            // Add necessary error handling or retry logic here
                        }
                        // After completing the process for the selected account, go back to the account summary
                        // name = "";
                        // inv_num = "";
                        await GenerateCSVForAll(driver, name, inv_num, 1, username, url, completedDownload, logger, methodname, screenshotPath);
                        await Task.Delay(5000);
                        driver.FindElement(By.Id("mainContentId:accSmyCmdLink")).Click();
                        await Task.Delay(5000);
                        // Refresh the select element to avoid staleness
                        selectElement = new SelectElement(driver.FindElement(By.Id("mainContentId:newAccount1")));
                    }
                    //SCREENSHOT
                    var GetPrAcc = ((ITakesScreenshot)driver).GetScreenshot();
                    GetPrAcc.SaveAsFile(screenshotPath + "GetPrAcc.png", ScreenshotImageFormat.Png);
                    System.Console.WriteLine("took screenshot of Primary Account page !");
                    // After completing the process for all selected accounts
                    return true;
                }
                else
                {
                    // Only one account is present, directly process it
                    IWebElement accountElement = driver.FindElement(By.XPath("//span[contains(@class, 'iceOutTxt text')]"));
                    string accountText = accountElement.Text;

                    if (!primaryAcct.Contains(accountText))
                    {
                        primaryAcct.Add(accountText);

                        // Call the method to complete the process for the selected account
                        bool success = await SearchSetup(driver, counter, methodname, username, url, name, inv_num, completedDownload, logger, screenshotPath);
                        if (!success)
                        {
                            // Handle the case where the process completion failed for the selected account
                            logger.Log($"Failed to complete the process for account: {accountText} due to no record available.", LogLevel.Info);
                            // await Task.Delay(3000);
                            return false;
                        }
                        // After completing the process for the selected account, go back to the account summary
                        await GenerateCSVForAll(driver, name, inv_num, 1, username, url, completedDownload, logger, methodname, screenshotPath);
                        await Task.Delay(5000);
                        driver.FindElement(By.Id("mainContentId:accSmyCmdLink")).Click();
                        await Task.Delay(5000);
                    }
                    // return true;
                }
                return false;
            }
            catch (Exception e)
            {
                bool success = false;
                logger.Log($"Error in GetPrimaryAccount method, {e.Message}", LogLevel.Error);
                success = await GetPrimaryAccount(counter, driver, logger, username, url, name, inv_num, completedDownload, methodname, screenshotPath);
                logger.Log($"Re-trying not resolved Primary Account counters{counter}, method name getPrimaryAccount", LogLevel.Info);
                return success;
            }
        }

        public static async Task<int> ShowSearchFilter(IWebDriver driver, int counter, string url, string screenshotPath)
        {
            try // screenshot required
            {
                driver.FindElement(By.Id("searchDownload")).Click();
                //SCREENSHOT
                var SearchFilter = ((ITakesScreenshot)driver).GetScreenshot();
                SearchFilter.SaveAsFile(screenshotPath + "SearchFilter.png", ScreenshotImageFormat.Png);
                System.Console.WriteLine("took screenshot of Search Filter page !");
                await Task.Delay(5000);
            }
            catch
            {
                counter++;
                if (counter <= 1)
                {
                    driver.Navigate().GoToUrl(url);
                    return await ShowSearchFilter(driver, counter, url, screenshotPath);
                }
            }
            return counter;
        }

        public static async Task<bool> SearchSetup(IWebDriver driver, int counter, string methodname, string username, string url, string name, string inv_num, List<string> completedDownload, Logger logger, string screenshotPath)
        {
            Console.WriteLine($"Search Setup method \n");
            try
            {
                await ShowSearchFilter(driver, 0, url, screenshotPath); //Screenshot required
                await Task.Delay(5000);
                Console.WriteLine($"ShowSearchFilter completed !!! \n");

                // Filter ddl Status click
                driver.FindElements(By.XPath("//select[@id='mainContentId:status']"))[0].Click();
                driver.FindElement(By.XPath("//select[@id='mainContentId:status']/option[text()='All']")).Click();
                Console.WriteLine($"clicked inv_Status \n");
                await Task.Delay(2000);

                // date range filter values
                string from_date = DateTime.Today.AddDays(-8).ToString("MM/dd/yyyy");
                string end_date = DateTime.Today.AddDays(-1).ToString("MM/dd/yyyy");
                await Task.Delay(2000);

                IWebElement from_dt = driver.FindElement(By.Id("mainContentId:fromDate"));
                from_dt.Click();
                from_dt.Clear();
                from_dt.SendKeys(from_date);
                await Task.Delay(2000);
                Console.WriteLine($"insert from date {from_date} \n");

                IWebElement end_dt = driver.FindElement(By.Id("mainContentId:toDate"));
                end_dt.Click();
                end_dt.Clear();
                end_dt.SendKeys(end_date);
                await Task.Delay(2000);
                Console.WriteLine($"insert to date {end_date} \n");

                // Filter ddl Sarch for Invoices click
                driver.FindElements(By.XPath("//select[@id='mainContentId:advTempl']"))[0].Click();
                driver.FindElement(By.XPath("//select[@id='mainContentId:advTempl']/option[text()='Invoices']")).Click();
                await Task.Delay(2000);
                Console.WriteLine($"Invoice Selected !!! \n");

                var purpleButton = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='mainContentId:advancedSearchwldDataNewSrch']")));

                // Filter checkbox click
                if (purpleButton != null)
                {
                    var checkbox = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//input[@id='mainContentId:newSerchCheckBox']")));
                    // IWebElement checkbox = driver.FindElements(By.XPath("//input[@id='mainContentId:newSerchCheckBox']"))[0];
                    checkbox.Click();
                    // await Task.Delay(8000);    //wait_after_click_event
                    Console.WriteLine($"account checkbox checked \n");
                }
                //SCREENSHOT
                var SearchFilter = ((ITakesScreenshot)driver).GetScreenshot();
                SearchFilter.SaveAsFile(screenshotPath + "SearchFilter.png", ScreenshotImageFormat.Png);
                System.Console.WriteLine("took screenshot of Search Filter Applied !");
                // await Task.Delay(5000);  
                // Filter search button click
                driver.FindElements(By.XPath("//input[@id='mainContentId:advancedSearchwldDataNewSrch' and @class='iceCmdBtn purpleButton']"))[0].Click();
                await Task.Delay(8000);    //wait_after_click_event
                Console.WriteLine($"clicked search button \n");

                // Wait for the element to be clickable
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                By elementLocator = By.XPath("//*[@id='mainContentId:invAdvSrchRsltpaginator']/option[1]");
                try
                {
                    WebDriverWait waitDriver = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    IWebElement element = waitDriver.Until(ExpectedConditions.ElementToBeClickable(elementLocator));
                    // If the element is clickable, click on it
                    element.Click();
                    // await Task.Delay(5000); // Wait after click event

                    Console.WriteLine($"SearchSetup paginator selected to 5 ! \n");
                    logger.Log($"Search done by {username} using SearchSetup", LogLevel.Info);
                    return true;
                }
                catch (WebDriverTimeoutException)
                {
                    driver.FindElements(By.XPath("//*[@id='mainContentId:accSmyCmdLink']"))[0].Click();

                    // await GetPrimaryAccount(counter, driver, logger, username, url, name, inv_num, completedDownload, methodname, screenshotPath);
                    // await Task.Delay(5000);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("test" + e);
                counter++;
                if (counter == 1)
                {
                    // Console.WriteLine(logstr);
                    Console.WriteLine(e);
                }

                if (counter < 1)   //ExceptionRetryCount
                {
                    Console.WriteLine($"Re-trying search filters counters{counter}, method name {methodname}");
                    await SearchSetup(driver, counter, methodname, username, url, name, inv_num, completedDownload, logger, screenshotPath);
                }

                Console.WriteLine($"Re-trying not resolved search filters counters{counter}, method name {methodname}");
                // string errormessage = $"Carrier:{url} Username:{username} Date:{DateTime.Now} Error:{e}";
                logger.Log($"Error in SearchSetup method !!! , {e.Message}", LogLevel.Error);//////////////////////////////////////////////////////////////////////////////
                // Email.SendEmail(errormessage);
                return false;
            }
        }

        public static async Task GenerateCSVForAll(IWebDriver driver, string name, string inv_num, int counter, string username, string url, List<string> completedDownload, Logger logger, string methodname, string screenshotPath)
        {
            Console.WriteLine($"Generate CSV For All method \n");

            var generatedDownloadLinkList = new List<string>();
            string[] generatedDownloadArray = generatedDownloadLinkList.ToArray();
            //create random name
            Random random = new Random();
            inv_num = random.Next(100000000, 999999999).ToString();

            var inv_name = inv_num + "_" + DateTime.Today.ToString("yyyyMMdd");
            Console.WriteLine($"invoice created by {inv_name} \n");
            try
            {
                ReadOnlyCollection<IWebElement> createCSVDiv = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID1']/tbody/tr[9]"));
                if (createCSVDiv.Count > 0)
                {
                    ReadOnlyCollection<IWebElement> outerbox = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID1-8-0']"));
                    if (outerbox.Count > 0)
                    {
                        ReadOnlyCollection<IWebElement> innerTable = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID36']"));
                        if (innerTable.Count > 0)
                        {
                            ReadOnlyCollection<IWebElement> innerTableBody = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID36']/tbody"));
                            if (innerTableBody.Count > 0)
                            {
                                ReadOnlyCollection<IWebElement> innerTbleRow2 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID36']/tbody/tr[2]"));
                                if (innerTbleRow2.Count > 0)
                                {
                                    ReadOnlyCollection<IWebElement> innerTblRowData = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID36-1-0']"));
                                    if (innerTblRowData.Count > 0)
                                    {
                                        ReadOnlyCollection<IWebElement> tablepath = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID42']"));
                                        if (tablepath.Count > 0)
                                        {
                                            ReadOnlyCollection<IWebElement> step1 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID42']/tbody/tr[1]"));
                                            if (step1.Count > 0)
                                            {
                                                ReadOnlyCollection<IWebElement> step2 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID42-0-0']"));
                                                if (step2.Count > 0)
                                                {
                                                    ReadOnlyCollection<IWebElement> step3 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID43']"));
                                                    if (step3.Count > 0)
                                                    {
                                                        ReadOnlyCollection<IWebElement> step4 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID43']/tbody"));
                                                        if (step4.Count > 0)
                                                        {
                                                            ReadOnlyCollection<IWebElement> step5 = driver.FindElements(By.Id("mainContentId:searchResultID47"));
                                                            if (step5.Count > 0)
                                                            {
                                                                ReadOnlyCollection<IWebElement> step6 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID47']/tbody"));
                                                                if (step6.Count > 0)
                                                                {
                                                                    ReadOnlyCollection<IWebElement> step7 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID47']/tbody/tr[1]"));
                                                                    if (step7.Count > 0)
                                                                    {
                                                                        ReadOnlyCollection<IWebElement> step8 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID47-0-1']"));
                                                                        if (step8.Count > 0)
                                                                        {
                                                                            ReadOnlyCollection<IWebElement> step9 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID50']"));
                                                                            IWebElement inputBox = driver.FindElement(By.XPath("//*[@id='mainContentId:searchResultID51']"));
                                                                            // Task.Delay(2000);
                                                                            inputBox.Click();
                                                                            inputBox.Clear();
                                                                            inputBox.SendKeys(inv_name);
                                                                            await Task.Delay(2000);
                                                                            // generatedDownloadArray.Append(inv_name);
                                                                            generatedDownloadArray = new string[] { inv_name };

                                                                        }
                                                                        ReadOnlyCollection<IWebElement> step10 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID61']"));
                                                                        if (step10.Count > 0)
                                                                        {
                                                                            ReadOnlyCollection<IWebElement> step11 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultTemplateType']"));
                                                                            if (step11.Count > 0)
                                                                            {
                                                                                driver.FindElement(By.XPath("//*[@id='mainContentId:searchResultTemplateType']/option[2]")).Click();
                                                                                await Task.Delay(2000);
                                                                            }
                                                                            ReadOnlyCollection<IWebElement> step12 = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID64']"));
                                                                            if (step12.Count > 0)
                                                                            {
                                                                                ReadOnlyCollection<IWebElement> step13 = driver.FindElements(By.XPath("//*[@id='mainContentId:fileType1']"));
                                                                                if (step13.Count > 0)
                                                                                {
                                                                                    driver.FindElement(By.XPath("//*[@id='mainContentId:fileType1']/option[2]")).Click();
                                                                                    await Task.Delay(2000);
                                                                                }
                                                                            }
                                                                        }
                                                                        ReadOnlyCollection<IWebElement> step14cdw = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID64']"));
                                                                        if (step14cdw.Count > 0)
                                                                        {
                                                                            ReadOnlyCollection<IWebElement> step15cdw = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID42']/tbody/tr[2]"));
                                                                            if (step15cdw.Count > 0)
                                                                            {
                                                                                ReadOnlyCollection<IWebElement> step16cdw = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID42-1-0']"));
                                                                                if (step16cdw.Count > 0)
                                                                                {
                                                                                    ReadOnlyCollection<IWebElement> step17cdw = driver.FindElements(By.XPath("//*[@id='mainContentId:searchResultID78']"));
                                                                                    if (step17cdw.Count > 0)
                                                                                    {
                                                                                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                                                                                        IWebElement createButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='mainContentId:searchResultID79']")));
                                                                                        Actions actions = new Actions(driver);
                                                                                        actions.MoveToElement(createButton).Click().Perform();
                                                                                        await Task.Delay(8000);
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                        //SCREENSHOT
                                                                        var searchDownload = ((ITakesScreenshot)driver).GetScreenshot();
                                                                        searchDownload.SaveAsFile(screenshotPath + "searchDownload.png", ScreenshotImageFormat.Png);
                                                                        System.Console.WriteLine("took screenshot of Search/Download page !");
                                                                        // IWebElement AccSummary = new WebDriverWait(driver, TimeSpan.FromSeconds(5000)).Until(ExpectedConditions.ElementExists(By.Id("mainContentId:accSmyCmdLink")));
                                                                        // AccSummary.Click();
                                                                        // await Task.Delay(3000);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                logger.Log($"Invoice name {inv_name} has been generated for {username} !!!", LogLevel.Info);
                //logger.Log($"GenerateCSVForAll completed successfully !", LogLevel.Info);
                await DownloadCSV(driver, counter, url, username, generatedDownloadArray, logger, screenshotPath);
                logger.Log($"Invoice downloaded for {username} with file name {inv_name}", LogLevel.Info);
                logger.Log("DownloadCSV completed successfully!", LogLevel.Info);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                logger.Log($"{username} facing error in GenerateCSVForAll method, {e.Message}", LogLevel.Error);
            }
        }

        public static async Task<bool> DownloadCSV(IWebDriver driver, int counter, string url, string username, string[] generatedDownloadArray, Logger logger, string screenshotPath)
        {
            var fileName = generatedDownloadArray[0];
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                IWebElement table = wait.Until(ExpectedConditions.ElementExists(By.Id("mainContentId:dwldCtrTbl:tbody")));
                IReadOnlyCollection<IWebElement> rows = table.FindElements(By.TagName("tr"));

                if (rows.Count > 0)
                {
                    int tdCount = rows.ElementAt(0).FindElements(By.TagName("td")).Count;
                    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    {
                        IWebElement row = rows.ElementAt(rowIndex);
                        IReadOnlyCollection<IWebElement> tdList = row.FindElements(By.TagName("td"));

                        if (tdList.Count == tdCount)
                        {
                            string tdText = tdList.ElementAt(0).Text;
                            if (generatedDownloadArray.Any(dwnld => dwnld.Contains(tdText)))
                            {
                                var canDownload = true;
                                while (canDownload)
                                {
                                    var anchor_clickEvent = tdList.ElementAt(0).FindElements(By.TagName("a"));
                                    if (anchor_clickEvent.Count == 0)
                                    {
                                        DownloadFilesTable(driver, counter, screenshotPath);
                                        await Task.Delay(3000);
                                        table = wait.Until(ExpectedConditions.ElementExists(By.Id("mainContentId:dwldCtrTbl:tbody")));
                                        rows = table.FindElements(By.TagName("tr"));
                                        row = rows.ElementAt(rowIndex);
                                        tdList = row.FindElements(By.TagName("td"));
                                    }
                                    else
                                    {
                                        canDownload = false;
                                    }
                                }
                                //SCREENSHOT
                                var DownCSV = ((ITakesScreenshot)driver).GetScreenshot();
                                DownCSV.SaveAsFile(screenshotPath + "DownCSV.png", ScreenshotImageFormat.Png);
                                System.Console.WriteLine("took screenshot of Download CSV page !");

                                // IWebElement anchor = tdList.ElementAt(0).FindElement(By.TagName("a"));
                                // wait.Until(ExpectedConditions.ElementToBeClickable(anchor));
                                // await Task.Delay(5000);
                                // await Task.Run(() => anchor.Click());
                                // await Task.Run(async () =>
                                // {
                                //     await Task.Delay(5000); // Delay for 5 seconds
                                //     anchor.Click();
                                // });
                                IWebElement anchor = tdList.ElementAt(0).FindElement(By.TagName("a"));
                                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                                await Task.Run(() => jsExecutor.ExecuteScript("arguments[0].click();", anchor));;

                                // -----------------provide the desired download path------------------
                                // var downloadPath = @"E:\Office\Web Scraping\FedEx\Download"; // Replace with the actual path where the files are downloaded
                                // var filePath = Path.Combine(downloadPath, fileName);
                                // wait.Until(d => File.Exists(filePath));
                                await Task.Delay(5000);                                
                                // Exit the loop after downloading the desired file
                                // break;
                                // string mainWindowHandle = driver.CurrentWindowHandle;
                                // HashSet<string> windowHandles = new HashSet<string> { mainWindowHandle };

                                // string anchorWindowHandle = driver.CurrentWindowHandle;
                                // windowHandles.Add(anchorWindowHandle);

                                // driver.SwitchTo().Window(anchorWindowHandle);

                                // // Perform the file download logic here

                                // driver.Close(); // Close the child window
                                // driver.SwitchTo().Window(mainWindowHandle); // Switch back to the main window
                                string mainWindowHandle = driver.CurrentWindowHandle;
                                HashSet<string> windowHandles = new HashSet<string> { mainWindowHandle };

                                string anchorWindowHandle = driver.WindowHandles.Last(); // Get the handle of the last opened window (popup window)
                                windowHandles.Add(anchorWindowHandle);

                                driver.SwitchTo().Window(anchorWindowHandle);

                                // Perform the file download logic here

                                driver.Close(); // Close the popup window
                                driver.SwitchTo().Window(mainWindowHandle); // Switch back to the main window
                            }
                        }
                    }
                }
                // driver.Quit();
                return true;
            }
            catch (Exception e)
            {
                counter++;
                Console.WriteLine("Error in DownloadCSV method!!!");
                logger.Log($"Error in downloading {fileName}!!!, {e.Message}", LogLevel.Error);
                driver.Quit();
                return false;
            }
        }

        public static async void DownloadFilesTable(IWebDriver driver, int counter, string screenshotPath)
        {
            string fedexurl = "https://www.fedex.com/fedexbillingonline/pages/accountsummary/accountSummaryFBO.xhtml";
            try
            {
                driver.Navigate().GoToUrl(fedexurl);
                await Task.Delay(5000);
                // Scroll up to the top of the page/////////////////////////////////////////////////////////////////////////////////////
                ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, 0);");
                // Wait for the scroll animation to complete (adjust delay as needed)
                await Task.Delay(1000);

                if (driver.FindElements(By.XPath("//*[@id='searchDownload']")).Count > 0)
                {
                    driver.FindElement(By.XPath("//*[@id='searchDownload']")).Click();
                    await Task.Delay(2000);
                }

                if (driver.FindElements(By.XPath("//*[@id='searchDownloadList']")).Count > 0)
                {
                    var downloadCenter = driver.FindElement(By.XPath("//*[@id='searchDownloadList']/li[2]")); //mainContentId:downloadCenterId   
                    //SCREENSHOT
                    var DownLodFileTbl = ((ITakesScreenshot)driver).GetScreenshot();
                    DownLodFileTbl.SaveAsFile(screenshotPath + "DownLodFileTbl.png", ScreenshotImageFormat.Png);
                    System.Console.WriteLine("took screenshot of AccSum page while download file is getting ready !");
                    downloadCenter.Click();
                    await Task.Delay(8000);
                }
            }
            catch (Exception e)
            {
                if (counter == 0)
                {
                    Console.WriteLine("Exception raised in method DownloadFilesTable");
                    Console.WriteLine(e.ToString());
                    // counter++;
                    // DownloadFilesTable(driver, ref counter);
                }
            }
        }

        public static bool IsElementPresent(string locator, IWebDriver driver)
        {
            try
            {
                driver.FindElement(By.XPath(locator));
            }
            catch
            {
                return false;
            }
            return true;
        }

        public class Logger
        {
            private readonly StreamWriter writer;
            private readonly LogLevel level;

            public Logger(StreamWriter writer, LogLevel level)
            {
                this.writer = writer;
                this.level = level;
            }

            public void Log(string message, LogLevel logLevel)
            {
                if (logLevel >= level)
                {
                    string logMessage = string.Format(LogFormat, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), logLevel.ToString(), message);
                    writer.WriteLine(logMessage);
                }
            }
        }

        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }
    }
}