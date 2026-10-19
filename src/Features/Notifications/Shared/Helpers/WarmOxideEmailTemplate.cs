namespace ShapeUp.Features.Notifications.Shared.Helpers;

using System.Net;
using System.Text;

/// <summary>Self-contained HTML (Warm Oxide, same look as the invitation e-mail) for transactional e-mails. All text is HTML-encoded.</summary>
public static class WarmOxideEmailTemplate
{
    public static string Render(string eyebrow, string title, IEnumerable<string> paragraphs, string? highlightLabel = null, string? highlight = null,
        string? ctaText = null, string? ctaUrl = null, string footer = "Você recebeu esta mensagem porque tem uma conta ou um convite no ShapeUp.")
    {
        var body = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            body.Append("<tr><td align=\"left\" style=\"padding-bottom:14px;font-family:'Source Sans 3','Segoe UI',Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#D9CFC7;\">")
                .Append(Enc(paragraph))
                .Append("</td></tr>");
        }

        if (!string.IsNullOrWhiteSpace(highlight))
        {
            body.Append("<tr><td style=\"padding-bottom:14px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>")
                .Append("<td bgcolor=\"#2B221E\" style=\"background-color:#2B221E;border-left:4px solid #D4A359;border-radius:8px;padding:14px 16px;font-family:'Source Sans 3','Segoe UI',Arial,Helvetica,sans-serif;font-size:15px;line-height:22px;color:#F5EDE6;\">");
            if (!string.IsNullOrWhiteSpace(highlightLabel))
                body.Append("<div style=\"font-size:12px;letter-spacing:1.5px;font-weight:bold;color:#D4A359;padding-bottom:4px;\">")
                    .Append(Enc(highlightLabel.ToUpperInvariant())).Append("</div>");
            body.Append(Enc(highlight)).Append("</td></tr></table></td></tr>");
        }

        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            body.Append("<tr><td align=\"left\" style=\"padding-top:8px;padding-bottom:14px;\"><a href=\"")
                .Append(Enc(ctaUrl))
                .Append("\" style=\"display:inline-block;background-color:#E06C43;color:#18120F;font-family:'Source Sans 3','Segoe UI',Arial,Helvetica,sans-serif;font-size:16px;font-weight:bold;text-decoration:none;border-radius:10px;padding:14px 28px;\">")
                .Append(Enc(ctaText)).Append("</a></td></tr>");
        }

        return $$"""
<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>{{Enc(title)}}</title></head>
<body bgcolor="#18120F" style="margin:0;padding:0;background-color:#18120F;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="#18120F" style="background-color:#18120F;"><tr>
<td align="center" style="padding:32px 16px;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;">
<tr><td align="center" style="padding-bottom:24px;font-family:'Barlow Condensed','Arial Narrow',Arial,Helvetica,sans-serif;font-size:32px;line-height:36px;font-weight:bold;letter-spacing:1px;color:#E06C43;">ShapeUp</td></tr>
<tr><td bgcolor="#211A17" style="background-color:#211A17;border-radius:16px;border-top:4px solid #E06C43;padding:36px 32px;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
<tr><td align="left" style="padding-bottom:12px;font-family:'Source Sans 3','Segoe UI',Arial,Helvetica,sans-serif;font-size:13px;line-height:18px;font-weight:bold;letter-spacing:2px;color:#D4A359;">{{Enc(eyebrow.ToUpperInvariant())}}</td></tr>
<tr><td align="left" style="padding-bottom:16px;font-family:'Barlow Condensed','Arial Narrow',Arial,Helvetica,sans-serif;font-size:30px;line-height:36px;font-weight:bold;color:#F5EDE6;">{{Enc(title)}}</td></tr>
{{body}}
</table></td></tr>
<tr><td align="center" style="padding-top:20px;font-family:'Source Sans 3','Segoe UI',Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#8F837B;">{{Enc(footer)}}</td></tr>
</table></td></tr></table></body></html>
""";
    }

    /// <summary>Escapes markup only; accented letters stay readable (the message is sent as UTF-8).</summary>
    private static string Enc(string value) => value
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
}
