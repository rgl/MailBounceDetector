// developed by Rui Lopes (ruilopes.com). licensed under MIT.

using MimeKit;
using System.IO;
using Xunit;

namespace MailBounceDetector.Tests
{
    public class BounceDetectorTests
    {
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
            Assert.Equal("<560418D8.2010303@example.com>", result.UndeliveredMessageId);
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
            Assert.Equal("<20150925104956.BD73C158@test.local>", result.UndeliveredMessageId);
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
            Assert.Equal("<19960317035438.316.qmail@silverton.berkeley.edu>", result.UndeliveredMessageId);
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
