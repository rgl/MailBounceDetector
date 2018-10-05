// developed by Rui Lopes (ruilopes.com). licensed under MIT.

using MimeKit;
using System.IO;
using Xunit;

namespace MailBounceDetector.Tests
{
    public class BounceDetectorTests
    {
        [Fact]
        public void FailedTestForTestingJenkinsXunitPlugin()
        {
            Assert.True(false);
        }

        [Fact]
        public void NonBouncePostfix()
        {
            var message = MimeMessage.Load(OpenFixture("non_bounce_postfix_hello_world.eml"));

            var result = BounceDetector.Detect(message);

            Assert.False(result.IsBounce);
            Assert.False(result.IsHard);
            Assert.False(result.IsSoft);
        }

        [Fact]
        public void ExchangeNonExistingMailbox()
        {
            var message = MimeMessage.Load(OpenFixture("bounce_exchange_non_existing_mailbox.eml"));

            var result = BounceDetector.Detect(message);

            Assert.True(result.IsBounce);
            Assert.True(result.IsHard);
            Assert.False(result.IsSoft);
            Assert.Equal("5 Permanent Failure", result.PrimaryStatus.ToString());
            Assert.Equal("1 Addressing Status", result.SecundaryStatus.ToString());
            Assert.Equal("11 Bad destination mailbox address", result.CombinedStatus.ToString());
            Assert.Equal("mx.google.com", result.RemoteMta);
            Assert.Equal("test.local", result.ReportingMta);
            Assert.Equal("email_that_does_not_exists_this_is_just_a_test@gmail.com", result.FinalRecipient);
            Assert.Equal("560418D8.2010303@example.com", result.UndeliveredMessageId);
            Assert.IsType<TextPart>(result.DeliveryNotificationPart);
            Assert.NotNull(result.DeliveryStatus);
            Assert.IsType<MessagePart>(result.UndeliveredMessagePart);
            Assert.Null(result.DiagnosticCodes);
            Assert.Equal("failed", result.Action);
        }

        [Fact]
        public void PostfixNonExistingMailbox()
        {
            var message = MimeMessage.Load(OpenFixture("bounce_postfix_non_existing_mailbox.eml"));

            var result = BounceDetector.Detect(message);

            Assert.True(result.IsBounce);
            Assert.True(result.IsHard);
            Assert.False(result.IsSoft);
            Assert.Equal("5 Permanent Failure", result.PrimaryStatus.ToString());
            Assert.Equal("1 Addressing Status", result.SecundaryStatus.ToString());
            Assert.Equal("11 Bad destination mailbox address", result.CombinedStatus.ToString());
            Assert.Null(result.RemoteMta);
            Assert.Equal("test.local", result.ReportingMta);
            Assert.Equal("email_that_does_not_exists_this_is_just_a_test@test.local", result.FinalRecipient);
            Assert.Equal("20150925104956.BD73C158@test.local", result.UndeliveredMessageId);
            Assert.IsType<TextPart>(result.DeliveryNotificationPart);
            Assert.NotNull(result.DeliveryStatus);
            Assert.IsType<MessagePart>(result.UndeliveredMessagePart);
            Assert.NotNull(result.DiagnosticCodes);
            Assert.Equal(2, result.DiagnosticCodes.Length);
            Assert.Equal("", result.DiagnosticCodes[0]);
            Assert.Equal("unknown user:    \"email_that_does_not_exists_this_is_just_a_test@test.local\"", result.DiagnosticCodes[1]);
            Assert.Equal("failed", result.Action);
        }

        [Fact]
        public void QmailNoHostFound()
        {
            var message = MimeMessage.Load(OpenFixture("bounce_qmail_no_host_found.eml"));

            var result = BounceDetector.Detect(message);

            Assert.True(result.IsBounce);
            Assert.True(result.IsHard);
            Assert.False(result.IsSoft);
            Assert.Equal("5 Permanent Failure", result.PrimaryStatus.ToString());
            Assert.Equal("3 Mail System Status", result.SecundaryStatus.ToString());
            Assert.Equal("30 Other or undefined mail system status", result.CombinedStatus.ToString());
            Assert.Null(result.RemoteMta);
            Assert.Equal("silverton.berkeley.edu", result.ReportingMta);
            Assert.Equal("god@heaven.af.mil", result.FinalRecipient);
            Assert.Equal("19960317035438.316.qmail@silverton.berkeley.edu", result.UndeliveredMessageId);
            Assert.IsType<TextPart>(result.DeliveryNotificationPart);
            Assert.NotNull(result.DeliveryStatus);
            Assert.IsType<MessagePart>(result.UndeliveredMessagePart);
            Assert.NotNull(result.DiagnosticCodes);
            Assert.Equal(2, result.DiagnosticCodes.Length);
            Assert.Equal("", result.DiagnosticCodes[0]);
            Assert.Equal("Sorry, I couldn't find any host by that name.", result.DiagnosticCodes[1]);
            Assert.Equal("failed", result.Action);
        }

