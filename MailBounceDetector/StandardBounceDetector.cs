// developed by Rui Lopes (ruilopes.com). licensed under MIT.

using MimeKit;

namespace MailBounceDetector
{
    public sealed class StandardBounceDetector
    {
        /// <summary>
        /// detects whether a message is a standard bounce message.
        /// </summary>
        ///
        /// essentially this finds the `multipart/report; report-type=delivery-status` part and
        /// its `message/delivery-status` sub-part to decide whether a message is a bounce.
        ///
        /// the message/delivery-status mime part looks something like:
        ///
        ///      Content-Type: message/delivery-status
        ///
        ///      Reporting-MTA: dns; PTPEDGE02.test.local
        ///
        ///      Final-recipient: RFC822;
        ///       email_that_does_not_exists_this_is_just_a_test@gmail.com
        ///      Action: failed
        ///      Status: 5.1.1
        ///      Remote-MTA: dns; mx.google.com
        ///      X-Supplementary-Info: &lt;mx.google.com #5.1.1 smtp;550-5.1.1 The email account
        ///       that you tried to reach does not exist.Please try 550-5.1.1 double-checking
        ///      the recipient's email address for typos or 550-5.1.1 unnecessary spaces.
        ///       Learn more at 550 5.1.1  https://support.google.com/mail/answer/6596
        ///       om11si19081667wic.29 - gsmtp>
        ///
        /// See the multipart/report format RFC:
        ///      The Multipart/Report Media Type for the Reporting of Mail System Administrative Messages
        ///      https://tools.ietf.org/html/rfc6522
        /// See the message/delivery-status RFC:
        ///      An Extensible Message Format for Delivery Status Notifications
        ///      https://tools.ietf.org/html/rfc3464
        public static BounceDetectResult Detect(MimeMessage message)
        {
            var visitor = new Visitor();

            message.Accept(visitor);

            var result = visitor.Result;

            return new BounceDetectResult(
                message,
                result.DeliveryNotificationPart,
                result.DeliveryStatus,
                result.UndeliveredMessagePart
            );
        }

        private sealed class VisitorResult
        {
            // normally this is-a TextPart.
            public MimeEntity DeliveryNotificationPart { get; set; }
            public MessageDeliveryStatus DeliveryStatus { get; set; }
            // normally this is-a MessagePart.
            public MimeEntity UndeliveredMessagePart { get; set; }
        }

        private sealed class Visitor : MimeVisitor
        {
            public VisitorResult Result { get; } = new VisitorResult();

            // called when a multipart/* part is found.
            //
            // we are interested in the `multipart/report; report-type=delivery-status` part. it
            // contains the parts that decribe the bounce.
            //
            // See https://tools.ietf.org/html/rfc3464
            // See https://github.com/jstedfast/MimeKit/blob/master/MimeKit/MessageDeliveryStatus.cs
            // See CreateEntity method at https://github.com/jstedfast/MimeKit/blob/master/MimeKit/ParserOptions.cs
            // See https://github.com/jstedfast/MimeKit/blob/master/MimeKit/MimeParser.cs
            protected override void VisitMultipart(Multipart multipart)
            {
                base.VisitMultipart(multipart);

                // save the multipart/report parts.
                if (multipart.ContentType.MediaSubtype == "report" && multipart.ContentType.Parameters["report-type"] == "delivery-status")
                {
                    if (multipart.Count > 0)
                    {
                        Result.DeliveryNotificationPart = multipart[0];
                    }

                    if (multipart.Count > 1)
                    {
                        Result.DeliveryStatus = multipart[1] as MessageDeliveryStatus;
                    }

                    if (multipart.Count > 2)
                    {
                        Result.UndeliveredMessagePart = multipart[2];
                    }
                }
            }
        }
    }
}
