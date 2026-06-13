<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ResetPassword.aspx.vb" Inherits="ResetPassword" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Reset Password – IOCL Fleet Compliance Portal</title>
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
                radial-gradient(ellipse 80% 60% at 20% 20%, rgba(220, 60, 30, 0.10) 0%, transparent 60%),
                radial-gradient(ellipse 60% 50% at 80% 80%, rgba(0, 84, 166, 0.10) 0%, transparent 60%);
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
            width: 100%; max-width: 440px; padding: 20px;
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
            width: 52px; height: 52px; border-radius: 14px;
            background: linear-gradient(135deg, #dc4a1a, #ff6b00);
            box-shadow: 0 8px 24px rgba(220, 74, 26, 0.4);
            margin-bottom: 14px;
        }

        .card-header h1 { font-size: 18px; font-weight: 700; color: #f1f5f9; letter-spacing: -0.01em; }
        .card-header p  { font-size: 12px; color: #64748b; margin-top: 5px; }

        /* Email badge */
        .email-badge {
            background: rgba(255, 107, 0, 0.08);
            border: 1px solid rgba(255, 107, 0, 0.2);
            border-radius: 8px;
            padding: 8px 14px;
            font-size: 12px;
            color: #f59e0b;
            text-align: center;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 6px;
        }

        /* Fields */
        .field { margin-bottom: 16px; }

        .field label {
            display: block; font-size: 10px; font-weight: 700; color: #94a3b8;
            letter-spacing: 0.1em; text-transform: uppercase; margin-bottom: 7px;
        }

        .input-wrap { position: relative; }

        .input-icon {
            position: absolute; left: 13px; top: 50%; transform: translateY(-50%);
            color: #475569; display: flex; align-items: center;
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

        .input-wrap input::placeholder { color: #334155; }

        .input-wrap input:focus {
            border-color: #ff6b00;
            box-shadow: 0 0 0 3px rgba(255, 107, 0, 0.15);
        }

        /* OTP field special styling */
        .otp-input {
            text-align: center !important;
            font-size: 22px !important;
            font-weight: 700 !important;
            letter-spacing: 8px !important;
            padding: 14px !important;
        }

        /* Panels */
        .panel {
            border-radius: 10px; padding: 10px 14px; font-size: 12px;
            margin-bottom: 16px; display: flex; align-items: center; gap: 8px;
        }

        .panel-error  { background: rgba(127,29,29,0.25); border: 1px solid rgba(239,68,68,0.3); color: #fca5a5; }
        .panel-success { background: rgba(6,78,59,0.25); border: 1px solid rgba(34,197,94,0.3); color: #86efac; }

        /* Password strength indicator */
        .strength-bar { height: 4px; border-radius: 4px; margin-top: 6px; background: #1e293b; overflow: hidden; }
        .strength-fill { height: 100%; border-radius: 4px; transition: width 0.3s, background 0.3s; width: 0; }

        /* Buttons */
        .btn-primary {
            width: 100%; margin-top: 8px; padding: 13px;
            background: linear-gradient(135deg, #dc4a1a 0%, #ff6b00 100%);
            border: none; border-radius: 10px;
            font-size: 13px; font-weight: 700; font-family: 'Inter', sans-serif;
            color: white; letter-spacing: 0.04em; text-transform: uppercase;
            cursor: pointer; box-shadow: 0 6px 24px rgba(220, 74, 26, 0.4);
            transition: all 0.2s;
        }

        .btn-primary:hover {
            transform: translateY(-1px);
            box-shadow: 0 10px 28px rgba(220, 74, 26, 0.5);
        }

        .btn-primary:active { transform: translateY(0); }

        .back-link {
            display: block; text-align: center; margin-top: 16px;
            font-size: 11px; color: #475569; text-decoration: none; transition: color 0.2s;
        }

        .back-link:hover { color: #94a3b8; }

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
    <div class="particle" style="left:8%;width:4px;height:4px;background:#ff6b00;animation-duration:18s;animation-delay:0s;"></div>
    <div class="particle" style="left:25%;width:3px;height:3px;background:#0054A6;animation-duration:23s;animation-delay:5s;"></div>
    <div class="particle" style="left:65%;width:5px;height:5px;background:#ff6b00;animation-duration:16s;animation-delay:8s;"></div>
    <div class="particle" style="left:85%;width:3px;height:3px;background:#0054A6;animation-duration:21s;animation-delay:3s;"></div>

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
                        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="white" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                        </svg>
                    </div>
                    <h1>Reset Password</h1>
                    <p>Enter the OTP sent to your registered email address</p>
                </div>

                <!-- Email badge (shown after OTP is sent) -->
                <asp:Panel ID="pnlEmailBadge" runat="server" Visible="false">
                    <div class="email-badge">
                        <svg width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                        <asp:Label ID="lblEmailHint" runat="server"></asp:Label>
                    </div>
                </asp:Panel>

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

                <!-- OTP -->
                <div class="field">
                    <label for="txtOtp">One-Time Password (OTP)</label>
                    <div class="input-wrap">
                        <span class="input-icon">
                            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                            </svg>
                        </span>
                        <asp:TextBox ID="txtOtp" runat="server"
                            placeholder="6-digit OTP"
                            MaxLength="6"
                            autocomplete="one-time-code"
                            onkeypress="return onlyDigits(event)"
                            oninput="this.value=this.value.replace(/\D/g,'').substring(0,6)"
                            CssClass="otp-input">
                        </asp:TextBox>
                    </div>
                </div>

                <!-- New Password -->
                <div class="field">
                    <label for="txtNewPassword">New Password</label>
                    <div class="input-wrap">
                        <span class="input-icon">
                            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                        </span>
                        <asp:TextBox ID="txtNewPassword" runat="server"
                            TextMode="Password"
                            placeholder="Min. 6 characters"
                            oninput="updateStrength(this.value)">
                        </asp:TextBox>
                    </div>
                    <div class="strength-bar">
                        <div class="strength-fill" id="strengthFill"></div>
                    </div>
                </div>

                <!-- Confirm Password -->
                <div class="field">
                    <label for="txtConfirmPassword">Confirm New Password</label>
                    <div class="input-wrap">
                        <span class="input-icon">
                            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                        </span>
                        <asp:TextBox ID="txtConfirmPassword" runat="server"
                            TextMode="Password"
                            placeholder="Re-enter password">
                        </asp:TextBox>
                    </div>
                </div>

                <!-- Submit -->
                <asp:Button ID="btnReset" runat="server"
                    OnClick="btnReset_Click"
                    CssClass="btn-primary"
                    Text="Reset Password" />

                <a href="ForgotPassword.aspx" class="back-link">&larr; Request a new OTP</a>
            </div>

        </div>
    </form>

    <script>
        function onlyDigits(e) {
            var ch = e.key || String.fromCharCode(e.which || e.keyCode);
            if (!/^[0-9]$/.test(ch)) return false;
            return true;
        }

        function updateStrength(val) {
            var fill = document.getElementById('strengthFill');
            if (!fill) return;
            var score = 0;
            if (val.length >= 6)  score++;
            if (val.length >= 10) score++;
            if (/[A-Z]/.test(val)) score++;
            if (/[0-9]/.test(val)) score++;
            if (/[^A-Za-z0-9]/.test(val)) score++;
            var pct = (score / 5 * 100) + '%';
            var color = score <= 1 ? '#ef4444' : score <= 3 ? '#f59e0b' : '#22c55e';
            fill.style.width  = pct;
            fill.style.background = color;
        }
    </script>
</body>
</html>
