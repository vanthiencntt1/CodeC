


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace WpfAuto
{
    public partial class MainWindow : Window
    {
        #region Biến
        int soCuaSo = 1;
        int viTriX = 0;
        int viTriY = 0;
        List<ChromeDriver> chromeDrivers = new List<ChromeDriver>();
     


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        bool isRunning = false;

        List<Account> accounts = new List<Account>
        {
         
           
            // Thêm nhiều tài khoản khác nếu cần
        };

        List<CancellationTokenSource> cancellationTokenSources = new List<CancellationTokenSource>();
        #endregion Biến

        public MainWindow()
        {
            InitializeComponent();
            // Gán số cửa sổ bằng số account truyền vào
            soCuaSo = accounts.Count;

            AccountListBox.ItemsSource = accounts;
        }

        public async void Button_Click(object sender, RoutedEventArgs e)
        {
            
           
                string url = Url.Text;
                int chieuRong = int.Parse(Width.Text);
                int chieuCao = int.Parse(Height.Text);
                soCuaSo = Math.Min(soCuaSo, accounts.Count);
                for (int i = 0; i < soCuaSo; i++)
                {
                    // string userDataDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TempChromeUser_{Guid.NewGuid()}");
                    ChromeOptions chromeOptions = new ChromeOptions();
                    chromeOptions.AddExcludedArgument("enable-automation");
                    chromeOptions.AddAdditionalOption("useAutomationExtension", false);
                    chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
                    chromeOptions.AddArgument("--disable-infobars"); // Có thể đã bị loại bỏ trong các phiên bản mới
                    chromeOptions.AddArgument("--disable-extensions");
                    chromeOptions.AddArgument("--disable-plugins-discovery");
                    chromeOptions.AddArgument("--lang=vi");
                  
                    chromeOptions.AddArgument("--disable-gpu"); // Tùy chọn, hỗ trợ Windows
                   
                    chromeOptions.AddArgument("--disable-extensions"); // Tắt các tiện ích mở rộng
                    chromeOptions.AddUserProfilePreference("credentials_enable_service", false); // Tắt dịch vụ lưu mật khẩu
                    chromeOptions.AddUserProfilePreference("profile.password_manager_enabled", false); // Tắt trình quản lý mật khẩu
                    chromeOptions.AddArgument("--disable-infobars");
                    //chromeOptions.AddArgument($"--user-data-dir={userDataDir}"); // Sử dụng thư mục người dùng mới

                    // tắt chuyển trang                                                                                
                    // chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.javascript", 2); // Tắt JavaScript


                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    ChromeDriver chromeDriver = new ChromeDriver(service, chromeOptions);
                    chromeDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(120);
                    chromeDriver.Navigate().GoToUrl(url);

                    chromeDriver.Manage().Window.Size = new System.Drawing.Size(chieuRong, chieuCao);
                    chromeDriver.Manage().Window.Position = new System.Drawing.Point(viTriX, viTriY);

                    // Đưa cửa sổ lên trên cùng
                    IntPtr hWnd = FindWindow(null, chromeDriver.Title);
                    if (hWnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(hWnd);
                    }

                    // Cố gắng ẩn dấu hiệu tự động hóa bằng JavaScript
                    try
                    {
                        IJavaScriptExecutor js = chromeDriver as IJavaScriptExecutor;
                        js.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                    }
                    catch (Exception ex)
                    {
                        // Xử lý ngoại lệ nếu cần
                        Console.WriteLine(ex.Message);
                    }

                    chromeDrivers.Add(chromeDriver);
                    viTriX += chieuRong + 100; // Di chuyển sang phải
                }

                // Đăng nhập cho từng tài khoản đồng thời
                var loginTasks = chromeDrivers.Select((driver, index) =>
                {
                    if (index < accounts.Count)
                    {
                        var account = accounts[index];
                        return Login(driver, account.Username, account.Password);
                    }
                    return Task.CompletedTask;
                });

                await Task.WhenAll(loginTasks);

                // Khởi tạo và khởi chạy các vòng lặp riêng biệt cho từng trình duyệt
                foreach (var driver in chromeDrivers)
                {
                    var cts = new CancellationTokenSource();
                    cancellationTokenSources.Add(cts);
                    isRunning = true;
                    _ = RunTasksContinuously(driver, cts.Token);
                }
            
          
        
        }

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;

            foreach (var cts in cancellationTokenSources)
            {
                cts.Cancel();
            }

            foreach (var chromeDriver in chromeDrivers)
            {
                chromeDriver.Quit();
            }

            chromeDrivers.Clear();
            cancellationTokenSources.Clear();
        }

        public async Task Login(ChromeDriver chromeDriver, string username, string password)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
         ;
            chromeDriver.Navigate().GoToUrl(Url + "/wp-login.php");
   
            // Tìm phần tử nhập email và điền thông tin
            IWebElement userLogin = wait.Until(d => d.FindElement(By.Id("user_login")));
            userLogin.Clear();
            userLogin.SendKeys(username);

            // Tìm phần tử nhập mật khẩu và điền thông tin
            IWebElement userPass = wait.Until(d => d.FindElement(By.Id("user_pass")));
            userPass.Clear();
            userPass.SendKeys(password);

            // Tìm nút đăng nhập và nhấp vào
            IWebElement submitButton = wait.Until(d => d.FindElement(By.Id("wp-submit")));
            submitButton.Click();
            await Task.Delay(3000);
            chromeDriver.Navigate().Refresh();
        }

        public void JS()
        {
            // Sử dụng phiên ChromeDriver hiện tại thay vì tạo mới
            if (chromeDrivers.Count > 0)
            {
                var chromeDriver = chromeDrivers[0]; // Chọn phiên đầu tiên để thực thi JavaScript
                IJavaScriptExecutor js = chromeDriver;
                // Thực thi JavaScript
                js.ExecuteScript("window.location.href = '';");
            }
        }

        // Đưa cửa sổ đầu tiên lên trên cùng
        public void BringWindowToFront(int index)
        {
            if (index < 0 || index >= chromeDrivers.Count)
                return;

            // Đưa cửa sổ trình duyệt tại index lên trên cùng
            IntPtr hWnd = FindWindow(null, chromeDrivers[index].Title);
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
            }
        }

        public async Task RunTasksContinuously(ChromeDriver driver, CancellationToken cancellationToken)
        {
            // Thiết lập thời gian cho các tác vụ
            TimeSpan diemDanhInterval = TimeSpan.FromHours(25);
            TimeSpan TongMonInterval = TimeSpan.FromHours(25);
            TimeSpan phucLoiInterval = TimeSpan.FromHours(7.2);
            TimeSpan truyenThuaInterval = TimeSpan.FromHours(8.2);
            TimeSpan thiLuyenInterval = TimeSpan.FromHours(8.2);
            TimeSpan BossInterval = TimeSpan.FromHours(1);

            DateTime nextDiemDanhTime = DateTime.Now;
            DateTime nextTongMonTime = DateTime.Now;
            DateTime nextPhucLoiTime = DateTime.Now;
            DateTime nexttruyenThuaTime = DateTime.Now;
            DateTime nextthiLuyenTime = DateTime.Now;
            DateTime nextBossTime = DateTime.Now;

            while (!cancellationToken.IsCancellationRequested && isRunning)
            {
                if (DateTime.Now >= nextthiLuyenTime)
                {
                     await ThiLuyen(driver);
                    nextthiLuyenTime = DateTime.Now.Add(thiLuyenInterval); // Cập nhật thời gian tiếp theo cho ThiLuyen
                }
                // Kiểm tra nếu đến thời điểm thực hiện DiemDanh
                if (DateTime.Now >= nextDiemDanhTime)
                {
                    await DiemDanh(driver);
                    nextDiemDanhTime = DateTime.Now.Add(diemDanhInterval); // Cập nhật thời gian tiếp theo cho DiemDanh
                }

                // Kiểm tra nếu đến thời điểm thực hiện PhucLoi
                if (DateTime.Now >= nextPhucLoiTime)
                {
                    await PhucLoi(driver);
                    nextPhucLoiTime = DateTime.Now.Add(phucLoiInterval); // Cập nhật thời gian tiếp theo cho PhucLoi
                }

                if (DateTime.Now >= nexttruyenThuaTime)
                {
                    await TruyenThua(driver);
                    nexttruyenThuaTime = DateTime.Now.Add(truyenThuaInterval); // Cập nhật thời gian tiếp theo cho TruyenThua
                }



                if (DateTime.Now >= nextTongMonTime)
                {
                    await TongMon(driver);
                    nextTongMonTime = DateTime.Now.Add(TongMonInterval); // Cập nhật thời gian tiếp theo cho TongMon
                }

                if (DateTime.Now >= nextBossTime)
                {
                    await Boss(driver);
                    nextBossTime = DateTime.Now.Add(BossInterval); // Cập nhật thời gian tiếp theo cho Boss
                }
               

                // Đợi một khoảng thời gian ngắn trước khi kiểm tra lại
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Nếu bị hủy, thoát vòng lặp
                    break;
                }
            }
        }

        #region HOATDONG
        public async Task DiemDanh(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/diem-danh");

            try
            {
                IWebElement submitButton = wait.Until(d => d.FindElement(By.Id("checkInButton")));
                submitButton.Click();
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }

        public async Task PhucLoi(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/phuc-loi");

            try
            {
                IWebElement claimButton = wait.Until(d => d.FindElement(By.ClassName("mycred-tbr-claim-button")));
                claimButton.Click();
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }

        public async Task TruyenThua(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/mo-ra-truyen-thua");

            try
            {
                IWebElement claimButton = wait.Until(d => d.FindElement(By.ClassName("mycred-tbr-claim-button")));
                claimButton.Click();
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }

        public async Task ThiLuyen(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/thi-luyen-tong-mon");
            IJavaScriptExecutor js = (IJavaScriptExecutor)chromeDriver;
            js.ExecuteScript(@" var script = document.querySelector('script[src*=\'disable-devtool\']');if (script) { script.remove(); } ");



            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            // Ghi đè thuộc tính location của window với biến Url

            js.ExecuteScript(@" Object.freeze(window.location);");
            js.ExecuteScript(@"
                window.addEventListener('beforeunload', function (e) {
                    e.preventDefault();
                    e.returnValue = '';  // Ngăn chặn hành động reload hoặc chuyển hướng
                });

                window.addEventListener('unload', function (e) {
                    e.preventDefault();
                    e.returnValue = '';  // Ngăn chặn hành động chuyển hướng
                });
            ");


            try
            {
                IWebElement claimButton = wait.Until(d => d.FindElement(By.ClassName("mycred-tbr-claim-button")));
                claimButton.Click();
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }

        public async Task TongMon(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/danh-sach-thanh-vien-tong-mon");

            try
            {
                IWebElement claimButton = wait.Until(d => d.FindElement(By.Id("te-le-button")));
                claimButton.Click();
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }

        public async Task Boss(ChromeDriver chromeDriver)
        {
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(10));
            chromeDriver.Navigate().GoToUrl(Url + "/hoang-vuc");

            try
            {
                IWebElement claimButton = wait.Until(d => d.FindElement(By.Id("battle-button")));
                claimButton.Click();
               IJavaScriptExecutor js = (IJavaScriptExecutor)chromeDriver;
                js.ExecuteScript("document.querySelector('#boss-damage-screen > button.attack-button').click();");

               
            }
            catch
            {
                // Xử lý ngoại lệ nếu cần
            }
            await Task.Delay(3000);
        }
        #endregion HOATDONG

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy thông tin từ TextBox
            string email = Username.Text;
            string password = PasswordTextBox.Text;

            // Kiểm tra thông tin không trống
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                var newAccount = new Account(email, password);
                accounts.Add(newAccount);
                AccountListBox.Items.Refresh(); // Refresh the list
                Username.Clear(); // Xóa trường nhập liệu
                PasswordTextBox.Clear();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin email và mật khẩu.");
            }
        }

        private void UpdateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountListBox.SelectedItem is Account selectedAccount)
            {
                // Lấy thông tin từ TextBox
                string email = Username.Text;
                string password = PasswordTextBox.Text;

                // Kiểm tra thông tin không trống
                if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
                {
                    selectedAccount.Username = email;
                    selectedAccount.Password = password;
                    AccountListBox.Items.Refresh(); // Refresh the list
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin email và mật khẩu.");
                }
            }
        }
        private void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountListBox.SelectedItem is Account selectedAccount)
            {
                accounts.Remove(selectedAccount);
                AccountListBox.Items.Refresh(); // Refresh the list
                Username.Clear(); // Xóa trường nhập liệu
                PasswordTextBox.Clear();
            }
        }

        private void AccountListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Nếu có mục được chọn, hiển thị thông tin vào các TextBox
            if (AccountListBox.SelectedItem is Account selectedAccount)
            {
                Username.Text = selectedAccount.Username;
                PasswordTextBox.Text = selectedAccount.Password;
            }
        }
    }

    
}
