using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Project.Accomodations;

namespace Project.Booking
{
    public partial class FullStayExperience
    {
        private TextBox _txtEmailVerificationCode = null!;
        private Button _btnSendEmailCode = null!;
        private Button _btnVerifyEmailCode = null!;
        private Label _lblEmailVerification = null!;
        private bool _emailVerified;
        private string _verifiedEmail = string.Empty;
        private string _pendingEmailCode = string.Empty;
        private string _pendingEmail = string.Empty;
        private DateTime _pendingEmailExpiryUtc = DateTime.MinValue;
        private int _emailVerificationAttempts;
        private DateTime _lastVerificationSentUtc = DateTime.MinValue;

        private int AddEmailVerificationBlock(Panel body, int y, int width)
        {
            var panel = new Panel { Location = new Point(0, y), Size = new Size(width, 136), BackColor = Color.Transparent };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8);
                using var fill = new SolidBrush(Color.FromArgb(10, 212, 160, 23));
                using var pen = new Pen(Color.FromArgb(46, 212, 160, 23), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            };

            panel.Controls.Add(new Label { Text = "EMAIL VERIFICATION", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(14, 10), BackColor = Color.Transparent });
            panel.Controls.Add(new Label { Text = "A 6-digit code will be sent to this email before your booking can continue.", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(14, 28), BackColor = Color.Transparent });

            _btnSendEmailCode = BookingFlowTheme.CreateSecondaryButton("Send Code");
            _btnSendEmailCode.Size = new Size(132, 34);
            _btnSendEmailCode.Location = new Point(14, 52);
            _btnSendEmailCode.Click += async (s, e) => await SendVerificationCodeAsync();
            panel.Controls.Add(_btnSendEmailCode);

            _txtEmailVerificationCode = new TextBox { Location = new Point(158, 52), Size = new Size(120, 34), Font = new Font("Segoe UI", 10f, FontStyle.Bold), MaxLength = 6, TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.FixedSingle, BackColor = BookingFlowTheme.Cream };
            _txtEmailVerificationCode.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            panel.Controls.Add(_txtEmailVerificationCode);

            _btnVerifyEmailCode = BookingFlowTheme.CreatePrimaryButton("Verify");
            _btnVerifyEmailCode.Size = new Size(112, 34);
            _btnVerifyEmailCode.Location = new Point(290, 52);
            _btnVerifyEmailCode.Click += (s, e) => VerifyEmailCode();
            panel.Controls.Add(_btnVerifyEmailCode);

            _lblEmailVerification = new Label { Text = "Verification required before you can continue.", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = false, Size = new Size(width - 28, 42), Location = new Point(14, 90), BackColor = Color.Transparent };
            panel.Controls.Add(_lblEmailVerification);

            body.Controls.Add(panel);
            return panel.Height;
        }

