using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Project
{
    public class SplashScreenWeb : Form
    {
        private WebView2? webView;
        private bool _closingSplash;
        private bool _loadingStarted;
        private System.Windows.Forms.Timer? _fallbackTimer;
        private const int SplashWidth = 1280;
        private const int SplashHeight = 720;

        public SplashScreenWeb()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(SplashWidth, SplashHeight);
            DoubleBuffered = true;
            InitWebView();
        }

        private async void InitWebView()
        {
            try
            {
                webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };

                Controls.Add(webView);
                await webView.EnsureCoreWebView2Async(null);

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
                {
                    try
                    {
                        if (_loadingStarted &&
                            webView?.CoreWebView2?.DocumentTitle?.Contains("Ready", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            CloseSplash();
                        }
                    }
                    catch
                    {
                        // Ignore title-read failures and let the other handoff paths work.
                    }
                };
                webView.CoreWebView2.WebMessageReceived += (_, e) =>
                {
                    string message = e.TryGetWebMessageAsString();
                    if (string.Equals(message, "started", StringComparison.OrdinalIgnoreCase))
                    {
                        _loadingStarted = true;
                        StartPostClickFallbackTimer();
                        return;
                    }

                    if (_loadingStarted && string.Equals(message, "ready", StringComparison.OrdinalIgnoreCase))
                    {
                        CloseSplash();
                    }
                };

                string htmlPath = Path.Combine(Application.StartupPath, "wildnest_splash_ultimate.html");
                if (File.Exists(htmlPath))
                {
                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    string fallbackHtml = """
                    <!doctype html>
                    <html>
                    <head>
                      <meta charset="utf-8">
                      <title>WildNest</title>
                      <style>
                        body{
                          margin:0;
                          font-family:'Segoe UI',sans-serif;
                          background:linear-gradient(135deg,#04170d 0%,#0a2c18 60%,#133926 100%);
                          color:#f8f4ef;
                          display:flex;
                          align-items:center;
                          justify-content:center;
                          height:100vh;
                        }
                        .card{
                          width:72%;
                          max-width:820px;
                          padding:42px 48px;
                          border-radius:28px;
                          background:rgba(255,255,255,.05);
                          border:1px solid rgba(212,160,23,.35);
                          box-shadow:0 24px 80px rgba(0,0,0,.35);
                          text-align:center;
                        }
                        h1{
                          margin:0 0 12px 0;
                          font-family:Georgia,serif;
                          font-size:56px;
                          letter-spacing:2px;
                        }
                        p{
                          margin:0;
                          color:rgba(248,244,239,.82);
                          font-size:18px;
                          line-height:1.6;
                        }
                        .accent{color:#d4a017;}
                      </style>
                    </head>
                    <body>
                      <div class="card">
                        <h1>WILDNEST</h1>
                        <p>The splash HTML was not found yet.<br><span class="accent">Place wildnest_splash_ultimate.html in the project root.</span></p>
                      </div>
                    </body>
                    </html>
                    """;
                    webView.NavigateToString(fallbackHtml);
                    StartPostClickFallbackTimer();
                }
            }
            catch
            {
                StartPostClickFallbackTimer();
                CloseSplash();
            }
        }

        private void StartPostClickFallbackTimer()
        {
            _fallbackTimer?.Stop();
            _fallbackTimer?.Dispose();
            _fallbackTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _fallbackTimer.Tick += (_, _) =>
            {
                _fallbackTimer?.Stop();
                CloseSplash();
            };
            _fallbackTimer.Start();
        }

        private void CloseSplash()
        {
            if (_closingSplash)
                return;

            _closingSplash = true;
            _fallbackTimer?.Stop();
            if (InvokeRequired)
            {
                Invoke((Action)CloseSplash);
                return;
            }

            try
            {
                Close();
            }
            catch
            {
                if (!IsDisposed)
                    Close();
            }
        }

        private void InitializeComponent()
        {
            Name = "SplashScreenWeb";
            Text = "WildNest";
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(SplashWidth, SplashHeight);
        }
    }
}
