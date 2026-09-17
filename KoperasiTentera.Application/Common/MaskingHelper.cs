namespace KoperasiTentera.Application.Common;

/// <summary>
/// Server-side masking of contact details so full mobile numbers/emails are
/// never returned to the client before OTP verification (per design requirement).
/// </summary>
public static class MaskingHelper
{
    public static string MaskMobile(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length < 4)
        {
            return "••••";
        }

        string last4 = mobileNumber[^4..];
        return $"•• •• ••• {last4}";
    }

    public static string MaskEmail(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "•••••@•••••.com";
        }

        string localPart = email[..atIndex];
        string firstTwo = localPart.Length >= 2 ? localPart[..2] : localPart;
        return $"{firstTwo}•••@•••••.com";
    }
}
