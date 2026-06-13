<%@ Page Language="VB" AutoEventWireup="false" CodeFile="VerifyEmail.aspx.vb" Inherits="VerifyEmail" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Verify Email – IOCL Fleet Compliance Portal</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        body {
            font-family: 'Inter', sans-serif;
            min-height: 100vh;
            background: #0a0f1e;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
            position: relative;
        }

        body::before {
            content: '';
            position: absolute;
            inset: 0;
            background:
                radial-gradient(ellipse 80% 60% at 30% 30%, rgba(0, 84, 166, 0.12) 0%, transparent 60%),
                radial-gradient(ellipse 60% 50% at 70% 70%, rgba(0, 184, 100, 0.08) 0%, transparent 60%);
            pointer-events: none;
        }

        body::after {
            content: '';
            position: absolute;
            inset: 0;
            background-image:
                linear-gradient(rgba(255,255,255,0.02) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255,255,255,0.02) 1px, transparent 1px);
            background-size: 48px 48px;
            pointer-events: none;
        }

        .wrapper {
            position: relative; z-index: 10;
            width: 100%; max-width: 420px; padding: 20px;
        }

        .logo-strip {
            display: flex; align-items: center; justify-content: center;
            margin-bottom: 28px;
        }

        .logo-box {
            background: #ffffff; border-radius: 16px;
            padding: 12px 24px; display: flex; align-items: center; gap: 14px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.4), 0 0 0 1px rgba(255,255,255,0.08);
        }

        .logo-box img { height: 52px; width: auto; object-fit: contain; }

        .logo-divider {
            width: 1px; height: 40px;
            background: linear-gradient(to bottom, transparent, #d1d5db, transparent);
        }

        .logo-text .org {
            font-size: 11px; font-weight: 800; color: #0054A6;
            letter-spacing: 0.08em; text-transform: uppercase; line-height: 1;
        }

        .logo-text .sub {
            font-size: 9px; font-weight: 600; color: #dc4a1a;
            letter-spacing: 0.12em; text-transform: uppercase; margin-top: 4px; line-height: 1;
        }

        .card {
            background: rgba(15, 23, 42, 0.85);
            border: 1px solid rgba(255,255,255,0.08);
            border-radius: 20px;
            padding: 36px 32px 32px;
            backdrop-filter: blur(24px);
            -webkit-backdrop-filter: blur(24px);
            box-shadow: 0 32px 80px rgba(0,0,0,0.5), inset 0 1px 0 rgba(255,255,255,0.06);
        }

        .card-header { text-align: center; margin-bottom: 24px; }

        .icon-wrap {
            display: inline-flex; align-items: center; justify-content: center;
            width: 56px; height: 56px; border-radius: 14px;
            background: linear-gradient(135deg, #0054A6, #0077cc);
            box-shadow: 0 8px 24px rgba(0, 84, 166, 0.4);
            margin-bottom: 14px;
            animation: pulse-blue 2.5s ease-in-out infinite;
        }

        @keyframes pulse-blue {
            0%, 100% { box-shadow: 0 8px 24px rgba(0, 84, 166, 0.4); }
            50%       { box-shadow: 0 8px 36px rgba(0, 84, 166, 0.7); }
        }

        .card-header h1 { font-size: 18px; font-weight: 700; color: #f1f5f9; letter-spacing: -0.01em; }
        .card-header p  { font-size: 12px; color: #64748b; margin-top: 5px; line-height: 1.6; }

        /* Email hint */
        .info-badge {
            background: rgba(0, 84, 166, 0.1);
            border: 1px solid rgba(0, 84, 166, 0.25);
            border-radius: 10px;
            padding: 10px 14px;
            font-size: 12px;
            color: #7dd3fc;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 8px;
            line-height: 1.5;
        }

        /* OTP digits container */
        .otp-container {
            display: flex;
            gap: 8px;
            justify-content: center;
            margin-bottom: 20px;
        }

        .otp-digit {
            width: 48px; height: 58px;
            background: rgba(2, 6, 23, 0.6);
            border: 1.5px solid rgba(255,255,255,0.08);
            border-radius: 10px;
            font-size: 22px; font-weight: 700;
            color: #e2e8f0; font-family: monospace;
            text-align: center;
            outline: none;
            transition: border-color 0.2s, box-shadow 0.2s;
            caret-color: #0077cc;
        }

        .otp-digit:focus {
            border-color: #0077cc;
            box-shadow: 0 0 0 3px rgba(0, 119, 204, 0.2);
        }

        .otp-digit.filled {
            border-color: rgba(0, 119, 204, 0.5);
        }

        /* Hidden combined OTP field for server submit */
        .hidden { display: none; }

        /* Panels */
        .panel {
            border-radius: 10px; padding: 10px 14px; font-size: 12px;
            margin-bottom: 16px; display: flex; align-items: center; gap: 8px;
        }

        .panel-error   { background: rgba(127,29,29,0.25);  border: 1px solid rgba(239,68,68,0.3);  color: #fca5a5; }
        .panel-success { background: rgba(6,78,59,0.25);    border: 1px solid rgba(34,197,94,0.3);   color: #86efac; }

        /* Buttons */
        .btn-primary {
            width: 100%; padding: 13px;
            background: linear-gradient(135deg, #0054A6 0%, #0077cc 100%);
            border: none; border-radius: 10px;
            font-size: 13px; font-weight: 700; font-family: 'Inter', sans-serif;
            color: white; letter-spacing: 0.04em; text-transform: uppercase;
            cursor: pointer; box-shadow: 0 6px 24px rgba(0, 84, 166, 0.4);
            transition: all 0.2s;
        }

        .btn-primary:hover {
            transform: translateY(-1px);
            box-shadow: 0 10px 28px rgba(0, 84, 166, 0.5);
        }

        .btn-primary:active { transform: translateY(0); }

        .resend-row {
            display: flex; align-items: center; justify-content: center;
            margin-top: 16px; gap: 6px;
        }

        .resend-hint { font-size: 11px; color: #475569; }

        .btn-resend {
            font-size: 11px; font-weight: 700; color: #0077cc;
            background: none; border: none; cursor: pointer;
            font-family: 'Inter', sans-serif; padding: 0;
            text-decoration: underline; text-underline-offset: 2px;
            transition: color 0.2s;
        }

        .btn-resend:hover { color: #38bdf8; }
        .btn-resend:disabled { color: #334155; cursor: not-allowed; text-decoration: none; }

        .countdown { font-size: 11px; color: #f59e0b; font-weight: 600; }

        .particle {
            position: absolute; border-radius: 50%; pointer-events: none;
            animation: float linear infinite; opacity: 0;
        }

        @keyframes float {
            0%   { transform: translateY(100vh) scale(0); opacity: 0; }
            10%  { opacity: 0.3; }
            90%  { opacity: 0.15; }
            100% { transform: translateY(-100px) scale(1); opacity: 0; }
        }
    </style>
</head>
<body>
    <div class="particle" style="left:12%;width:4px;height:4px;background:#0077cc;animation-duration:19s;animation-delay:0s;"></div>
    <div class="particle" style="left:35%;width:3px;height:3px;background:#22c55e;animation-duration:24s;animation-delay:6s;"></div>
    <div class="particle" style="left:68%;width:5px;height:5px;background:#0077cc;animation-duration:16s;animation-delay:10s;"></div>
    <div class="particle" style="left:88%;width:3px;height:3px;background:#22c55e;animation-duration:22s;animation-delay:3s;"></div>

    <form id="form1" runat="server">
        <div class="wrapper">

            <!-- Logo -->
            <div class="logo-strip">
                <div class="logo-box">
                    <img src="https://iocl.com/assets/images/logo.gif" alt="IOCL" />
                    <div class="logo-divider"></div>
                    <div class="logo-text">
                        <div class="org">IOCL</div>
                        <div class="sub">Panipat Refinery</div>
                    </div>
                </div>
            </div>

            <!-- Card -->
            <div class="card">
                <div class="card-header">
                    <div class="icon-wrap">
                        <svg width="26" height="26" fill="none" viewBox="0 0 24 24" stroke="white" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                    </div>
                    <h1>Verify Your Email</h1>
                    <p>A 6-digit OTP was sent to your registered email.<br>Enter it below to activate your account.</p>
                </div>

                <!-- Info badge -->
                <div class="info-badge">
                    <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" style="flex-shrink:0;">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <span>Check your inbox (and spam folder). The OTP expires in <strong style="color:#f59e0b;">15 minutes</strong>.</span>
                </div>

                <!-- Error Panel -->
                <asp:Panel ID="pnlError" runat="server" Visible="false">
                    <div class="panel panel-error">
                        <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" style="flex-shrink:0;">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <asp:Label ID="lblError" runat="server"></asp:Label>
                    </div>
                </asp:Panel>

                <!-- Success Panel -->
                <asp:Panel ID="pnlSuccess" runat="server" Visible="false">
                    <div class="panel panel-success">
                        <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" style="flex-shrink:0;">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <asp:Label ID="lblSuccess" runat="server"></asp:Label>
                    </div>
                </asp:Panel>

                <!-- 6-box OTP input -->
                <div class="otp-container" id="otpBoxes">
                    <input class="otp-digit" id="d1" type="tel" maxlength="1" inputmode="numeric" autocomplete="one-time-code" />
                    <input class="otp-digit" id="d2" type="tel" maxlength="1" inputmode="numeric" />
                    <input class="otp-digit" id="d3" type="tel" maxlength="1" inputmode="numeric" />
                    <input class="otp-digit" id="d4" type="tel" maxlength="1" inputmode="numeric" />
                    <input class="otp-digit" id="d5" type="tel" maxlength="1" inputmode="numeric" />
                    <input class="otp-digit" id="d6" type="tel" maxlength="1" inputmode="numeric" />
                </div>

                <!-- Hidden combined OTP for form post -->
                <asp:TextBox ID="txtOtp" runat="server" CssClass="hidden"></asp:TextBox>

                <!-- Verify Button -->
                <asp:Button ID="btnVerify" runat="server"
                    OnClick="btnVerify_Click"
                    CssClass="btn-primary"
                    Text="Verify &amp; Enter Portal"
                    OnClientClick="return combineOtp();" />

                <!-- Resend row -->
                <div class="resend-row">
                    <span class="resend-hint">Didn't receive it?</span>
                    <asp:Button ID="btnResend" runat="server"
                        OnClick="btnResend_Click"
                        CssClass="btn-resend"
                        Text="Resend OTP"
                        CausesValidation="false"
                        OnClientClick="return combineOtp();" />
                    <span class="countdown" id="countdown" style="display:none;"></span>
                </div>
            </div>

        </div>
    </form>

    <script>
        var digits = ['d1','d2','d3','d4','d5','d6'];

        // Wire up digit inputs: auto-advance, auto-backspace, paste support
        digits.forEach(function (id, idx) {
            var el = document.getElementById(id);
            el.addEventListener('input', function () {
                this.value = this.value.replace(/\D/g, '').slice(-1);
                this.classList.toggle('filled', this.value.length > 0);
                if (this.value && idx < digits.length - 1) {
                    document.getElementById(digits[idx + 1]).focus();
                }
            });
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !this.value && idx > 0) {
                    document.getElementById(digits[idx - 1]).focus();
                }
                if (e.key === 'Enter') {
                    combineOtp();
                    document.getElementById('<%= btnVerify.ClientID %>').click();
                }
            });
        });

        // Handle paste of a 6-digit OTP
        document.getElementById('d1').addEventListener('paste', function (e) {
            e.preventDefault();
            var data = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
            data.split('').forEach(function (ch, i) {
                if (i < digits.length) {
                    var el = document.getElementById(digits[i]);
                    el.value = ch;
                    el.classList.add('filled');
                }
            });
            var last = Math.min(data.length, digits.length) - 1;
            if (last >= 0) document.getElementById(digits[last]).focus();
        });

        function combineOtp() {
            var val = digits.map(function(id) { return document.getElementById(id).value; }).join('');
            document.getElementById('<%= txtOtp.ClientID %>').value = val;
            return true;
        }

        // ── Resend cooldown ───────────────────────────────────────────────────
        var COOLDOWN_SECONDS = 120;
        var cooldownEnd = null;

        function startCooldown() {
            cooldownEnd = Date.now() + COOLDOWN_SECONDS * 1000;
            var btn = document.getElementById('<%= btnResend.ClientID %>');
            var cd  = document.getElementById('countdown');
            btn.disabled = true;
            cd.style.display = 'inline';
            tick();
        }

        function tick() {
            var remaining = Math.ceil((cooldownEnd - Date.now()) / 1000);
            var cd  = document.getElementById('countdown');
            var btn = document.getElementById('<%= btnResend.ClientID %>');
            if (remaining <= 0) {
                btn.disabled = false;
                cd.style.display = 'none';
                cd.textContent = '';
            } else {
                cd.textContent = '(' + remaining + 's)';
                setTimeout(tick, 1000);
            }
        }

        // If page loaded because of a resend postback, start cooldown
        <% If Session("OtpResentAt") IsNot Nothing Then %>
        startCooldown();
        <% End If %>
    </script>
</body>
</html>
