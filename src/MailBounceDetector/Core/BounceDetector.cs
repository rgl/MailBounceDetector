// developed by Rui Lopes (ruilopes.com). licensed under MIT.

using MimeKit;

namespace MailBounceDetector
{
    /// <summary>
    /// detects whether a message is a bounce message. It detects qmail and standard
    /// bounces by delegating to QmailBounceDetector and StandardBounceDetector.
    /// </summary>
    public sealed class BounceDetector
    {
        public static BounceDetectResult Detect(MimeMessage message)
        {
            return QmailBounceDetector.Detect(message) ?? StandardBounceDetector.Detect(message);
        }
    }
}