        [Fact]
        public void QmailExtraLinesBetweenRecipientParagraphs()
        {
            var message = MimeMessage.Load(OpenFixture("bounce_qmail_extra_lines_between_recipient_paragraphs.eml"));

            var result = BounceDetector.Detect(message);

            Assert.True(result.IsBounce);
            Assert.True(result.IsHard);
            Assert.False(result.IsSoft);
            Assert.Equal("5 Permanent Failure", result.PrimaryStatus.ToString());
            Assert.Equal("3 Mail System Status", result.SecundaryStatus.ToString());
            Assert.Equal("30 Other or undefined mail system status", result.CombinedStatus.ToString());
            Assert.Null(result.RemoteMta);
            Assert.Equal("example.com", result.ReportingMta);
            // NB even though there are multiple failure paragraphs, only the last one is returned here.
            Assert.Equal("bob@example.com", result.FinalRecipient);
            Assert.Equal("20160117020038.316.qmail@example.com", result.UndeliveredMessageId);
            Assert.IsType<TextPart>(result.DeliveryNotificationPart);
            Assert.NotNull(result.DeliveryStatus);
            Assert.Equal(3, result.DeliveryStatus.StatusGroups.Count);
            Assert.Equal("dns;example.com", result.DeliveryStatus.StatusGroups[0]["Reporting-MTA"]);
            Assert.Equal("rfc822;alice@example.com", result.DeliveryStatus.StatusGroups[1]["Final-Recipient"]);
            Assert.Equal("rfc822;bob@example.com", result.DeliveryStatus.StatusGroups[2]["Final-Recipient"]);
            Assert.IsType<MessagePart>(result.UndeliveredMessagePart);
            var undeliveredMessage = ((MessagePart)result.UndeliveredMessagePart).Message;
            Assert.Equal("are you there?", undeliveredMessage.Subject);
            Assert.Equal("Just checking.", undeliveredMessage.TextBody);
            Assert.Equal(2, undeliveredMessage.To.Count);
            Assert.Equal("alice@example.com", undeliveredMessage.To[0].ToString());
            Assert.Equal("bob@example.com", undeliveredMessage.To[1].ToString());
            Assert.NotNull(result.DiagnosticCodes);
            Assert.Equal(2, result.DiagnosticCodes.Length);
            Assert.Equal("", result.DiagnosticCodes[0]);
            Assert.Equal("Sorry, I couldn't find any host by that name.", result.DiagnosticCodes[1]);
            Assert.Equal("failed", result.Action);
        }

        [Fact]
        public void QmailWrappedInMultipartAlternateNonExistingMailbox()
        {
            var message = MimeMessage.Load(OpenFixture("bounce_qmail_multipart_alternative_non_existing_mailbox.eml"));

            var result = BounceDetector.Detect(message);

            Assert.True(result.IsBounce);
            Assert.True(result.IsHard);
            Assert.False(result.IsSoft);
            Assert.Equal("5 Permanent Failure", result.PrimaryStatus.ToString());
            Assert.Equal("7 Security or Policy Status", result.SecundaryStatus.ToString());
            Assert.Equal("71 Delivery not authorized, message refused", result.CombinedStatus.ToString());
            Assert.Null(result.RemoteMta);
            Assert.Equal("mgm-smtp.example.com", result.ReportingMta);
            Assert.Equal("notFound@example.com", result.FinalRecipient);
            Assert.Null(result.UndeliveredMessageId);
            Assert.IsType<TextPart>(result.DeliveryNotificationPart);
            Assert.NotNull(result.DeliveryStatus);
            Assert.IsType<MessagePart>(result.UndeliveredMessagePart);
            Assert.NotNull(result.DiagnosticCodes);
            Assert.Equal(2, result.DiagnosticCodes.Length);
            Assert.Equal("4.3.2.1", result.DiagnosticCodes[0]);
            Assert.Equal("does not like recipient. Remote host said: 554 5.7.1 <notFound@example.com>: Relay access denied Giving up on 4.3.2.1.", result.DiagnosticCodes[1]);
            Assert.Equal("failed", result.Action);
        }

        private Stream OpenFixture(string name)
        {
            var type = GetType();
            var assembly = type.Assembly;
            return assembly.GetManifestResourceStream(type.Namespace + ".Fixtures." + name);
        }
    }
}
