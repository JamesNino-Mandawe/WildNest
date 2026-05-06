using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Project
{
    internal static class EmailSecurity
    {
        public const int VerificationCodeValidMinutes = 3;
        public const int ResendCooldownSeconds = 45;
        public const int MaxVerificationAttempts = 5;

        private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "mailinator.com",
            "guerrillamail.com",
            "guerrillamail.net",
            "10minutemail.com",
            "temp-mail.org",
            "tempmail.com",
            "tempmailo.com",
            "yopmail.com",
            "yopmail.fr",
            "sharklasers.com",
            "getnada.com",
            "throwawaymail.com",
            "dispostable.com",
            "fakeinbox.com",
            "trashmail.com",
            "maildrop.cc",
            "emailondeck.com",
            "moakt.com",
            "mail.tm",
            "spamgourmet.com",
            "fake-mail.net",
            "getairmail.com",
            "generator.email",
            "inboxkitten.com"
        };

        public static bool TryNormalizeAndValidate(string rawEmail, out string normalizedEmail, out string error)
        {
            normalizedEmail = (rawEmail ?? string.Empty).Trim().ToLowerInvariant();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                error = "Email address is required.";
                return false;
            }

            if (normalizedEmail.Length > 254 || normalizedEmail.Contains(" "))
            {
                error = "Please enter a valid email address.";
                return false;
            }

            try
            {
                var parsed = new MailAddress(normalizedEmail);
                if (!string.Equals(parsed.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    error = "Please enter a valid email address.";
                    return false;
                }
            }
            catch
            {
                error = "Please enter a valid email address.";
                return false;
            }

            var at = normalizedEmail.LastIndexOf('@');
            if (at <= 0 || at >= normalizedEmail.Length - 3)
            {
                error = "Please enter a valid email address.";
                return false;
            }

            var domain = normalizedEmail[(at + 1)..];
            if (!domain.Contains('.') ||
                domain.StartsWith('.') ||
                domain.EndsWith('.') ||
                domain.Contains(".."))
            {
                error = "Email domain looks incomplete.";
                return false;
            }

            var labels = domain.Split('.');
            if (labels.Any(label => label.Length == 0 || label.StartsWith("-") || label.EndsWith("-")))
            {
                error = "Email domain looks incomplete.";
                return false;
            }

            if (DisposableDomains.Contains(domain))
            {
                error = "Temporary or disposable email addresses are not allowed for booking.";
                return false;
            }

            return true;
        }

        public static string CreateVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        public static string MaskEmail(string email)
        {
            if (!TryNormalizeAndValidate(email, out var normalized, out _))
                return email ?? string.Empty;

            var at = normalized.IndexOf('@');
            if (at <= 1)
                return normalized;

            var local = normalized[..at];
            var domain = normalized[at..];
            var visible = Math.Min(2, local.Length);
            return local[..visible] + new string('*', Math.Max(1, local.Length - visible)) + domain;
        }
    }
}
