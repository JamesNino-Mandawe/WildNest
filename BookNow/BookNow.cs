using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Project.Booking;

namespace Project
{
    public partial class BookNow : UserControl
    {
        private readonly BookNowWebBridge _bridge = new();
        private WebView2? _webView;
        private Label? _fallbackLabel;
        private string? _pendingTab;

        public BookNow()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = Color.FromArgb(240, 237, 232);
            BuildWebBookingSurface();
        }

        private async void BuildWebBookingSurface()
        {
            Controls.Clear();

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(240, 237, 232)
            };
            Controls.Add(_webView);

            try
            {
                await _webView.EnsureCoreWebView2Async(null);
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                _webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;

                string htmlPath = Path.Combine(Application.StartupPath, "BookNow", "wildnest_booknow_final.html");
                if (!File.Exists(htmlPath))
                    htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BookNow", "wildnest_booknow_final.html");

                if (!File.Exists(htmlPath))
                {
                    ShowFallback("BookNow HTML was not found. Expected: " + htmlPath);
                    return;
                }

                _webView.Source = new Uri(htmlPath);
            }
            catch (Exception ex)
            {
                ShowFallback("Could not load the premium Book Now UI: " + ex.Message);
            }
        }

        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_webView?.CoreWebView2 == null)
                return;

            await _webView.CoreWebView2.ExecuteScriptAsync(GetBridgeScript());
            if (!string.IsNullOrWhiteSpace(_pendingTab))
                SwitchHtmlTab(_pendingTab);
        }

        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (_webView?.CoreWebView2 == null)
                return;

            string response = await _bridge.HandleAsync(e.WebMessageAsJson);
            _webView.CoreWebView2.PostWebMessageAsJson(response);
        }

        public void OpenCabinStay() => SwitchHtmlTab("cabin");

        public void OpenDayVisit() => SwitchHtmlTab("day");

        public void OpenExperienceVisit() => SwitchHtmlTab("exp");

        public void OpenFullStayExperience() => SwitchHtmlTab("full");

        private async void SwitchHtmlTab(string tabName)
        {
            _pendingTab = tabName;
            if (_webView?.CoreWebView2 == null)
                return;

            await _webView.CoreWebView2.ExecuteScriptAsync($@"
(function(){{
  const btn=[...document.querySelectorAll('.tab-btn')].find(b => (b.getAttribute('onclick')||'').includes(""{tabName}""));
  if (btn && typeof switchTab === 'function') switchTab('{tabName}', btn);
  document.querySelector('.tabs-wrap')?.scrollIntoView({{block:'start'}});
}})();");
        }

        private void lblHeroSub_Click(object? sender, EventArgs e)
        {
        }

        private void ShowFallback(string message)
        {
            Controls.Clear();
            _fallbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 237, 232),
                ForeColor = Color.FromArgb(7, 26, 14),
                Font = new Font("Segoe UI", 11f),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = message
            };
            Controls.Add(_fallbackLabel);
        }

        private static string GetBridgeScript()
        {
            return @"
(function(){
  if (window.__wildNestBridgeReady) return;
  window.__wildNestBridgeReady = true;

  const style = document.createElement('style');
  style.textContent = `
    nav{display:none!important}
    body{overflow-x:hidden!important;background:#f0ede8!important}
    .hero{display:block!important}
    .tabs-wrap{top:0!important}
    .main-wrap{max-width:1320px!important;padding:28px 42px 84px!important;grid-template-columns:minmax(0,1fr) 360px!important;gap:30px!important}
    .booking-left{min-width:0!important}
    .summary-card{position:relative!important}
    .sum-body{max-height:none!important;overflow:visible!important}
    .btn-confirm[disabled]{opacity:.6;cursor:not-allowed}
    .wn-toast{position:fixed;right:28px;top:76px;z-index:9999;max-width:380px;padding:14px 18px;border-radius:14px;background:#071a0e;color:#f7f3ec;border:1px solid rgba(200,146,42,.45);box-shadow:0 18px 55px rgba(0,0,0,.25);font:600 13px 'DM Sans',sans-serif;animation:wnIn .18s ease}
    .wn-toast.error{border-color:rgba(190,55,55,.6);background:#2a0e0e}
    @keyframes wnIn{from{opacity:0;transform:translateY(-10px)}to{opacity:1;transform:translateY(0)}}
    .wn-modal-backdrop{position:fixed;inset:0;z-index:9998;background:rgba(7,26,14,.62);display:flex;align-items:center;justify-content:center;padding:22px}
    .wn-modal{width:min(440px,100%);border-radius:20px;background:#f7f3ec;border:1px solid rgba(200,146,42,.35);box-shadow:0 24px 80px rgba(0,0,0,.35);overflow:hidden}
    .wn-modal-head{background:#071a0e;color:#d4a017;padding:22px 26px;font:700 24px 'Cormorant Garamond',serif}
    .wn-modal-body{padding:24px 26px;color:#4d463d;font:400 14px/1.55 'DM Sans',sans-serif}
    .wn-code{width:100%;box-sizing:border-box;margin:16px 0 4px;padding:14px 16px;border-radius:12px;border:1.5px solid rgba(200,146,42,.45);font:800 24px 'DM Sans',sans-serif;letter-spacing:8px;text-align:center;background:#fff;color:#071a0e}
    .wn-modal-actions{display:flex;gap:10px;justify-content:flex-end;padding:0 26px 24px}
    .wn-btn{border:none;border-radius:999px;padding:11px 18px;font:700 12px 'DM Sans',sans-serif;cursor:pointer}
    .wn-btn.cancel{background:#e7e1d8;color:#4d463d}
    .wn-btn.ok{background:#d4a017;color:#071a0e}
  `;
  document.head.appendChild(style);

  const tabMap = { cb:'#tab-cabin', dv:'#tab-day', eo:'#tab-exp', fs:'#tab-full' };
  const stepMap = { cb:'#cb-s3', dv:'#dv-s3', eo:'#eo-s3', fs:'#fs-s3' };

  function toast(message, isError){
    const t=document.createElement('div');
    t.className='wn-toast'+(isError?' error':'');
    t.textContent=message;
    document.body.appendChild(t);
    setTimeout(()=>t.remove(),4200);
  }

  function askCode(message){
    return new Promise(resolve=>{
      const b=document.createElement('div');
      b.className='wn-modal-backdrop';
      b.innerHTML=`<div class='wn-modal'>
        <div class='wn-modal-head'>Email Verification</div>
        <div class='wn-modal-body'>
          <div>${message}</div>
          <input class='wn-code' maxlength='6' inputmode='numeric' placeholder='000000'/>
          <div style='font-size:12px;color:#8d8174'>Enter the 6-digit code sent by WildNest before this booking is saved.</div>
        </div>
        <div class='wn-modal-actions'>
          <button class='wn-btn cancel'>Cancel</button>
          <button class='wn-btn ok'>Verify & Confirm</button>
        </div>
      </div>`;
      document.body.appendChild(b);
      const input=b.querySelector('.wn-code');
      input.focus();
      input.addEventListener('input',()=>input.value=input.value.replace(/\D/g,'').slice(0,6));
      b.querySelector('.cancel').onclick=()=>{b.remove();resolve('');};
      b.querySelector('.ok').onclick=()=>{const v=input.value.trim();b.remove();resolve(v);};
      input.addEventListener('keydown',ev=>{if(ev.key==='Enter')b.querySelector('.ok').click();});
    });
  }

  function text(q, root=document){ return (root.querySelector(q)?.textContent || '').trim(); }
  function val(q, root=document){ return (root.querySelector(q)?.value || '').trim(); }
  function intText(id, def){ const n=parseInt((document.getElementById(id)?.textContent||'').replace(/\D/g,''),10); return Number.isFinite(n)?n:def; }
  function downloadDataUrl(dataUrl, filename){
    if(!dataUrl){ toast('QR is not ready yet. Please wait for confirmation to finish.', true); return; }
    const a=document.createElement('a');
    a.href=dataUrl;
    a.download=filename || 'WildNest-QR.png';
    document.body.appendChild(a);
    a.click();
    a.remove();
  }
  function wireConfirmedActions(prefix, result){
    const panel=document.getElementById(prefix+'-s5');
    if(!panel) return;
    const rid=result.reservationId || document.getElementById(prefix+'-bid')?.textContent || 'WildNest';
    const save=panel.querySelector('.btn-save-qr');
    if(save){
      save.disabled=false;
      save.onclick=ev=>{
        ev.preventDefault();
        downloadDataUrl(result.qrDataUrl, `${rid}-QR.png`);
      };
    }
    const print=panel.querySelector('.btn-print');
    if(print){
      print.onclick=ev=>{
        ev.preventDefault();
        window.print();
      };
    }
    const newBook=panel.querySelector('.btn-newbook');
    if(newBook){
      newBook.onclick=ev=>{
        ev.preventDefault();
        location.reload();
      };
    }
    const portal=panel.querySelector('.btn-portal');
    if(portal){
      portal.onclick=ev=>{
        ev.preventDefault();
        toast('Use the Guest Portal button from the main navigation, then enter this Booking ID and your verified email.', false);
      };
    }
  }

  function guest(prefix){
    const root=document.querySelector(stepMap[prefix]);
    const fields=[...root.querySelectorAll('input,select,textarea')];
    return {
      firstName:(fields[0]?.value||'').trim(),
      lastName:(fields[1]?.value||'').trim(),
      email:(fields[2]?.value||'').trim(),
      phone:(fields[3]?.value||'').trim(),
      nationality:(fields[4]?.value||'').trim(),
      validIdType:(fields[5]?.value||'').trim(),
      specialRequests:(fields[6]?.value||'').trim()
    };
  }

  function formatTime12h(raw){
    const value=(raw||'').trim();
    const match=value.match(/^(\d{1,2}):(\d{2})$/);
    if(!match) return value;
    let hour=parseInt(match[1],10);
    const minute=match[2];
    const suffix=hour >= 12 ? 'PM' : 'AM';
    hour = hour % 12;
    if(hour === 0) hour = 12;
    return hour + ':' + minute + ' ' + suffix;
  }

  function toTimeInputValue(raw){
    const value=(raw||'').trim().toUpperCase();
    const match=value.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/);
    if(!match) return '';
    let hour=parseInt(match[1],10);
    const minute=match[2];
    const suffix=match[3];
    if(suffix === 'AM'){
      if(hour === 12) hour = 0;
    }else{
      if(hour !== 12) hour += 12;
    }
    return hour.toString().PadLeft(2,'0') + ':' + minute;
  }

  function exactTime(prefix){
    const input=document.querySelector('[data-booking-time=""'+prefix+'""]');
    return formatTime12h((input?.value||'').trim());
  }

  function selectedSlot(prefix){
    const root=document.querySelector(tabMap[prefix]);
    const slot=root?.querySelector('.slot-grid .slot.selected');
    return (slot?.textContent||'').trim();
  }

  function arrivalDetails(prefix){
    const root=document.querySelector(stepMap[prefix]);
    const divider=[...root.querySelectorAll('.divider-label')].find(x => (x.textContent||'').trim().includes('Arrival'));
    if(!divider) return { arrivalTime:'', transport:'' };
    const block=divider.nextElementSibling;
    const timeInput=block.querySelector('input[type=""time""].f-inp');
    const selects=[...block.querySelectorAll('select.f-sel')];
    return {
      arrivalTime:formatTime12h((timeInput?.value||selects[0]?.value||'').trim()),
      transport:((timeInput ? selects[0] : selects[1])?.value||'').trim()
    };
  }

  function payload(prefix){
    const root=document.querySelector(tabMap[prefix]);
    const arrival=arrivalDetails(prefix);
    const slot=selectedSlot(prefix);
    const manualTime = exactTime(prefix);
    const arrivalTime = manualTime || (prefix === 'cb' || prefix === 'fs' ? arrival.arrivalTime : slot);
    return {
      state: JSON.parse(JSON.stringify(S[prefix] || {})),
      guest: guest(prefix),
      adults: intText(prefix+'-adults',2),
      children: intText(prefix+'-children',0),
      toddlers: intText(prefix+'-toddlers',0),
      visitDate: val('#'+prefix+'-date'),
      checkInDate: val('#'+prefix+'-ci'),
      checkOutDate: val('#'+prefix+'-co'),
      arrivalTime: arrivalTime,
      transport: arrival.transport,
      paymentMethod: text('.pay-card.selected .pay-name', root) || 'Credit / Debit Card'
    };
  }

  let pending = null;
  let busy = false;

  window.showConfirmed = function(prefix){
    if(busy) return;
    busy = true;
    pending = { prefix, payload: payload(prefix) };
    document.querySelectorAll('.btn-confirm').forEach(b=>b.disabled=true);
    toast('Securing your booking and sending verification code...', false);
    window.chrome.webview.postMessage({ action:'confirmBooking', prefix, payload: pending.payload });
  };

  window.chrome.webview.addEventListener('message', async ev => {
    const r = ev.data || {};
    document.querySelectorAll('.btn-confirm').forEach(b=>b.disabled=false);
    busy = false;

    if(!r.success){
      toast(r.message || 'Booking failed. Please check your details.', true);
      return;
    }

    if(r.status === 'verification_required'){
      const code = await askCode(r.message || 'Verification code sent.');
      if(!code){ toast('Booking was not saved because email verification was cancelled.', true); return; }
      busy = true;
      document.querySelectorAll('.btn-confirm').forEach(b=>b.disabled=true);
      window.chrome.webview.postMessage({ action:'verifyAndConfirm', prefix:pending.prefix, payload:pending.payload, code });
      return;
    }

    if(r.status === 'confirmed'){
      const prefix = pending?.prefix || 'cb';
      const bidEl=document.getElementById(prefix+'-bid');
      if(bidEl) bidEl.textContent = r.reservationId || '';
      const conf=document.querySelector('#'+prefix+'-s5 .conf-wrap');
      if(conf && r.qrDataUrl && !conf.querySelector('.wn-real-qr')){
        const qr=document.createElement('img');
        qr.className='wn-real-qr';
        qr.src=r.qrDataUrl;
        qr.alt='WildNest booking QR';
        qr.style.cssText='width:154px;height:154px;border:1px solid rgba(0,0,0,.12);border-radius:14px;padding:8px;background:white;margin:0 auto 18px;display:block';
        conf.insertBefore(qr, conf.querySelector('.bid-box'));
      }
      wireConfirmedActions(prefix, r);
      toast(r.message || 'Booking confirmed.', false);
      goStep(prefix, 5);
    }
  });
})();";
        }
    }
}