        private async Task SendVerificationCodeAsync()
        {
            if (!EmailSecurity.TryNormalizeAndValidate(_txtEmail.Text, out var normalizedEmail, out var error))
            {
                MessageBox.Show(error, "Invalid Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var secondsUntilResend = EmailSecurity.ResendCooldownSeconds - (int)(DateTime.UtcNow - _lastVerificationSentUtc).TotalSeconds;
            if (secondsUntilResend > 0)
            {
                _lblEmailVerification.Text = $"Please wait {secondsUntilResend}s before requesting another code.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.TextMuted;
                MessageBox.Show($"Please wait {secondsUntilResend} seconds before requesting another code.", "Verification Cooldown", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ResetEmailVerificationState(clearStatus: false);
            _pendingEmailCode = EmailSecurity.CreateVerificationCode();
            _pendingEmail = normalizedEmail;
            _pendingEmailExpiryUtc = DateTime.UtcNow.AddMinutes(EmailSecurity.VerificationCodeValidMinutes);
            _emailVerificationAttempts = 0;
            _lastVerificationSentUtc = DateTime.UtcNow;

            int previousBookings = GetExistingBookingCount(normalizedEmail);
            _btnSendEmailCode.Enabled = false;
            _btnVerifyEmailCode.Enabled = false;
            _lblEmailVerification.Text = $"Sending code to {EmailSecurity.MaskEmail(normalizedEmail)}...";
            _lblEmailVerification.ForeColor = BookingFlowTheme.TextMuted;

            var guestName = string.IsNullOrWhiteSpace(_txtFirst.Text) ? "WildNest Guest" : _txtFirst.Text.Trim();
            var result = await Task.Run(() => EmailService.SendVerificationCode(normalizedEmail, guestName, _pendingEmailCode, EmailSecurity.VerificationCodeValidMinutes));

            _btnSendEmailCode.Enabled = true;

            if (!result.Success)
            {
                _pendingEmailCode = string.Empty;
                _pendingEmail = string.Empty;
                _pendingEmailExpiryUtc = DateTime.MinValue;
                _lastVerificationSentUtc = DateTime.MinValue;
                _lblEmailVerification.Text = "Could not send verification code. Please check the email and try again.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.Danger;
                MessageBox.Show("Verification email failed: " + result.Message, "Email Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnVerifyEmailCode.Enabled = true;
            _lblEmailVerification.Text = previousBookings > 0
                ? $"Code sent to {EmailSecurity.MaskEmail(normalizedEmail)}. This email already has {previousBookings} booking(s), which is allowed if it belongs to you."
                : $"Code sent to {EmailSecurity.MaskEmail(normalizedEmail)}. Enter it within {EmailSecurity.VerificationCodeValidMinutes} minutes.";
            _lblEmailVerification.ForeColor = BookingFlowTheme.Success;
            _txtEmailVerificationCode.Focus();
        }

        private void VerifyEmailCode()
        {
            if (!EmailSecurity.TryNormalizeAndValidate(_txtEmail.Text, out var normalizedEmail, out var error))
            {
                MessageBox.Show(error, "Invalid Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (normalizedEmail != _pendingEmail || string.IsNullOrWhiteSpace(_pendingEmailCode))
            {
                MessageBox.Show("Please send a fresh verification code for this email address first.", "Verification Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (DateTime.UtcNow > _pendingEmailExpiryUtc)
            {
                ResetEmailVerificationState(clearStatus: false);
                _lblEmailVerification.Text = "Verification code expired. Please request a new one.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.Danger;
                MessageBox.Show("Your verification code has expired. Please request a new code.", "Verification Expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var enteredCode = _txtEmailVerificationCode.Text.Trim();
            if (enteredCode.Length != 6)
            {
                _lblEmailVerification.Text = "Enter the complete 6-digit verification code.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.Danger;
                MessageBox.Show("Please enter the complete 6-digit code.", "Verification Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.Equals(enteredCode, _pendingEmailCode, StringComparison.Ordinal))
            {
                _emailVerificationAttempts++;
                if (_emailVerificationAttempts >= EmailSecurity.MaxVerificationAttempts)
                {
                    ResetEmailVerificationState(clearStatus: false);
                    _lblEmailVerification.Text = "Too many incorrect attempts. Please request a new code.";
                    _lblEmailVerification.ForeColor = BookingFlowTheme.Danger;
                    MessageBox.Show("Too many incorrect attempts. Please request a new verification code.", "Verification Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int remaining = EmailSecurity.MaxVerificationAttempts - _emailVerificationAttempts;
                _lblEmailVerification.Text = $"Verification code did not match. {remaining} attempt(s) left.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.Danger;
                MessageBox.Show("Incorrect verification code.", "Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _emailVerified = true;
            _verifiedEmail = normalizedEmail;
            _pendingEmailCode = string.Empty;
            _pendingEmail = string.Empty;
            _pendingEmailExpiryUtc = DateTime.MinValue;
            _emailVerificationAttempts = 0;
            _lblEmailVerification.Text = $"Email verified successfully for {EmailSecurity.MaskEmail(_verifiedEmail)}.";
            _lblEmailVerification.ForeColor = BookingFlowTheme.Success;
            _btnVerifyEmailCode.Enabled = false;
            _btnSendEmailCode.Enabled = false;
            _btnSendEmailCode.Text = "Verified";
        }

        private void ResetEmailVerificationState(bool clearStatus = true)
        {
            _emailVerified = false;
            _verifiedEmail = string.Empty;
            _pendingEmailCode = string.Empty;
            _pendingEmail = string.Empty;
            _pendingEmailExpiryUtc = DateTime.MinValue;
            _emailVerificationAttempts = 0;
            if (_txtEmailVerificationCode != null) _txtEmailVerificationCode.Clear();
            if (_btnSendEmailCode != null)
            {
                _btnSendEmailCode.Enabled = true;
                _btnSendEmailCode.Text = "Send Code";
            }
            if (_btnVerifyEmailCode != null) _btnVerifyEmailCode.Enabled = false;
            if (clearStatus && _lblEmailVerification != null)
            {
                _lblEmailVerification.Text = "Verification required before you can continue.";
                _lblEmailVerification.ForeColor = BookingFlowTheme.TextMuted;
            }
        }

        private bool EnsureGuestVerificationReady()
        {
            if (!EmailSecurity.TryNormalizeAndValidate(_txtEmail.Text, out var normalizedEmail, out var error))
            {
                MessageBox.Show(error, "Invalid Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!_emailVerified || !string.Equals(_verifiedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please verify your email address before continuing.", "Verification Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private int GetExistingBookingCount(string normalizedEmail)
        {
            try
            {
                using var conn = new MySqlConnection(_connStr);
                conn.Open();
                using var cmd = new MySqlCommand(@"
SELECT COUNT(DISTINCT r.ReservationID)
FROM tbl_Guests g
LEFT JOIN tbl_Reservations r ON r.GuestID = g.GuestID
WHERE LOWER(g.Email) = @email;", conn);
                cmd.Parameters.AddWithValue("@email", normalizedEmail);
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
            catch
            {
                return 0;
            }
        }
    }
}
