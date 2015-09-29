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

        private Stream OpenFixture(string name)
        {
            var type = GetType();
            var assembly = type.Assembly;
            return assembly.GetManifestResourceStream(type.Namespace + ".Fixtures." + name);
        }
    }
}
