<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Login.aspx.vb" Inherits="Login" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>IOCL Fleet Compliance System - Login</title>
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

        /* Animated background */
        body::before {
            content: '';
            position: absolute;
            inset: 0;
            background:
                radial-gradient(ellipse 80% 60% at 20% 20%, rgba(220, 60, 30, 0.12) 0%, transparent 60%),
                radial-gradient(ellipse 60% 50% at 80% 80%, rgba(0, 84, 166, 0.12) 0%, transparent 60%);
            pointer-events: none;
        }

        /* Subtle grid */
        body::after {
            content: '';
            position: absolute;
            inset: 0;
            background-image:
                linear-gradient(rgba(255,255,255,0.025) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255,255,255,0.025) 1px, transparent 1px);
            background-size: 48px 48px;
            pointer-events: none;
        }

        .login-wrapper {
            position: relative;
            z-index: 10;
            width: 100%;
            max-width: 420px;
            padding: 20px;
        }

        /* Top logo strip */
        .logo-strip {
            display: flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 28px;
        }

        .logo-box {
            background: #ffffff;
            border-radius: 16px;
            padding: 12px 24px;
            display: flex;
            align-items: center;
            gap: 14px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.4), 0 0 0 1px rgba(255,255,255,0.08);
        }

        .logo-box img {
            height: 52px;
            width: auto;
            object-fit: contain;
        }

        .logo-divider {
            width: 1px;
            height: 40px;
            background: linear-gradient(to bottom, transparent, #d1d5db, transparent);
        }

        .logo-text {
            text-align: left;
        }

        .logo-text .org {
            font-size: 11px;
            font-weight: 800;
            color: #0054A6;
            letter-spacing: 0.08em;
            text-transform: uppercase;
            line-height: 1;
        }

        .logo-text .sub {
            font-size: 9px;
            font-weight: 600;
            color: #dc4a1a;
            letter-spacing: 0.12em;
            text-transform: uppercase;
            margin-top: 4px;
            line-height: 1;
        }

        /* Card */
        .card {
            background: rgba(15, 23, 42, 0.85);
            border: 1px solid rgba(255,255,255,0.08);
            border-radius: 20px;
            padding: 36px 32px 32px;
            backdrop-filter: blur(24px);
            -webkit-backdrop-filter: blur(24px);
            box-shadow:
                0 32px 80px rgba(0,0,0,0.5),
                0 0 0 1px rgba(255,255,255,0.04),
                inset 0 1px 0 rgba(255,255,255,0.06);
        }

        .card-header {
            text-align: center;
            margin-bottom: 28px;
        }

        .shield-icon {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 52px;
            height: 52px;
            border-radius: 14px;
            background: linear-gradient(135deg, #dc4a1a, #ff6b00);
            box-shadow: 0 8px 24px rgba(220, 74, 26, 0.4);
            margin-bottom: 14px;
        }

        .card-header h1 {
            font-size: 18px;
            font-weight: 700;
            color: #f1f5f9;
            letter-spacing: -0.01em;
        }

        .card-header p {
            font-size: 12px;
            color: #64748b;
            margin-top: 5px;
            font-weight: 400;
        }

        /* Form fields */
        .field {
            margin-bottom: 16px;
        }

        .field label {
            display: block;
            font-size: 10px;
            font-weight: 700;
            color: #94a3b8;
            letter-spacing: 0.1em;
            text-transform: uppercase;
            margin-bottom: 7px;
        }

        .input-wrap {
            position: relative;
        }

        .input-icon {
            position: absolute;
            left: 13px;
            top: 50%;
            transform: translateY(-50%);
            color: #475569;
            display: flex;
            align-items: center;
        }

        .input-wrap input {
            width: 100%;
            background: rgba(2, 6, 23, 0.6);
            border: 1.5px solid rgba(255,255,255,0.08);
            border-radius: 10px;
            padding: 11px 14px 11px 40px;
            font-size: 14px;
            font-family: 'Inter', sans-serif;
            color: #e2e8f0;
            outline: none;
            transition: border-color 0.2s, box-shadow 0.2s;
        }

        .input-wrap input::placeholder {
            color: #334155;
        }

        .input-wrap input:focus {
            border-color: #ff6b00;
            box-shadow: 0 0 0 3px rgba(255, 107, 0, 0.15);
        }

        /* Error panel */
        .error-panel {
            background: rgba(127, 29, 29, 0.25);
            border: 1px solid rgba(239, 68, 68, 0.3);
            border-radius: 10px;
            padding: 10px 14px;
            font-size: 12px;
            color: #fca5a5;
            margin-bottom: 16px;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        /* Login button */
        .btn-login {
            width: 100%;
            margin-top: 8px;
            padding: 13px;
            background: linear-gradient(135deg, #dc4a1a 0%, #ff6b00 100%);
            border: none;
            border-radius: 10px;
            font-size: 13px;
            font-weight: 700;
            font-family: 'Inter', sans-serif;
            color: white;
            letter-spacing: 0.04em;
            text-transform: uppercase;
            cursor: pointer;
            box-shadow: 0 6px 24px rgba(220, 74, 26, 0.4);
            transition: all 0.2s;
        }

        .btn-login:hover {
            transform: translateY(-1px);
            box-shadow: 0 10px 28px rgba(220, 74, 26, 0.5);
            background: linear-gradient(135deg, #c43d12 0%, #e85f00 100%);
        }

        .btn-login:active {
            transform: translateY(0);
        }

        /* Footer */
        .footer-note {
            text-align: center;
            margin-top: 18px;
            font-size: 10px;
            color: #334155;
            letter-spacing: 0.04em;
        }

        /* Floating particle dots */
        .particle {
            position: absolute;
            border-radius: 50%;
            pointer-events: none;
            animation: float linear infinite;
            opacity: 0;
        }

        @keyframes float {
            0%   { transform: translateY(100vh) scale(0); opacity: 0; }
            10%  { opacity: 0.4; }
            90%  { opacity: 0.2; }
            100% { transform: translateY(-100px) scale(1); opacity: 0; }
        }
    </style>
</head>
<body>
    <!-- Floating particles -->
    <div class="particle" style="left:8%;width:4px;height:4px;background:#ff6b00;animation-duration:18s;animation-delay:0s;"></div>
    <div class="particle" style="left:22%;width:3px;height:3px;background:#0054A6;animation-duration:22s;animation-delay:3s;"></div>
    <div class="particle" style="left:55%;width:5px;height:5px;background:#ff6b00;animation-duration:15s;animation-delay:7s;"></div>
    <div class="particle" style="left:75%;width:3px;height:3px;background:#0054A6;animation-duration:20s;animation-delay:2s;"></div>
    <div class="particle" style="left:90%;width:4px;height:4px;background:#ff6b00;animation-duration:17s;animation-delay:10s;"></div>

    <form id="form1" runat="server">
        <div class="login-wrapper">

            <!-- IOCL Logo Strip -->
            <div class="logo-strip">
                <div class="logo-box">
                    <img src="https://iocl.com/assets/images/logo.gif" alt="Indian Oil Corporation Limited" />
                    <div class="logo-divider"></div>
                    <div class="logo-text">
                        <div class="sub">Panipat Refinery</div>
                    </div>
                </div>
            </div>

            <!-- Login Card -->
            <div class="card">
                <div class="card-header">
                    <div class="shield-icon">
                        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="white" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                        </svg>
                    </div>
                    <h1>Fleet Compliance Portal</h1>
                    <p>Sign in with your Employee credentials</p>
                </div>

                <!-- Error Panel -->
                <asp:Panel ID="pnlError" runat="server" Visible="false">
                    <div class="error-panel">
                        <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" style="flex-shrink:0;">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <asp:Label ID="lblError" runat="server"></asp:Label>
                    </div>
                </asp:Panel>

                <!-- Employee Number -->
                <div class="field">
                    <label for="txtUsername" style="display:flex;justify-content:space-between;align-items:center;">
                        <span>Employee Number</span>
                        <span id="empCounter" style="font-size:9px;font-weight:600;color:#475569;letter-spacing:0.05em;">0 / 8</span>
                    </label>
                    <div class="input-wrap">
                        <span class="input-icon">
                            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                            </svg>
                        </span>
                        <asp:TextBox ID="txtUsername" runat="server"
                            placeholder="8-digit Emp. No."
                            autocomplete="off"
                            MaxLength="8"
                            onkeypress="return onlyDigits(event)"
                            oninput="updateCounter(this)">
                        </asp:TextBox>
                    </div>
                </div>

                <!-- Password -->
                <div class="field">
                    <label for="txtPassword">Password</label>
                    <div class="input-wrap">
                        <span class="input-icon">
                            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                        </span>
                        <asp:TextBox ID="txtPassword" runat="server"
                            TextMode="Password"
                            placeholder="Enter Password">
                        </asp:TextBox>
                    </div>
                </div>

                <!-- Submit -->
                <asp:Button ID="btnLogin" runat="server"
                    OnClick="btnLogin_Click"
                    CssClass="btn-login"
                    Text="Sign In" />

                <div class="footer-note">
                    IOCL Fleet Compliance &nbsp;|&nbsp; Panipat Refinery
                </div>
            </div>

        </div>
    </form>

    <script>
        // Only allow numeric digits
        function onlyDigits(e) {
            var ch = e.key || String.fromCharCode(e.which || e.keyCode);
            if (!/^[0-9]$/.test(ch)) return false;
            return true;
        }

        // Live digit counter
        function updateCounter(input) {
            // Strip any non-digit characters pasted in
            input.value = input.value.replace(/\D/g, '').substring(0, 8);
            var len = input.value.length;
            var counter = document.getElementById('empCounter');
            if (counter) {
                counter.textContent = len + ' / 8';
                counter.style.color = len === 8 ? '#22c55e' : (len >= 6 ? '#f59e0b' : '#475569');
            }
        }

        // Submit on Enter key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                var btn = document.getElementById('<%= btnLogin.ClientID %>');
                if (btn) btn.click();
            }
        });
    </script>
</body>
</html>
